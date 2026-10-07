using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.Application.LocalAnalysis;

internal sealed class LocalAnalystService(
    IMarketDataQueryStore marketData,
    ITechnicalAnalysisService technicalAnalysis,
    ILocalSignalEngine engine,
    ILocalSignalStore store,
    IMarketSessionCalendar sessionCalendar,
    LocalAnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<LocalAnalystService> logger) : ILocalAnalystService
{
    public async Task<LocalSignalSnapshot> EvaluateAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        Validate(symbol, timeframe, requireEnabled: true);
        var now = timeProvider.GetUtcNow();
        var source = await marketData.GetLatestAsync(
            settings.Symbol,
            timeframe,
            settings.HistoryLimit,
            completedOnly: true,
            cancellationToken);
        if (source.Count == 0)
        {
            return EmptyNothing(timeframe, "NO_COMPLETED_CANDLES", now);
        }

        var candles = source.OrderBy(candle => candle.OpenTimeUtc).ToArray();
        var latest = candles[^1];
        var checkpoint = await store.GetCheckpointAsync(settings.Symbol, timeframe, cancellationToken);
        if (checkpoint?.LastProcessedCandleTimeUtc == latest.OpenTimeUtc && checkpoint.Snapshot is not null)
        {
            return checkpoint.Snapshot with { IsCached = true };
        }

        var active = await store.GetActiveAsync(settings.Symbol, timeframe, cancellationToken);
        var qualityReason = ValidateDataQuality(candles, latest, now);
        if (qualityReason is not null)
        {
            return await SaveNothingAsync(timeframe, latest, active, qualityReason, now, cancellationToken);
        }

        TechnicalAnalysisResult analysis;
        try
        {
            analysis = await technicalAnalysis.AnalyzeAsync(
                new TechnicalAnalysisRequest(settings.Symbol, timeframe, latest.CloseTimeUtc),
                cancellationToken);
        }
        catch (TechnicalAnalysisException exception) when (exception.Code == TechnicalAnalysisErrorCodes.NoData)
        {
            return await SaveNothingAsync(timeframe, latest, active, "NO_ANALYZABLE_CANDLES", now, cancellationToken);
        }

        if (analysis.Diagnostics.DuplicateCandlesIgnored > 0)
        {
            return await SaveNothingAsync(timeframe, latest, active, "DUPLICATE_CANDLES", now, cancellationToken);
        }

        if (analysis.Diagnostics.InvalidCandlesIgnored > 0)
        {
            return await SaveNothingAsync(timeframe, latest, active, "INVALID_CANDLES", now, cancellationToken);
        }

        if (analysis.Diagnostics.MissingIntervalCount > settings.MaximumAllowedGaps)
        {
            return await SaveNothingAsync(timeframe, latest, active, "MARKET_DATA_GAPS", now, cancellationToken);
        }

        var decision = engine.Evaluate(candles, analysis, settings.Symbol, timeframe, now);
        var request = BuildPersistenceRequest(decision, active, now);
        var saved = await store.SaveAsync(request, cancellationToken);
        logger.LogInformation(
            "Local analyst evaluated {Symbol} {Timeframe} candle {CandleTimeUtc}: {State} score {Score}/{MaxScore}, mutation {Mutation}",
            settings.Symbol,
            timeframe.Code(),
            latest.OpenTimeUtc,
            saved.State,
            saved.Score,
            saved.MaxScore,
            request.Mutation);
        return saved;
    }

    public async Task<LocalSignalSnapshot> GetCurrentAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        Validate(symbol, timeframe, requireEnabled: false);
        var checkpoint = await store.GetCheckpointAsync(settings.Symbol, timeframe, cancellationToken);
        if (checkpoint?.Snapshot is not null)
        {
            return checkpoint.Snapshot;
        }

        var active = await store.GetActiveAsync(settings.Symbol, timeframe, cancellationToken);
        return active ?? EmptyNothing(timeframe, "NOT_EVALUATED", timeProvider.GetUtcNow());
    }

    public async Task<LocalSignalHistoryResult> GetHistoryAsync(
        string symbol,
        MarketTimeframe? timeframe,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 500)
        {
            throw Invalid("The history limit must be between 1 and 500.");
        }

        if (timeframe.HasValue)
        {
            Validate(symbol, timeframe.Value, requireEnabled: false);
        }
        else if (!string.Equals(symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw Invalid("The requested symbol is not configured for the local analyst.");
        }

        var signals = await store.GetHistoryAsync(settings.Symbol, timeframe, limit, cancellationToken);
        return new LocalSignalHistoryResult(settings.Symbol, timeframe, limit, signals);
    }

    public Task<IReadOnlyList<LocalSignalLifecycleItem>> GetLifecycleAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signalId) || signalId.Length > 160)
        {
            throw Invalid("A valid signal identifier is required.");
        }

        return store.GetLifecycleAsync(signalId.Trim(), cancellationToken);
    }

    public Task<IReadOnlyList<LocalSignalChartMarker>> GetChartMarkersAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 500)
        {
            throw Invalid("The chart marker limit must be between 1 and 500.");
        }

        Validate(symbol, timeframe, requireEnabled: false);
        return store.GetChartMarkersAsync(settings.Symbol, timeframe, limit, cancellationToken);
    }

    public async Task<LocalAnalystStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var checkpoints = await store.GetCheckpointsAsync(
            settings.Symbol,
            settings.Timeframes,
            cancellationToken);
        return new LocalAnalystStatus(
            settings.Enabled,
            settings.Symbol,
            settings.ConfigurationVersion,
            settings.EvaluationIntervalSeconds,
            checkpoints);
    }

    private LocalSignalPersistenceRequest BuildPersistenceRequest(
        LocalSignalDecision decision,
        LocalSignalSnapshot? active,
        DateTimeOffset now)
    {
        if (active is null)
        {
            var mutation = decision.State is LocalSignalState.Buy or LocalSignalState.Sell
                ? LocalSignalMutation.Open
                : LocalSignalMutation.None;
            var signalId = mutation == LocalSignalMutation.Open
                ? $"LOCAL-{decision.Symbol}-{decision.Timeframe.Code()}-{decision.SignalCandleTimeUtc:yyyyMMddHHmmss}-{decision.State.ToString().ToUpperInvariant()}"
                : null;
            return new LocalSignalPersistenceRequest(mutation, null, signalId, decision, now);
        }

        var stopReason = StopReason(active, decision, now);
        if (stopReason is not null)
        {
            var stopped = decision with
            {
                State = LocalSignalState.Stop,
                InvalidationPrice = active.InvalidationPrice,
                TargetPrice = active.TargetPrice,
                ValidUntilUtc = active.ValidUntilUtc ?? decision.ValidUntilUtc,
                Reason = stopReason
            };
            return new LocalSignalPersistenceRequest(
                LocalSignalMutation.Stop,
                active.PersistenceId,
                null,
                stopped,
                now);
        }

        var direction = active.OriginDirection ?? active.State;
        var directionalScore = decision.Conditions
            .Where(condition => direction == LocalSignalState.Buy ? condition.LongMatched : condition.ShortMatched)
            .Sum(condition => condition.Weight);
        var continued = decision with
        {
            State = direction,
            Score = directionalScore,
            Confidence = decision.MaxScore == 0m ? 0m : Math.Round(directionalScore / decision.MaxScore, 6),
            InvalidationPrice = active.InvalidationPrice,
            TargetPrice = active.TargetPrice,
            ValidUntilUtc = active.ValidUntilUtc ?? decision.ValidUntilUtc,
            Reason = null
        };
        return new LocalSignalPersistenceRequest(
            LocalSignalMutation.Update,
            active.PersistenceId,
            null,
            continued,
            now);
    }

    private string? StopReason(
        LocalSignalSnapshot active,
        LocalSignalDecision decision,
        DateTimeOffset now)
    {
        if (active.ValidUntilUtc.HasValue && now >= active.ValidUntilUtc.Value)
        {
            return "EXPIRED";
        }

        if (active.OriginDirection == LocalSignalState.Buy)
        {
            if (active.TargetPrice.HasValue && decision.SignalPrice >= active.TargetPrice.Value)
            {
                return "TARGET_REACHED";
            }

            if (active.InvalidationPrice.HasValue && decision.SignalPrice <= active.InvalidationPrice.Value)
            {
                return "INVALIDATION_LEVEL_BREACHED";
            }

            var opposing = decision.Conditions.Where(condition => condition.ShortMatched).Sum(condition => condition.Weight);
            if (opposing >= settings.StopOpposingScoreThreshold)
            {
                return "OPPOSING_SELL_CONFLUENCE";
            }
        }
        else if (active.OriginDirection == LocalSignalState.Sell)
        {
            if (active.TargetPrice.HasValue && decision.SignalPrice <= active.TargetPrice.Value)
            {
                return "TARGET_REACHED";
            }

            if (active.InvalidationPrice.HasValue && decision.SignalPrice >= active.InvalidationPrice.Value)
            {
                return "INVALIDATION_LEVEL_BREACHED";
            }

            var opposing = decision.Conditions.Where(condition => condition.LongMatched).Sum(condition => condition.Weight);
            if (opposing >= settings.StopOpposingScoreThreshold)
            {
                return "OPPOSING_BUY_CONFLUENCE";
            }
        }

        return null;
    }

    private async Task<LocalSignalSnapshot> SaveNothingAsync(
        MarketTimeframe timeframe,
        StoredMarketCandle latest,
        LocalSignalSnapshot? active,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var decision = new LocalSignalDecision(
            settings.Symbol,
            timeframe,
            $"{settings.Symbol}-{timeframe.Code()}-{latest.OpenTimeUtc:yyyyMMddHHmmss}",
            latest.OpenTimeUtc,
            latest.CloseTimeUtc,
            LocalSignalState.Nothing,
            latest.Close,
            0m,
            settings.MaximumScore,
            0m,
            "Unavailable",
            "Unavailable",
            "Unavailable",
            "Unavailable",
            "Unavailable",
            "Unavailable",
            active?.InvalidationPrice,
            active?.TargetPrice,
            reason,
            active?.ValidUntilUtc ?? latest.CloseTimeUtc,
            [new LocalSignalCondition("DataQuality", reason, false, false, 0m, [reason])]);
        return await store.SaveAsync(
            new LocalSignalPersistenceRequest(LocalSignalMutation.None, active?.PersistenceId, null, decision, now),
            cancellationToken);
    }

    private string? ValidateDataQuality(
        IReadOnlyList<StoredMarketCandle> candles,
        StoredMarketCandle latest,
        DateTimeOffset now)
    {
        if (candles.Count < settings.MinimumCandles)
        {
            return "INSUFFICIENT_HISTORY";
        }

        if (candles.GroupBy(candle => candle.OpenTimeUtc).Any(group => group.Count() > 1))
        {
            return "DUPLICATE_CANDLES";
        }

        if (candles.Any(candle => !IsValid(candle)))
        {
            return "INVALID_OR_INCOMPLETE_CANDLE";
        }

        if (candles.Any(candle =>
                !string.Equals(candle.Symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase)
                || candle.Timeframe != latest.Timeframe))
        {
            return "MIXED_MARKET_DATA";
        }

        if (sessionCalendar.IsPotentiallyOpen(settings.Symbol, now)
            && now - latest.CloseTimeUtc > TimeSpan.FromTicks(latest.Timeframe.Duration().Ticks * settings.StaleAfterIntervals))
        {
            return "STALE_MARKET_DATA";
        }

        return null;
    }

    private static bool IsValid(StoredMarketCandle candle) =>
        candle.IsComplete
        && candle.OpenTimeUtc < candle.CloseTimeUtc
        && candle.Open > 0m
        && candle.High >= Math.Max(candle.Open, candle.Close)
        && candle.Low > 0m
        && candle.Low <= Math.Min(candle.Open, candle.Close)
        && candle.High >= candle.Low;

    private LocalSignalSnapshot EmptyNothing(
        MarketTimeframe timeframe,
        string reason,
        DateTimeOffset now) =>
        new(
            null,
            settings.Symbol,
            timeframe,
            null,
            null,
            LocalSignalState.Nothing,
            null,
            null,
            0m,
            settings.MaximumScore,
            0m,
            "Unavailable",
            "Unavailable",
            "Unavailable",
            "Unavailable",
            "Unavailable",
            "Unavailable",
            null,
            null,
            reason,
            null,
            "NONE",
            settings.ConfigurationVersion,
            [new LocalSignalCondition("DataQuality", reason, false, false, 0m, [reason])],
            now,
            null,
            null,
            null,
            false);

    private void Validate(string symbol, MarketTimeframe timeframe, bool requireEnabled)
    {
        if (requireEnabled && !settings.Enabled)
        {
            throw new LocalAnalystException(
                LocalAnalystErrorCodes.Disabled,
                "The local analyst is disabled by configuration.");
        }

        if (!string.Equals(symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw Invalid("The requested symbol is not configured for the local analyst.");
        }

        if (!settings.Timeframes.Contains(timeframe))
        {
            throw Invalid("The requested timeframe is not enabled for the local analyst.");
        }
    }

    private static LocalAnalystException Invalid(string message) =>
        new(LocalAnalystErrorCodes.InvalidRequest, message);
}
