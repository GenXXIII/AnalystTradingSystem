using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.AI;
using XauAi.Domain.AI;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.AI.Persistence;

internal sealed class EfAiInterpretationStore(XauAiDbContext context) : IAiInterpretationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<IReadOnlyList<AiEvidenceCandidate>> LoadEvidenceAsync(
        AiEvidenceSelectionQuery query,
        CancellationToken cancellationToken = default)
    {
        var evidenceTypes = RelevantEvidenceTypes(query.Specialist);
        var source = context.EvidenceRecords.AsNoTracking()
            .Where(record => record.CanonicalSymbol == query.Instrument
                && record.IsRelevant
                && evidenceTypes.Contains(record.EvidenceType)
                && record.AvailableAtUtc >= query.FromUtc
                && record.AvailableAtUtc <= query.AnalysisTimeUtc
                && (!record.ValidFromUtc.HasValue || record.ValidFromUtc <= query.AnalysisTimeUtc)
                && (!record.ValidToUtc.HasValue || record.ValidToUtc > query.AnalysisTimeUtc));
        var records = await source
            .OrderByDescending(record => record.AvailableAtUtc)
            .ThenByDescending(record => record.Id)
            .Take(query.CandidateLimit)
            .ToArrayAsync(cancellationToken);
        if (records.Length == 0)
        {
            return [];
        }

        var ids = records.Select(record => record.Id).ToArray();
        var economicValues = await context.EconomicEvents.AsNoTracking()
            .Where(economicEvent => ids.Contains(economicEvent.Id))
            .Select(economicEvent => new
            {
                economicEvent.Id,
                economicEvent.PreviousValue,
                economicEvent.ForecastValue,
                economicEvent.ActualValue,
                economicEvent.ValueUnit
            })
            .ToDictionaryAsync(economicEvent => economicEvent.Id, cancellationToken);
        var clusterRows = await context.EvidenceClusterMembers.AsNoTracking()
            .Where(member => ids.Contains(member.EvidenceId))
            .Select(member => new { member.EvidenceId, member.EvidenceClusterId })
            .ToArrayAsync(cancellationToken);
        var clusters = clusterRows
            .GroupBy(row => row.EvidenceId)
            .ToDictionary(group => group.Key, group => (Guid?)group.OrderBy(row => row.EvidenceClusterId).First().EvidenceClusterId);
        var relationRows = await context.EvidenceRelations.AsNoTracking()
            .Where(relation => ids.Contains(relation.EvidenceId) || ids.Contains(relation.RelatedEvidenceId))
            .Select(relation => new AiEvidenceRelation(
                relation.EvidenceId,
                relation.RelatedEvidenceId,
                relation.RelationType))
            .ToArrayAsync(cancellationToken);
        var relations = relationRows
            .GroupBy(relation => relation.EvidenceId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<AiEvidenceRelation>)[.. group]);

        return [.. records.Select(record =>
        {
            economicValues.TryGetValue(record.Id, out var economicEvent);
            return new AiEvidenceCandidate(
                record.Id,
                record.EvidenceType,
                record.SourceType,
                record.SourceKey,
                record.ExternalId,
                record.ContentHash,
                record.CanonicalSymbol,
                record.TimeframeCode,
                record.ObservedAtUtc,
                record.AvailableAtUtc,
                record.PublishedAtUtc,
                record.ValidFromUtc,
                record.ValidToUtc,
                record.Title,
                record.Summary,
                record.NumericValue,
                record.OriginalValue,
                record.Unit ?? economicEvent?.ValueUnit ?? "Unknown",
                record.Direction,
                record.Importance,
                record.Category,
                record.Quality,
                record.Completeness,
                record.IsRelevant,
                record.OriginalSourceUrl,
                record.MetadataJson,
                record.UpdatedAtUtc,
                clusters.GetValueOrDefault(record.Id),
                RelationsFor(record.Id, relations, relationRows))
            {
                PreviousValue = economicEvent?.PreviousValue,
                ExpectedValue = economicEvent?.ForecastValue,
                ActualValue = economicEvent?.ActualValue
            };
        })];
    }

    public async Task<AiInterpretationResult?> FindCurrentByCacheKeyAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var analysis = await context.AiAnalyses.AsNoTracking()
            .Where(item => item.CacheKey == cacheKey
                && item.Status == AiInterpretationExecutionStatus.Completed.ToString()
                && item.LifecycleStatus == AiInterpretationLifecycle.Current.ToString())
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return analysis is null ? null : await MapAsync(analysis, cancellationToken);
    }

    public async Task<AiInterpretationResult> SaveCompletedAsync(
        AiInterpretationWriteModel interpretation,
        CancellationToken cancellationToken = default)
    {
        var instrumentId = await InstrumentIdAsync(interpretation.Instrument, cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.AiAnalyses
            .Where(item => item.InstrumentId == instrumentId
                && item.Specialist == interpretation.Specialist.ToString()
                && item.AnalysisType == interpretation.Interpretation.InterpretationType.ToString()
                && item.Timeframe == interpretation.Timeframe
                && item.Status == AiInterpretationExecutionStatus.Completed.ToString()
                && item.LifecycleStatus == AiInterpretationLifecycle.Current.ToString())
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.LifecycleStatus, AiInterpretationLifecycle.Superseded.ToString()),
                cancellationToken);

        var entity = CreateBase(
            interpretation.Id,
            instrumentId,
            interpretation.Timeframe,
            interpretation.Specialist,
            interpretation.Interpretation.InterpretationType,
            interpretation.Provider,
            interpretation.Model,
            interpretation.PromptVersion,
            interpretation.ConfigurationVersion,
            interpretation.EvidenceVersion,
            interpretation.CacheKey,
            interpretation.InputDigest,
            interpretation.AnalysisTimeUtc,
            interpretation.EvidenceUpdatedAtUtc,
            interpretation.CreatedAtUtc,
            interpretation.CompletedAtUtc,
            AiInterpretationExecutionStatus.Completed,
            AiInterpretationLifecycle.Current,
            interpretation.LatencyMilliseconds);
        entity.Direction = interpretation.Interpretation.Direction.ToString();
        entity.Impact = interpretation.Interpretation.Impact.ToString();
        entity.AffectedAssetsJson = JsonSerializer.Serialize(interpretation.Interpretation.AffectedAssets, SerializerOptions);
        entity.Mechanism = interpretation.Interpretation.Mechanism;
        entity.ExpectedEffect = interpretation.Interpretation.ExpectedEffect;
        entity.ObservedReaction = interpretation.Interpretation.ObservedReaction;
        entity.ReactionAlignment = interpretation.Interpretation.ReactionAlignment.ToString();
        entity.CurrentRelevance = interpretation.Interpretation.CurrentRelevance.ToString();
        entity.Confidence = interpretation.Interpretation.Confidence;
        entity.Uncertainty = interpretation.Interpretation.Uncertainty;
        entity.Summary = interpretation.Interpretation.Summary;
        entity.Output = interpretation.OutputJson;
        entity.InputTokens = interpretation.InputTokens;
        entity.OutputTokens = interpretation.OutputTokens;
        context.AiAnalyses.Add(entity);
        AddEvidenceLinks(interpretation.Id, interpretation.EvidenceIds);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<AiInterpretationResult> SaveFailureAsync(
        AiInterpretationFailureWriteModel interpretation,
        CancellationToken cancellationToken = default)
    {
        var instrumentId = await InstrumentIdAsync(interpretation.Instrument, cancellationToken);
        var entity = CreateBase(
            interpretation.Id,
            instrumentId,
            interpretation.Timeframe,
            interpretation.Specialist,
            interpretation.InterpretationType,
            interpretation.Provider,
            interpretation.Model,
            interpretation.PromptVersion,
            interpretation.ConfigurationVersion,
            interpretation.EvidenceVersion,
            interpretation.CacheKey,
            interpretation.InputDigest,
            interpretation.AnalysisTimeUtc,
            interpretation.EvidenceUpdatedAtUtc,
            interpretation.CreatedAtUtc,
            interpretation.CompletedAtUtc,
            AiInterpretationExecutionStatus.Failed,
            AiInterpretationLifecycle.Invalid,
            interpretation.LatencyMilliseconds);
        entity.ErrorCode = interpretation.ErrorCode;
        entity.ErrorMessage = interpretation.ErrorMessage;
        context.AiAnalyses.Add(entity);
        AddEvidenceLinks(interpretation.Id, interpretation.EvidenceIds);
        await context.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task MarkChangedInterpretationsStaleAsync(
        string? instrument,
        DateTimeOffset checkedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var affected = await (
            from analysis in context.AiAnalyses.AsNoTracking()
            join instrumentRow in context.Instruments.AsNoTracking() on analysis.InstrumentId equals instrumentRow.Id
            join link in context.AiAnalysisEvidence.AsNoTracking() on analysis.Id equals link.AiAnalysisId
            join evidence in context.EvidenceRecords.AsNoTracking() on link.EvidenceRecordId equals evidence.Id
            where (instrument == null || instrumentRow.Symbol == instrument)
                && analysis.Status == "Completed"
                && analysis.LifecycleStatus == "Current"
                && (evidence.UpdatedAtUtc > analysis.EvidenceUpdatedAtUtc
                    || (evidence.ValidToUtc.HasValue && evidence.ValidToUtc <= checkedAtUtc))
            select analysis.Id)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        if (affected.Length > 0)
        {
            await context.AiAnalyses
                .Where(item => affected.Contains(item.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.LifecycleStatus, AiInterpretationLifecycle.Stale.ToString()),
                    cancellationToken);
        }
    }

    public async Task<AiInterpretationResult?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var analysis = await context.AiAnalyses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return analysis is null ? null : await MapAsync(analysis, cancellationToken);
    }

    public async Task<AiInterpretationResult?> GetLatestAsync(
        string instrument,
        AiSpecialist? specialist,
        AiInterpretationType? interpretationType,
        string? timeframe,
        CancellationToken cancellationToken = default)
    {
        var source = from item in context.AiAnalyses.AsNoTracking()
                     join instrumentRow in context.Instruments.AsNoTracking()
                         on item.InstrumentId equals instrumentRow.Id
                     where instrumentRow.Symbol == instrument
                         && item.Status == "Completed"
                         && item.LifecycleStatus == "Current"
                     select item;
        if (specialist.HasValue)
        {
            var value = specialist.Value.ToString();
            source = source.Where(item => item.Specialist == value);
        }

        if (interpretationType.HasValue)
        {
            var value = interpretationType.Value.ToString();
            source = source.Where(item => item.AnalysisType == value);
        }

        if (timeframe is not null)
        {
            source = source.Where(item => item.Timeframe == timeframe);
        }

        var analysis = await source
            .OrderByDescending(item => item.AnalysisTimeUtc)
            .ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return analysis is null ? null : await MapAsync(analysis, cancellationToken);
    }

    public async Task<PagedAiInterpretations> QueryAsync(
        AiInterpretationQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = context.AiAnalyses.AsNoTracking().AsQueryable();
        if (query.Instrument is not null)
        {
            var instrumentId = await context.Instruments.AsNoTracking()
                .Where(item => item.Symbol == query.Instrument)
                .Select(item => (Guid?)item.Id)
                .SingleOrDefaultAsync(cancellationToken);
            source = instrumentId.HasValue
                ? source.Where(item => item.InstrumentId == instrumentId.Value)
                : source.Where(item => false);
        }

        if (query.Specialist.HasValue)
        {
            var value = query.Specialist.Value.ToString();
            source = source.Where(item => item.Specialist == value);
        }

        if (query.InterpretationType.HasValue)
        {
            var value = query.InterpretationType.Value.ToString();
            source = source.Where(item => item.AnalysisType == value);
        }

        if (query.Timeframe is not null) source = source.Where(item => item.Timeframe == query.Timeframe);
        if (query.Lifecycle.HasValue)
        {
            var value = query.Lifecycle.Value.ToString();
            source = source.Where(item => item.LifecycleStatus == value);
        }
        else if (!query.IncludeHistorical)
        {
            source = source.Where(item => item.LifecycleStatus == "Current");
        }

        if (query.FromUtc.HasValue) source = source.Where(item => item.AnalysisTimeUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue) source = source.Where(item => item.AnalysisTimeUtc < query.ToUtc.Value);

        var total = await source.CountAsync(cancellationToken);
        var rows = await source
            .OrderByDescending(item => item.AnalysisTimeUtc)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedAiInterpretations(
            await MapAsync(rows, cancellationToken),
            query.Page,
            query.PageSize,
            total,
            Pages(total, query.PageSize));
    }

    public async Task<PagedAiInterpretations> GetByEvidenceAsync(
        Guid evidenceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ids = context.AiAnalysisEvidence.AsNoTracking()
            .Where(link => link.EvidenceRecordId == evidenceId)
            .Select(link => link.AiAnalysisId);
        var source = context.AiAnalyses.AsNoTracking().Where(item => ids.Contains(item.Id));
        var total = await source.CountAsync(cancellationToken);
        var rows = await source
            .OrderByDescending(item => item.AnalysisTimeUtc)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedAiInterpretations(
            await MapAsync(rows, cancellationToken),
            page,
            pageSize,
            total,
            Pages(total, pageSize));
    }

    private async Task<Guid> InstrumentIdAsync(string instrument, CancellationToken cancellationToken) =>
        await context.Instruments.AsNoTracking()
            .Where(item => item.Symbol == instrument)
            .Select(item => item.Id)
            .SingleOrDefaultAsync(cancellationToken) is var id && id != Guid.Empty
                ? id
                : throw new AiInterpretationException(
                    AiInterpretationErrorCodes.NoEvidence,
                    "The interpretation instrument is not present in reference data.");

    private void AddEvidenceLinks(Guid analysisId, IReadOnlyList<Guid> evidenceIds) =>
        context.AiAnalysisEvidence.AddRange(evidenceIds.Distinct().Select(evidenceId => new AiAnalysisEvidence
        {
            AiAnalysisId = analysisId,
            EvidenceRecordId = evidenceId,
            Role = "Input"
        }));

    private static AiAnalysis CreateBase(
        Guid id,
        Guid instrumentId,
        string? timeframe,
        AiSpecialist specialist,
        AiInterpretationType interpretationType,
        string provider,
        string model,
        string promptVersion,
        string configurationVersion,
        string evidenceVersion,
        string cacheKey,
        string inputDigest,
        DateTimeOffset analysisTimeUtc,
        DateTimeOffset evidenceUpdatedAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset completedAtUtc,
        AiInterpretationExecutionStatus status,
        AiInterpretationLifecycle lifecycle,
        int latencyMilliseconds) => new()
        {
            Id = id,
            InstrumentId = instrumentId,
            AnalysisType = interpretationType.ToString(),
            Specialist = specialist.ToString(),
            Timeframe = timeframe,
            Provider = provider,
            Model = model,
            ModelVersion = model,
            PromptIdentifier = $"{specialist}.{interpretationType}",
            PromptVersion = promptVersion,
            AnalysisVersion = configurationVersion,
            ApplicationVersion = "1.0.0",
            InputDigest = inputDigest,
            CacheKey = cacheKey,
            EvidenceVersion = evidenceVersion,
            AnalysisTimeUtc = analysisTimeUtc,
            EvidenceUpdatedAtUtc = evidenceUpdatedAtUtc,
            Direction = AiInterpretationDirection.Unknown.ToString(),
            Impact = AiInterpretationImpact.Unknown.ToString(),
            AffectedAssetsJson = "[]",
            Mechanism = string.Empty,
            ExpectedEffect = string.Empty,
            ObservedReaction = string.Empty,
            ReactionAlignment = AiReactionAlignment.Unknown.ToString(),
            CurrentRelevance = AiCurrentRelevance.Unknown.ToString(),
            Uncertainty = string.Empty,
            Summary = string.Empty,
            Status = status.ToString(),
            LifecycleStatus = lifecycle.ToString(),
            LatencyMilliseconds = latencyMilliseconds,
            CreatedAtUtc = createdAtUtc,
            CompletedAtUtc = completedAtUtc
        };

    private async Task<AiInterpretationResult> MapAsync(
        AiAnalysis analysis,
        CancellationToken cancellationToken)
    {
        var results = await MapAsync([analysis], cancellationToken);
        return results[0];
    }

    private async Task<IReadOnlyList<AiInterpretationResult>> MapAsync(
        IReadOnlyList<AiAnalysis> analyses,
        CancellationToken cancellationToken)
    {
        if (analyses.Count == 0)
        {
            return [];
        }

        var analysisIds = analyses.Select(item => item.Id).ToArray();
        var instrumentIds = analyses.Where(item => item.InstrumentId.HasValue)
            .Select(item => item.InstrumentId!.Value)
            .Distinct()
            .ToArray();
        var instruments = await context.Instruments.AsNoTracking()
            .Where(item => instrumentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Symbol, cancellationToken);
        var links = await context.AiAnalysisEvidence.AsNoTracking()
            .Where(link => analysisIds.Contains(link.AiAnalysisId))
            .OrderBy(link => link.EvidenceRecordId)
            .ToArrayAsync(cancellationToken);
        var evidence = links.GroupBy(link => link.AiAnalysisId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)[.. group.Select(link => link.EvidenceRecordId)]);

        return [.. analyses.Select(item =>
        {
            var status = Parse(item.Status, AiInterpretationExecutionStatus.Failed);
            var structured = status == AiInterpretationExecutionStatus.Completed && item.Output is not null
                ? JsonSerializer.Deserialize<StructuredAiInterpretation>(item.Output, SerializerOptions)
                : null;
            return new AiInterpretationResult(
                item.Id,
                item.InstrumentId.HasValue && instruments.TryGetValue(item.InstrumentId.Value, out var symbol)
                    ? symbol
                    : "XAUUSD",
                item.Timeframe,
                Parse(item.Specialist, AiSpecialist.Master),
                Parse(item.AnalysisType, AiInterpretationType.EvidenceSynthesis),
                Parse(item.Direction, AiInterpretationDirection.Unknown),
                Parse(item.Impact, AiInterpretationImpact.Unknown),
                JsonSerializer.Deserialize<string[]>(item.AffectedAssetsJson, SerializerOptions) ?? [],
                item.Mechanism,
                item.ExpectedEffect,
                item.ObservedReaction,
                Parse(item.ReactionAlignment, AiReactionAlignment.Unknown),
                Parse(item.CurrentRelevance, AiCurrentRelevance.Unknown),
                item.Confidence,
                item.Uncertainty,
                item.Summary,
                item.Provider,
                item.Model,
                item.PromptVersion,
                item.AnalysisVersion,
                item.EvidenceVersion,
                item.AnalysisTimeUtc,
                item.EvidenceUpdatedAtUtc,
                item.CreatedAtUtc,
                item.CompletedAtUtc,
                status,
                Parse(item.LifecycleStatus, AiInterpretationLifecycle.Invalid),
                false,
                item.InputTokens,
                item.OutputTokens,
                item.LatencyMilliseconds,
                item.ErrorCode,
                item.ErrorMessage,
                evidence.GetValueOrDefault(item.Id, []),
                structured);
        })];
    }

    private static IReadOnlyList<AiEvidenceRelation> RelationsFor(
        Guid id,
        IReadOnlyDictionary<Guid, IReadOnlyList<AiEvidenceRelation>> direct,
        IReadOnlyList<AiEvidenceRelation> all) =>
        [.. direct.GetValueOrDefault(id, [])
            .Concat(all.Where(relation => relation.RelatedEvidenceId == id))
            .Distinct()];

    private static string[] RelevantEvidenceTypes(AiSpecialist specialist) => specialist switch
    {
        AiSpecialist.News => ["News", "Economic", "Analyst", "Market"],
        AiSpecialist.Candle => ["Market", "Candle", "Technical"],
        AiSpecialist.Structure => ["Market", "Technical", "Session"],
        AiSpecialist.Liquidity => ["Market", "Technical", "Liquidity"],
        AiSpecialist.Flow => ["Market", "CandleFlow", "OrderFlow"],
        AiSpecialist.Ktr => ["Market", "Technical"],
        AiSpecialist.Risk => ["News", "Economic", "Analyst", "Market", "Technical", "Liquidity"],
        _ => ["Market", "Technical", "Candle", "CandleFlow", "Liquidity", "OrderFlow", "Session", "News", "Economic", "Analyst"]
    };

    private static TEnum Parse<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;

    private static int Pages(int total, int pageSize) => total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
}
