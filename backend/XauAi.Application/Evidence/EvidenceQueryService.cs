using Microsoft.Extensions.Logging;

namespace XauAi.Application.Evidence;

internal sealed class EvidenceQueryService(
    IEvidenceStore store,
    EvidenceSettings settings,
    TimeProvider timeProvider,
    ILogger<EvidenceQueryService> logger) : IEvidenceQueryService
{
    public Task<PagedEvidence> GetEvidenceAsync(
        EvidenceQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        var now = timeProvider.GetUtcNow();
        var asOf = ValidateAnalysisTime(query.AsOfUtc, now);
        var instrument = NormalizeOptionalInstrument(query.Instrument);
        var timeframe = NormalizeOptionalTimeframe(query.Timeframe);
        ValidateRange(query.FromUtc, query.ToUtc, settings.MaximumQueryRangeDays);
        return store.QueryAsync(
            new EvidenceStoreQuery(
                instrument,
                query.EvidenceType,
                query.SourceType,
                query.FromUtc?.ToUniversalTime(),
                query.ToUtc?.ToUniversalTime(),
                timeframe,
                query.Direction,
                query.Importance,
                query.RelevantOnly,
                asOf,
                query.Page,
                query.PageSize),
            cancellationToken);
    }

    public async Task<EvidenceDetailResult> GetEvidenceAsync(
        Guid id,
        DateTimeOffset? asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var asOf = ValidateAnalysisTime(asOfUtc, timeProvider.GetUtcNow());
        var evidence = await store.GetAsync(id, asOf, cancellationToken)
            ?? throw new EvidenceException(EvidenceErrorCodes.NotFound, "The requested evidence record was not found.");
        var relations = await store.GetRelationsAsync(id, cancellationToken);
        var clusters = await store.GetClustersAsync(id, cancellationToken);
        return new EvidenceDetailResult(evidence, relations, clusters);
    }

    public async Task<EvidencePack> GetPackAsync(
        EvidencePackRequest request,
        CancellationToken cancellationToken = default)
    {
        var instrument = EvidenceNormalization.NormalizeInstrument(request.Instrument)
            ?? throw Invalid("The evidence-pack instrument must be a supported XAUUSD alias.");
        var primary = EvidenceNormalization.NormalizeTimeframe(request.PrimaryTimeframe)
            ?? throw Invalid("The evidence-pack primary timeframe is invalid.");
        var confirmation = string.IsNullOrWhiteSpace(request.ConfirmationTimeframe)
            ? DefaultConfirmation(primary)
            : EvidenceNormalization.NormalizeTimeframe(request.ConfirmationTimeframe)
                ?? throw Invalid("The evidence-pack confirmation timeframe is invalid.");
        if (confirmation == primary)
        {
            throw Invalid("The confirmation timeframe must differ from the primary timeframe.");
        }

        var analysisTime = ValidateAnalysisTime(request.AnalysisTimeUtc, timeProvider.GetUtcNow());
        var lookbackDays = request.LookbackDays ?? settings.DefaultPackLookbackDays;
        if (lookbackDays is < 1 || lookbackDays > settings.MaximumPackLookbackDays)
        {
            throw Invalid($"Evidence-pack lookback must be between 1 and {settings.MaximumPackLookbackDays} days.");
        }

        var from = analysisTime.AddDays(-lookbackDays);
        var items = await store.QueryPackAsync(
            instrument,
            from,
            analysisTime,
            settings.MaximumPackItemsPerType,
            cancellationToken);
        var relevant = items.Where(item => item.IsRelevant).ToArray();
        var conflicts = DetectConflicts(relevant);
        var groups = Enum.GetValues<EvidenceType>()
            .Select(type => new EvidenceTypeGroup(
                type,
                relevant.Where(item => item.EvidenceType == type).ToArray()))
            .ToArray();
        var timeframes = relevant
            .Where(item => item.Timeframe is not null
                && item.EvidenceType is EvidenceType.Market
                    or EvidenceType.Candle
                    or EvidenceType.Technical
                    or EvidenceType.CandleFlow
                    or EvidenceType.Liquidity
                    or EvidenceType.OrderFlow
                    or EvidenceType.Session)
            .GroupBy(item => item.Timeframe!, StringComparer.Ordinal)
            .OrderBy(group => TimeframeOrder(group.Key))
            .Select(group => new EvidenceTimeframeGroup(
                group.Key,
                TimeframeRole(group.Key, primary, confirmation),
                group.OrderByDescending(item => item.AvailableAtUtc).ToArray()))
            .ToArray();
        var coverage = groups.Select(group => new EvidenceCoverage(
                group.EvidenceType,
                CoverageState(group.Items),
                group.Items.Count,
                group.Items.Count(item => item.Completeness == EvidenceCompleteness.Complete),
                group.Items.Count(item => item.Completeness == EvidenceCompleteness.Partial)))
            .ToArray();

        logger.LogInformation(
            "Evidence pack requested for {Instrument} at {AnalysisTimeUtc} with primary {PrimaryTimeframe}: {RecordCount} records and {ConflictsDetected} conflicts",
            instrument,
            analysisTime,
            primary,
            relevant.Length,
            conflicts.Count);
        return new EvidencePack(
            instrument,
            analysisTime,
            from,
            primary,
            confirmation,
            groups,
            timeframes,
            conflicts,
            coverage);
    }

    public async Task<IReadOnlyList<EvidenceConflictResult>> GetConflictsAsync(
        EvidenceConflictQuery query,
        CancellationToken cancellationToken = default)
    {
        var instrument = EvidenceNormalization.NormalizeInstrument(query.Instrument)
            ?? throw Invalid("The evidence conflict instrument must be a supported XAUUSD alias.");
        var timeframe = NormalizeOptionalTimeframe(query.Timeframe);
        var asOf = ValidateAnalysisTime(query.AsOfUtc, timeProvider.GetUtcNow());
        var from = query.FromUtc?.ToUniversalTime() ?? asOf.AddDays(-settings.DefaultPackLookbackDays);
        var to = query.ToUtc?.ToUniversalTime() ?? asOf;
        ValidateRange(from, to, settings.MaximumQueryRangeDays);
        var items = await store.QueryPackAsync(
            instrument,
            from,
            asOf,
            settings.MaximumPackItemsPerType,
            cancellationToken);
        return DetectConflicts(items.Where(item => timeframe is null || item.Timeframe == timeframe).ToArray());
    }

    public Task<PagedEvidenceSources> GetSourcesAsync(
        EvidenceSourceQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        var asOf = ValidateAnalysisTime(query.AsOfUtc, timeProvider.GetUtcNow());
        return store.QuerySourcesAsync(query, asOf, cancellationToken);
    }

    private IReadOnlyList<EvidenceConflictResult> DetectConflicts(IReadOnlyList<EvidenceResult> items)
    {
        var result = new List<EvidenceConflictResult>();
        var directional = items.Where(item => item.Direction is EvidenceDirection.Bullish or EvidenceDirection.Bearish)
            .GroupBy(item => new { item.Instrument, item.Timeframe, item.Category });
        foreach (var group in directional)
        {
            var bullish = group.Where(item => item.Direction == EvidenceDirection.Bullish).ToArray();
            var bearish = group.Where(item => item.Direction == EvidenceDirection.Bearish).ToArray();
            foreach (var first in bullish)
            {
                foreach (var second in bearish.Where(item =>
                             item.SourceKey != first.SourceKey
                             && Math.Abs((item.AvailableAtUtc - first.AvailableAtUtc).TotalHours) <= settings.ConflictWindowHours))
                {
                    result.Add(new EvidenceConflictResult(
                        first.Id,
                        second.Id,
                        first.Instrument,
                        first.Timeframe,
                        first.Category,
                        first.Direction,
                        second.Direction,
                        "Independent sources express opposite normalized directions within the deterministic conflict window."));
                    if (result.Count >= settings.MaximumConflicts)
                    {
                        return result;
                    }
                }
            }
        }

        return result;
    }

    private static EvidenceCoverageState CoverageState(IReadOnlyList<EvidenceResult> items) =>
        items.Count == 0 ? EvidenceCoverageState.Unavailable
        : items.Any(item => item.Completeness == EvidenceCompleteness.Partial
            || item.Quality is EvidenceQuality.Low or EvidenceQuality.Unknown)
            ? EvidenceCoverageState.Partial
            : EvidenceCoverageState.Available;

    private static string? DefaultConfirmation(string primary) => primary switch
    {
        "M5" => "M1",
        "M15" => "M5",
        "M30" => "M15",
        "H1" => "M15",
        "H4" => "H1",
        "D1" => "H4",
        "W1" => "D1",
        "MN1" => "W1",
        _ => null
    };

    private static EvidenceTimeframeRole TimeframeRole(string timeframe, string primary, string? confirmation) =>
        timeframe == primary ? EvidenceTimeframeRole.Primary
        : timeframe == confirmation ? EvidenceTimeframeRole.Confirmation
        : EvidenceTimeframeRole.Context;

    private static int TimeframeOrder(string timeframe) => timeframe switch
    {
        "MN1" => 0,
        "W1" => 1,
        "D1" => 2,
        "H4" => 3,
        "H1" => 4,
        "M30" => 5,
        "M15" => 6,
        "M5" => 7,
        "M1" => 8,
        _ => 9
    };

    private static DateTimeOffset ValidateAnalysisTime(DateTimeOffset? requested, DateTimeOffset now)
    {
        var value = (requested ?? now).ToUniversalTime();
        if (value > now.ToUniversalTime())
        {
            throw Invalid("Evidence analysis time cannot be in the future.");
        }

        return value;
    }

    private static void ValidateRange(DateTimeOffset? from, DateTimeOffset? to, int maximumDays)
    {
        if (from.HasValue && to.HasValue)
        {
            if (from.Value >= to.Value)
            {
                throw Invalid("Evidence query start must be before the end.");
            }

            if (to.Value - from.Value > TimeSpan.FromDays(maximumDays))
            {
                throw Invalid($"Evidence query range cannot exceed {maximumDays} days.");
            }
        }
    }

    private void ValidatePage(int page, int pageSize)
    {
        if (page < 1)
        {
            throw Invalid("Evidence page must be at least 1.");
        }

        if (pageSize is < 1 || pageSize > settings.MaximumPageSize)
        {
            throw Invalid($"Evidence page size must be between 1 and {settings.MaximumPageSize}.");
        }
    }

    private static string? NormalizeOptionalInstrument(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : EvidenceNormalization.NormalizeInstrument(value)
                ?? throw Invalid("The evidence instrument filter must be a supported XAUUSD alias.");

    private static string? NormalizeOptionalTimeframe(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : EvidenceNormalization.NormalizeTimeframe(value)
                ?? throw Invalid("The evidence timeframe filter is invalid.");

    private static EvidenceException Invalid(string message) =>
        new(EvidenceErrorCodes.InvalidRequest, message);
}
