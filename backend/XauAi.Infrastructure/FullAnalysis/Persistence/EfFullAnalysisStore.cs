using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.FullAnalysis;
using XauAi.Application.MarketData;
using XauAi.Domain.FullAnalysis;
using XauAi.Infrastructure.Persistence;
using DomainFullAnalysis = XauAi.Domain.FullAnalysis.FullAnalysis;

namespace XauAi.Infrastructure.FullAnalysis.Persistence;

internal sealed class EfFullAnalysisStore(XauAiDbContext context) : IFullAnalysisStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<FullAnalysisResult> CreateJobAsync(
        FullAnalysisJobWriteModel job,
        CancellationToken cancellationToken = default)
    {
        var instrumentId = await context.Instruments.AsNoTracking()
            .Where(instrument => instrument.Symbol == job.Symbol)
            .Select(instrument => instrument.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (instrumentId == Guid.Empty)
        {
            throw new FullAnalysisException(
                FullAnalysisErrorCodes.MissingMarketData,
                "The Full Analyst instrument is not present in reference data.");
        }

        var entity = new DomainFullAnalysis
        {
            Id = job.Id,
            InstrumentId = instrumentId,
            Symbol = job.Symbol,
            Timeframe = job.Timeframe.Code(),
            AnalysisTimeUtc = job.AnalysisTimeUtc,
            Decision = FullDecision.Wait.ToString(),
            Reasoning = string.Empty,
            InvalidationJson = "{}",
            Uncertainty = string.Empty,
            Status = FullAnalysisStatus.Analyzing.ToString(),
            Provider = "None",
            Model = string.Empty,
            PromptVersion = job.PromptVersion,
            ConfigurationVersion = job.ConfigurationVersion,
            SnapshotHash = string.Empty,
            CreatedAtUtc = job.CreatedAtUtc,
            UpdatedAtUtc = job.CreatedAtUtc
        };
        context.FullAnalyses.Add(entity);
        context.FullAnalysisLifecycleEvents.Add(Event(
            job.Id,
            "ANALYZING",
            null,
            FullAnalysisStatus.Analyzing,
            "User requested Full Analyst.",
            null,
            job.CreatedAtUtc));
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task UpdateSnapshotAsync(
        FullSnapshotWriteModel snapshot,
        CancellationToken cancellationToken = default)
    {
        var entity = await RequireAnalyzingAsync(snapshot.AnalysisId, cancellationToken);
        entity.CurrentPrice = snapshot.CurrentPrice;
        entity.SnapshotHash = snapshot.SnapshotHash;
        entity.SnapshotJson = snapshot.SnapshotJson;
        entity.UpdatedAtUtc = snapshot.UpdatedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FullAnalysisResult> CompleteAsync(
        FullAnalysisCompletionWriteModel completion,
        CancellationToken cancellationToken = default)
    {
        var entity = await RequireAnalyzingAsync(completion.AnalysisId, cancellationToken);
        var active = completion.Decision != FullDecision.Wait;
        entity.Decision = completion.Decision.ToString();
        entity.Confidence = completion.Confidence;
        entity.Agreement = completion.Agreement;
        entity.ConflictsJson = JsonSerializer.Serialize(completion.Conflicts, SerializerOptions);
        entity.KeyEvidenceIdsJson = JsonSerializer.Serialize(completion.KeyEvidenceIds, SerializerOptions);
        entity.Reasoning = completion.Reasoning;
        entity.InvalidationJson = JsonSerializer.Serialize(completion.Invalidation, SerializerOptions);
        entity.Uncertainty = completion.Uncertainty;
        entity.ValidUntilUtc = active ? completion.ValidUntilUtc : null;
        entity.FutureAvailable = active;
        entity.MasterResultId = completion.MasterResultId;
        entity.Status = (active ? FullAnalysisStatus.Active : FullAnalysisStatus.Wait).ToString();
        entity.Provider = completion.Provider;
        entity.Model = completion.Model;
        entity.PromptVersion = completion.PromptVersion;
        entity.ConfigurationVersion = completion.ConfigurationVersion;
        entity.WorkspaceResultsJson = JsonSerializer.Serialize(completion.WorkspaceResults, SerializerOptions);
        entity.InputTokens = completion.WorkspaceResults.Sum(run => run.InputTokens ?? 0);
        entity.OutputTokens = completion.WorkspaceResults.Sum(run => run.OutputTokens ?? 0);
        entity.UpdatedAtUtc = completion.CompletedAtUtc;
        entity.EndedAtUtc = active ? null : completion.CompletedAtUtc;

        context.FullAnalysisLifecycleEvents.Add(Event(
            entity.Id,
            completion.Decision.ToString().ToUpperInvariant(),
            FullAnalysisStatus.Analyzing,
            active ? FullAnalysisStatus.Active : FullAnalysisStatus.Wait,
            completion.Reasoning,
            entity.CurrentPrice,
            completion.CompletedAtUtc));
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<FullAnalysisResult?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.FullAnalyses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<FullAnalysisResult>> GetActiveAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.FullAnalyses.AsNoTracking()
            .Where(item => item.Symbol == symbol && item.Status == "Active")
            .OrderByDescending(item => item.AnalysisTimeUtc)
            .ToArrayAsync(cancellationToken);
        return [.. entities.Select(Map)];
    }

    public async Task<PagedFullAnalyses> QueryAsync(
        FullAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = context.FullAnalyses.AsNoTracking().Where(item => item.Symbol == query.Symbol);
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
        return new PagedFullAnalyses(
            [.. entities.Select(Map)],
            query.Page,
            query.PageSize,
            total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<IReadOnlyList<FullLifecycleItem>> GetLifecycleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var events = await context.FullAnalysisLifecycleEvents.AsNoTracking()
            .Where(item => item.FullAnalysisId == id)
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return [.. events.Select(item => new FullLifecycleItem(
            item.Id,
            item.EventType,
            ParseNullableStatus(item.PreviousStatus),
            ParseStatus(item.Status),
            item.Reason,
            item.Price,
            item.OccurredAtUtc))];
    }

    public async Task<bool> TryTransitionAsync(
        FullLifecycleTransition transition,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transactionScope = await context.Database.BeginTransactionAsync(cancellationToken);
            var expected = transition.ExpectedStatus.ToString();
            var next = transition.NewStatus.ToString();
            var affected = await context.FullAnalyses
                .Where(item => item.Id == transition.AnalysisId && item.Status == expected)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, next)
                    .SetProperty(item => item.FutureAvailable, false)
                    .SetProperty(item => item.UpdatedAtUtc, transition.OccurredAtUtc)
                    .SetProperty(item => item.EndedAtUtc, transition.OccurredAtUtc),
                    cancellationToken);
            if (affected == 0)
            {
                await transactionScope.RollbackAsync(cancellationToken);
                return false;
            }

            context.FullAnalysisLifecycleEvents.Add(Event(
                transition.AnalysisId,
                transition.EventType,
                transition.ExpectedStatus,
                transition.NewStatus,
                transition.Reason,
                transition.Price,
                transition.OccurredAtUtc));
            await context.SaveChangesAsync(cancellationToken);
            await transactionScope.CommitAsync(cancellationToken);
            return true;
        });
    }

    private async Task<DomainFullAnalysis> RequireAnalyzingAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await context.FullAnalyses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw NotFound();
        if (!string.Equals(entity.Status, FullAnalysisStatus.Analyzing.ToString(), StringComparison.Ordinal))
        {
            throw new FullAnalysisException(FullAnalysisErrorCodes.InvalidState, "Only an analyzing Full Analyst job can be updated.");
        }

        return entity;
    }

    private static FullAnalysisResult Map(DomainFullAnalysis item) => new(
        item.Id,
        item.Symbol,
        item.AnalysisTimeUtc,
        item.CurrentPrice,
        ParseTimeframe(item.Timeframe),
        Parse(item.Decision, FullDecision.Wait),
        item.Confidence,
        item.Agreement,
        Deserialize<string[]>(item.ConflictsJson) ?? [],
        Deserialize<Guid[]>(item.KeyEvidenceIdsJson) ?? [],
        item.Reasoning,
        item.InvalidationJson == "{}" ? null : Deserialize<FullInvalidation>(item.InvalidationJson),
        item.Uncertainty,
        item.ValidUntilUtc,
        item.FutureAvailable,
        Deserialize<FullWorkspaceRunResult[]>(item.WorkspaceResultsJson) ?? [],
        item.MasterResultId,
        ParseStatus(item.Status),
        item.Provider,
        item.Model,
        item.PromptVersion,
        item.ConfigurationVersion,
        item.SnapshotHash,
        item.SnapshotJson == "{}" ? null : Deserialize<FullAnalysisSnapshot>(item.SnapshotJson),
        item.InputTokens,
        item.OutputTokens,
        item.CreatedAtUtc,
        item.UpdatedAtUtc,
        item.EndedAtUtc);

    private static FullAnalysisLifecycleEvent Event(
        Guid analysisId,
        string eventType,
        FullAnalysisStatus? previous,
        FullAnalysisStatus status,
        string? reason,
        decimal? price,
        DateTimeOffset occurredAt) => new()
        {
            Id = Guid.NewGuid(),
            FullAnalysisId = analysisId,
            EventType = eventType,
            PreviousStatus = previous?.ToString(),
            Status = status.ToString(),
            Reason = Limit(reason, 500),
            Price = price,
            OccurredAtUtc = occurredAt,
            CreatedAtUtc = occurredAt
        };

    private static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static MarketTimeframe ParseTimeframe(string value) =>
        MarketTimeframes.TryParse(value, out var timeframe) ? timeframe : MarketTimeframe.M5;
    private static FullAnalysisStatus ParseStatus(string value) => Parse(value, FullAnalysisStatus.Wait);
    private static FullAnalysisStatus? ParseNullableStatus(string? value) => value is null ? null : ParseStatus(value);
    private static TEnum Parse<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    private static string? Limit(string? value, int maximumLength) =>
        value is null || value.Length <= maximumLength ? value : value[..maximumLength];
    private static FullAnalysisException NotFound() => new(
        FullAnalysisErrorCodes.NotFound,
        "The requested Full Analysis was not found.");
}
