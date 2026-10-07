using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.MarketData;
using XauAi.Application.TargetAnalysis;
using XauAi.Domain.TargetAnalysis;
using XauAi.Infrastructure.Persistence;
using DomainTargetAnalysis = XauAi.Domain.TargetAnalysis.TargetAnalysis;

namespace XauAi.Infrastructure.TargetAnalysis.Persistence;

internal sealed class EfTargetAnalysisStore(XauAiDbContext context) : ITargetAnalysisStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<TargetAnalysisResult> CreateJobAsync(
        TargetAnalysisJobWriteModel job,
        CancellationToken cancellationToken = default)
    {
        var instrumentId = await context.Instruments.AsNoTracking()
            .Where(instrument => instrument.Symbol == job.Symbol)
            .Select(instrument => instrument.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (instrumentId == Guid.Empty)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.MissingMarketData,
                "The target-analysis instrument is not present in reference data.");
        }

        var entity = new DomainTargetAnalysis
        {
            Id = job.Id,
            InstrumentId = instrumentId,
            Symbol = job.Symbol,
            Timeframe = job.Timeframe.Code(),
            AnalysisTimeUtc = job.AnalysisTimeUtc,
            DirectionContext = TargetDirectionContext.Unknown.ToString(),
            ReasoningSummary = string.Empty,
            Uncertainty = string.Empty,
            Status = TargetAnalysisStatus.Analyzing.ToString(),
            Provider = "None",
            Model = string.Empty,
            PromptVersion = job.PromptVersion,
            ConfigurationVersion = job.ConfigurationVersion,
            SnapshotJson = "{}",
            CreatedAtUtc = job.CreatedAtUtc,
            UpdatedAtUtc = job.CreatedAtUtc
        };
        context.TargetAnalyses.Add(entity);
        context.TargetAnalysisLifecycleEvents.Add(Event(
            job.Id,
            "ANALYZING",
            null,
            TargetAnalysisStatus.Analyzing,
            "User requested Target Analyst.",
            null,
            job.CreatedAtUtc));
        await context.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task UpdateSnapshotAsync(
        TargetSnapshotWriteModel snapshot,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.TargetAnalyses.SingleOrDefaultAsync(
            item => item.Id == snapshot.AnalysisId,
            cancellationToken) ?? throw NotFound();
        if (!string.Equals(entity.Status, TargetAnalysisStatus.Analyzing.ToString(), StringComparison.Ordinal))
        {
            throw InvalidState("Only an analyzing target job can receive a snapshot.");
        }

        entity.CurrentPrice = snapshot.CurrentPrice;
        entity.SnapshotJson = snapshot.SnapshotJson;
        entity.UpdatedAtUtc = snapshot.UpdatedAtUtc;
        var existingIds = await context.TargetAnalysisEvidence.AsNoTracking()
            .Where(link => link.TargetAnalysisId == snapshot.AnalysisId)
            .Select(link => link.EvidenceRecordId)
            .ToArrayAsync(cancellationToken);
        context.TargetAnalysisEvidence.AddRange(snapshot.EvidenceIds
            .Except(existingIds)
            .Select(evidenceId => new TargetAnalysisEvidence
            {
                TargetAnalysisId = snapshot.AnalysisId,
                EvidenceRecordId = evidenceId,
                Role = "Snapshot"
            }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TargetAnalysisResult> CompleteAsync(
        TargetAnalysisCompletionWriteModel completion,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await RequireAnalyzingAsync(completion.AnalysisId, cancellationToken);
        AddWorkspaceResults(completion.AnalysisId, completion.WorkspaceResults);
        var masterRun = completion.WorkspaceResults.Single(run => run.Workspace == TargetWorkspace.Master);
        entity.TargetPrice = completion.Master.TargetPrice;
        entity.InvalidationPrice = completion.Master.InvalidationPrice;
        entity.DirectionContext = completion.Master.DirectionContext.ToString();
        entity.Confidence = completion.Master.Confidence;
        entity.ValidUntilUtc = completion.Master.ValidUntilUtc;
        entity.MasterResultId = masterRun.Id;
        entity.ReasoningSummary = completion.Master.ReasoningSummary;
        entity.Uncertainty = completion.Master.Uncertainty;
        entity.NoTargetReason = null;
        entity.Status = TargetAnalysisStatus.Active.ToString();
        entity.Provider = completion.Provider;
        entity.Model = completion.Model;
        entity.PromptVersion = completion.PromptVersion;
        entity.ConfigurationVersion = completion.ConfigurationVersion;
        entity.UpdatedAtUtc = completion.CompletedAtUtc;
        context.TargetAnalysisLifecycleEvents.Add(Event(
            entity.Id,
            "SUCCESS",
            TargetAnalysisStatus.Analyzing,
            TargetAnalysisStatus.Success,
            "Target Master produced one validated target.",
            completion.Master.TargetPrice,
            completion.CompletedAtUtc));
        context.TargetAnalysisLifecycleEvents.Add(Event(
            entity.Id,
            "ACTIVATED",
            TargetAnalysisStatus.Success,
            TargetAnalysisStatus.Active,
            "Validated target monitoring started.",
            completion.Master.TargetPrice,
            completion.CompletedAtUtc.AddTicks(1)));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<TargetAnalysisResult> CompleteNoValidTargetAsync(
        TargetNoValidTargetWriteModel completion,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await RequireAnalyzingAsync(completion.AnalysisId, cancellationToken);
        AddWorkspaceResults(completion.AnalysisId, completion.WorkspaceResults);
        entity.TargetPrice = null;
        entity.InvalidationPrice = null;
        entity.DirectionContext = TargetDirectionContext.Unknown.ToString();
        entity.Confidence = null;
        entity.ValidUntilUtc = null;
        entity.MasterResultId = completion.WorkspaceResults
            .FirstOrDefault(run => run.Workspace == TargetWorkspace.Master)?.Id;
        entity.ReasoningSummary = completion.ReasoningSummary;
        entity.Uncertainty = completion.Uncertainty;
        entity.NoTargetReason = completion.Reason;
        entity.Status = TargetAnalysisStatus.NoValidTarget.ToString();
        entity.Provider = completion.Provider;
        entity.Model = completion.Model;
        entity.PromptVersion = completion.PromptVersion;
        entity.ConfigurationVersion = completion.ConfigurationVersion;
        entity.UpdatedAtUtc = completion.CompletedAtUtc;
        entity.EndedAtUtc = completion.CompletedAtUtc;
        context.TargetAnalysisLifecycleEvents.Add(Event(
            entity.Id,
            "NO_VALID_TARGET",
            TargetAnalysisStatus.Analyzing,
            TargetAnalysisStatus.NoValidTarget,
            completion.Reason,
            entity.CurrentPrice,
            completion.CompletedAtUtc));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<TargetAnalysisResult?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.TargetAnalyses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : await MapAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<TargetAnalysisResult>> GetActiveAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.TargetAnalyses.AsNoTracking()
            .Where(item => item.Symbol == symbol && item.Status == "Active")
            .OrderByDescending(item => item.AnalysisTimeUtc)
            .ToArrayAsync(cancellationToken);
        return await MapAsync(entities, cancellationToken);
    }

    public async Task<PagedTargetAnalyses> QueryAsync(
        TargetAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = context.TargetAnalyses.AsNoTracking().Where(item => item.Symbol == query.Symbol);
        if (query.Timeframe.HasValue)
        {
            var timeframe = query.Timeframe.Value.Code();
            source = source.Where(item => item.Timeframe == timeframe);
        }

        if (query.Status.HasValue)
        {
            var status = query.Status.Value.ToString();
            source = source.Where(item => item.Status == status);
        }

        var total = await source.CountAsync(cancellationToken);
        var entities = await source
            .OrderByDescending(item => item.AnalysisTimeUtc)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedTargetAnalyses(
            await MapAsync(entities, cancellationToken),
            query.Page,
            query.PageSize,
            total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<IReadOnlyList<TargetLifecycleItem>> GetLifecycleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var events = await context.TargetAnalysisLifecycleEvents.AsNoTracking()
            .Where(item => item.TargetAnalysisId == id)
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return [.. events.Select(item => new TargetLifecycleItem(
            item.Id,
            item.EventType,
            ParseNullableStatus(item.PreviousStatus),
            ParseStatus(item.Status),
            item.Reason,
            item.Price,
            item.OccurredAtUtc))];
    }

    public async Task<bool> TryTransitionAsync(
        TargetLifecycleTransition transition,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var expected = transition.ExpectedStatus.ToString();
        var next = transition.NewStatus.ToString();
        var affected = await context.TargetAnalyses
            .Where(item => item.Id == transition.AnalysisId && item.Status == expected)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, next)
                .SetProperty(item => item.UpdatedAtUtc, transition.OccurredAtUtc)
                .SetProperty(item => item.EndedAtUtc, transition.OccurredAtUtc),
                cancellationToken);
        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        context.TargetAnalysisLifecycleEvents.Add(Event(
            transition.AnalysisId,
            transition.EventType,
            transition.ExpectedStatus,
            transition.NewStatus,
            transition.Reason,
            transition.Price,
            transition.OccurredAtUtc));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<DomainTargetAnalysis> RequireAnalyzingAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await context.TargetAnalyses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw NotFound();
        if (!string.Equals(entity.Status, TargetAnalysisStatus.Analyzing.ToString(), StringComparison.Ordinal))
        {
            throw InvalidState("Only an analyzing target job can be completed.");
        }

        return entity;
    }

    private void AddWorkspaceResults(
        Guid analysisId,
        IReadOnlyList<TargetWorkspaceRunResult> runs)
    {
        context.TargetSpecialistResults.AddRange(runs.Select(run =>
        {
            var specialist = run.SpecialistOutput;
            var master = run.MasterOutput;
            var evidenceIds = specialist?.EvidenceIds ?? master?.EvidenceIds ?? [];
            return new TargetSpecialistResult
            {
                Id = run.Id,
                TargetAnalysisId = analysisId,
                Workspace = run.Workspace.ToString(),
                Status = run.Status.ToString(),
                HasCandidate = specialist?.HasCandidate ?? master?.ValidTarget ?? false,
                CandidateTargetPrice = specialist?.CandidateTargetPrice ?? master?.TargetPrice,
                CandidateInvalidationPrice = specialist?.CandidateInvalidationPrice ?? master?.InvalidationPrice,
                DirectionContext = (specialist?.DirectionContext ?? master?.DirectionContext ?? TargetDirectionContext.Unknown).ToString(),
                Confidence = specialist?.Confidence ?? master?.Confidence,
                RiskAcceptable = run.Workspace == TargetWorkspace.Risk ? specialist?.RiskAcceptable : null,
                Summary = specialist?.Summary ?? master?.ReasoningSummary ?? string.Empty,
                Uncertainty = specialist?.Uncertainty ?? master?.Uncertainty ?? string.Empty,
                OutputJson = string.IsNullOrWhiteSpace(run.OutputJson) ? "{}" : run.OutputJson,
                EvidenceIdsJson = JsonSerializer.Serialize(evidenceIds, SerializerOptions),
                Provider = run.Configuration.Provider,
                Model = run.Configuration.Model,
                PromptVersion = run.Configuration.PromptVersion,
                ConfigurationVersion = run.Configuration.ConfigurationVersion,
                InputTokens = run.InputTokens,
                OutputTokens = run.OutputTokens,
                LatencyMilliseconds = run.LatencyMilliseconds,
                ErrorCode = run.ErrorCode,
                ErrorMessage = run.ErrorMessage,
                CreatedAtUtc = run.CreatedAtUtc,
                CompletedAtUtc = run.CompletedAtUtc
            };
        }));
    }

    private async Task<TargetAnalysisResult> MapAsync(
        DomainTargetAnalysis entity,
        CancellationToken cancellationToken)
    {
        var mapped = await MapAsync([entity], cancellationToken);
        return mapped[0];
    }

    private async Task<IReadOnlyList<TargetAnalysisResult>> MapAsync(
        IReadOnlyList<DomainTargetAnalysis> entities,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
        {
            return [];
        }

        var ids = entities.Select(item => item.Id).ToArray();
        var specialistRows = await context.TargetSpecialistResults.AsNoTracking()
            .Where(item => ids.Contains(item.TargetAnalysisId))
            .OrderBy(item => item.Workspace)
            .ToArrayAsync(cancellationToken);
        var specialists = specialistRows.GroupBy(item => item.TargetAnalysisId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TargetSpecialistResultView>)[.. group.Select(MapSpecialist)]);
        var evidenceRows = await context.TargetAnalysisEvidence.AsNoTracking()
            .Where(item => ids.Contains(item.TargetAnalysisId))
            .OrderBy(item => item.EvidenceRecordId)
            .ToArrayAsync(cancellationToken);
        var evidence = evidenceRows.GroupBy(item => item.TargetAnalysisId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)[.. group.Select(item => item.EvidenceRecordId)]);

        return [.. entities.Select(item => new TargetAnalysisResult(
            item.Id,
            item.Symbol,
            item.AnalysisTimeUtc,
            item.CurrentPrice,
            ParseTimeframe(item.Timeframe),
            item.TargetPrice,
            item.InvalidationPrice,
            ParseDirection(item.DirectionContext),
            item.Confidence,
            item.ValidUntilUtc,
            evidence.GetValueOrDefault(item.Id, []),
            specialists.GetValueOrDefault(item.Id, []),
            item.MasterResultId,
            item.ReasoningSummary,
            item.Uncertainty,
            item.NoTargetReason,
            ParseStatus(item.Status),
            item.Provider,
            item.Model,
            item.PromptVersion,
            item.ConfigurationVersion,
            DeserializeSnapshot(item.SnapshotJson),
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            item.EndedAtUtc))];
    }

    private static TargetSpecialistResultView MapSpecialist(TargetSpecialistResult item) => new(
        item.Id,
        Parse(item.Workspace, TargetWorkspace.Master),
        Parse(item.Status, TargetWorkspaceExecutionStatus.Failed),
        item.HasCandidate,
        item.CandidateTargetPrice,
        item.CandidateInvalidationPrice,
        ParseDirection(item.DirectionContext),
        item.Confidence,
        item.RiskAcceptable,
        item.Summary,
        item.Uncertainty,
        DeserializeEvidenceIds(item.EvidenceIdsJson),
        item.Provider,
        item.Model,
        item.PromptVersion,
        item.ConfigurationVersion,
        item.InputTokens,
        item.OutputTokens,
        item.LatencyMilliseconds,
        item.ErrorCode,
        item.ErrorMessage,
        item.CreatedAtUtc,
        item.CompletedAtUtc);

    private static TargetAnalysisLifecycleEvent Event(
        Guid analysisId,
        string eventType,
        TargetAnalysisStatus? previous,
        TargetAnalysisStatus status,
        string? reason,
        decimal? price,
        DateTimeOffset occurredAt) => new()
        {
            Id = Guid.NewGuid(),
            TargetAnalysisId = analysisId,
            EventType = eventType,
            PreviousStatus = previous?.ToString(),
            Status = status.ToString(),
            Reason = reason,
            Price = price,
            OccurredAtUtc = occurredAt,
            CreatedAtUtc = occurredAt
        };

    private static TargetAnalysisSnapshot? DeserializeSnapshot(string json)
    {
        if (json == "{}")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TargetAnalysisSnapshot>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<Guid> DeserializeEvidenceIds(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static MarketTimeframe ParseTimeframe(string value) =>
        MarketTimeframes.TryParse(value, out var timeframe) ? timeframe : MarketTimeframe.M5;

    private static TargetAnalysisStatus ParseStatus(string value) =>
        Parse(value, TargetAnalysisStatus.NoValidTarget);

    private static TargetAnalysisStatus? ParseNullableStatus(string? value) =>
        value is null ? null : ParseStatus(value);

    private static TargetDirectionContext ParseDirection(string value) =>
        Parse(value, TargetDirectionContext.Unknown);

    private static TEnum Parse<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;

    private static TargetAnalysisException NotFound() => new(
        TargetAnalysisErrorCodes.NotFound,
        "The requested target analysis was not found.");

    private static TargetAnalysisException InvalidState(string message) => new(
        TargetAnalysisErrorCodes.InvalidState,
        message);
}
