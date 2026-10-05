using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace XauAi.Application.MarketData;

internal sealed class MarketDataSynchronizationService(
    IMarketDataProvider provider,
    IMarketCandleStore candleStore,
    IMarketDataQueryStore queryStore,
    IMarketDataSyncStateStore stateStore,
    IMarketSessionCalendar sessionCalendar,
    MarketDataPipelineSettings settings,
    TimeProvider timeProvider,
    ILogger<MarketDataSynchronizationService> logger) : IMarketDataSynchronizationService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SynchronizationLocks =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<MarketDataPipelineResult> SynchronizeAsync(
        MarketDataSynchronizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var synchronizationLock = SynchronizationLocks.GetOrAdd(
            $"{request.Symbol}:{request.Timeframe.Code()}",
            _ => new SemaphoreSlim(1, 1));
        await synchronizationLock.WaitAsync(cancellationToken);

        try
        {
            return await SynchronizeExclusiveAsync(request, cancellationToken);
        }
        finally
        {
            synchronizationLock.Release();
        }
    }

    private async Task<MarketDataPipelineResult> SynchronizeExclusiveAsync(
        MarketDataSynchronizationRequest request,
        CancellationToken cancellationToken)
    {
        var requestedToUtc = request.ToUtc.ToUniversalTime();
        var requestedFromUtc = await ResolveFromUtcAsync(request, requestedToUtc, cancellationToken);
        MarketDataRequestValidation.Validate(request.Symbol, requestedFromUtc, requestedToUtc);

        var startedAtUtc = timeProvider.GetUtcNow();
        var runId = await stateStore.StartRunAsync(
            request.Symbol,
            request.Timeframe,
            requestedFromUtc,
            requestedToUtc,
            startedAtUtc,
            cancellationToken);
        var progress = new MutableProgress();

        logger.LogInformation(
            "Market-data synchronization {RunId} started for {Symbol} {Timeframe} from {FromUtc} to {ToUtc}",
            runId,
            request.Symbol,
            request.Timeframe.Code(),
            requestedFromUtc,
            requestedToUtc);

        try
        {
            var cursor = requestedFromUtc;
            var duration = request.Timeframe.Duration();
            var batchSpan = TimeSpan.FromTicks(checked(duration.Ticks * (settings.BatchSize - 1L)));

            while (cursor <= requestedToUtc)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batchToUtc = cursor.Add(batchSpan);
                if (batchToUtc > requestedToUtc)
                {
                    batchToUtc = requestedToUtc;
                }

                // The provider contract requires from < to. A one-second envelope retrieves
                // the single final aligned candle when a batch boundary lands exactly on ToUtc.
                var providerToUtc = batchToUtc == cursor ? cursor.AddSeconds(1) : batchToUtc;
                var received = await RetrieveWithRetryAsync(
                    request.Symbol,
                    request.Timeframe,
                    cursor,
                    providerToUtc,
                    cancellationToken);
                progress.BatchesProcessed++;
                progress.Received += received.Count;

                var validated = new List<MarketCandleSnapshot>(received.Count);
                var rejectionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var candle in received)
                {
                    var outcome = MarketCandleValidation.ValidateAndNormalize(
                        candle,
                        request.Symbol,
                        request.Timeframe,
                        cursor,
                        batchToUtc,
                        timeProvider.GetUtcNow());
                    if (!outcome.IsValid || outcome.Candle is null)
                    {
                        progress.Rejected++;
                        rejectionCounts[outcome.ErrorCode ?? "UNKNOWN"] =
                            rejectionCounts.GetValueOrDefault(outcome.ErrorCode ?? "UNKNOWN") + 1;
                        continue;
                    }

                    if (!request.IncludeFormingCandle && !outcome.Candle.IsComplete)
                    {
                        progress.Skipped++;
                        continue;
                    }

                    validated.Add(outcome.Candle);
                }

                if (rejectionCounts.Count > 0)
                {
                    logger.LogWarning(
                        "Market-data synchronization {RunId} rejected {RejectedCount} records with reasons {Reasons}",
                        runId,
                        rejectionCounts.Values.Sum(),
                        string.Join(",", rejectionCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}")));
                }

                var unique = validated
                    .OrderBy(candle => candle.OpenTimeUtc)
                    .DistinctBy(candle => candle.OpenTimeUtc)
                    .ToArray();
                progress.Skipped += validated.Count - unique.Length;
                progress.Accepted += unique.Length;

                var saveResult = await candleStore.SaveAsync(unique, cancellationToken);
                progress.Inserted += saveResult.Inserted;
                progress.Updated += saveResult.Updated;
                progress.Skipped += saveResult.Skipped;

                if (batchToUtc >= requestedToUtc)
                {
                    break;
                }

                cursor = batchToUtc.Add(duration);
            }

            var latest = await candleStore.GetLatestAsync(
                request.Symbol,
                request.Timeframe,
                requestedToUtc,
                cancellationToken);
            var openTimes = await queryStore.GetOpenTimesAsync(
                request.Symbol,
                request.Timeframe,
                requestedFromUtc,
                requestedToUtc,
                cancellationToken);
            var gaps = MarketDataGapDetector.Detect(
                request.Symbol,
                request.Timeframe,
                openTimes,
                sessionCalendar,
                settings.MaxGapResults);
            var completedAtUtc = timeProvider.GetUtcNow();
            var result = new MarketDataPipelineResult(
                runId,
                request.Symbol,
                request.Timeframe,
                requestedFromUtc,
                requestedToUtc,
                latest?.OpenTimeUtc,
                progress.BatchesProcessed,
                progress.Received,
                progress.Accepted,
                progress.Inserted,
                progress.Updated,
                progress.Skipped,
                progress.Rejected,
                gaps.Count,
                Math.Max(0, (long)(completedAtUtc - startedAtUtc).TotalMilliseconds));

            await stateStore.CompleteRunAsync(runId, result, completedAtUtc, cancellationToken);
            logger.LogInformation(
                "Market-data synchronization {RunId} completed in {DurationMilliseconds} ms: {Received} received, {Inserted} inserted, {Updated} updated, {Skipped} skipped, {Rejected} rejected, {GapCount} candidate gaps",
                runId,
                result.DurationMilliseconds,
                result.Received,
                result.Inserted,
                result.Updated,
                result.Skipped,
                result.Rejected,
                result.DetectedGaps);
            return result;
        }
        catch (OperationCanceledException)
        {
            await RecordFailureAsync(
                runId,
                progress,
                "MARKET_DATA_SYNC_CANCELLED",
                "Market-data synchronization was interrupted.",
                CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            var (code, message) = exception is MarketDataException marketDataException
                ? (marketDataException.Code, marketDataException.SafeMessage)
                : (MarketDataErrorCodes.SynchronizationFailed, "Market-data synchronization failed.");
            await RecordFailureAsync(runId, progress, code, message, CancellationToken.None);
            logger.LogWarning(
                exception,
                "Market-data synchronization {RunId} failed with code {ErrorCode} after {BatchCount} batches",
                runId,
                code,
                progress.BatchesProcessed);
            throw;
        }
    }

    private async Task<DateTimeOffset> ResolveFromUtcAsync(
        MarketDataSynchronizationRequest request,
        DateTimeOffset requestedToUtc,
        CancellationToken cancellationToken)
    {
        if (request.FromUtc is { } explicitFrom)
        {
            return request.Timeframe.AlignDown(explicitFrom);
        }

        var latest = await candleStore.GetLatestAsync(
            request.Symbol,
            request.Timeframe,
            requestedToUtc,
            cancellationToken);
        if (latest is null)
        {
            return request.Timeframe.AlignDown(requestedToUtc.AddDays(-settings.InitialHistoryDays));
        }

        var historyFromUtc = request.Timeframe.AlignDown(
            requestedToUtc.AddDays(-settings.InitialHistoryDays));
        var openTimes = await queryStore.GetOpenTimesAsync(
            request.Symbol,
            request.Timeframe,
            historyFromUtc,
            requestedToUtc,
            cancellationToken);
        var earliestGap = MarketDataGapDetector.Detect(
                request.Symbol,
                request.Timeframe,
                openTimes,
                sessionCalendar,
                settings.MaxGapResults)
            .FirstOrDefault();
        if (earliestGap is not null)
        {
            // Re-read one candle before the first detected gap. This safely overlaps an
            // existing candle and also includes an aligned session-opening candle whose
            // first quote may arrive just after the nominal open (for example 22:01 UTC).
            var recoveryFromUtc = earliestGap.ExpectedOpenTimeUtc.Subtract(request.Timeframe.Duration());
            if (recoveryFromUtc < historyFromUtc)
            {
                recoveryFromUtc = historyFromUtc;
            }

            logger.LogInformation(
                "Market-data synchronization found a stored gap for {Symbol} {Timeframe} at {GapOpenTimeUtc}; recovering from {RecoveryFromUtc}",
                request.Symbol,
                request.Timeframe.Code(),
                earliestGap.ExpectedOpenTimeUtc,
                recoveryFromUtc);
            return recoveryFromUtc;
        }

        return latest.IsComplete
            ? latest.OpenTimeUtc.Add(request.Timeframe.Duration())
            : latest.OpenTimeUtc;
    }

    private async Task<IReadOnlyList<MarketCandleSnapshot>> RetrieveWithRetryAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await provider.GetCandlesAsync(symbol, timeframe, fromUtc, toUtc, cancellationToken);
            }
            catch (MarketDataException exception) when (
                attempt < settings.MaxRetries && IsTransient(exception.Code))
            {
                var delay = TimeSpan.FromSeconds(settings.RetryBaseDelaySeconds * Math.Pow(2, attempt));
                logger.LogWarning(
                    "Market-data provider request failed with code {ErrorCode}; retry {RetryNumber} of {MaxRetries} after {DelaySeconds} seconds",
                    exception.Code,
                    attempt + 1,
                    settings.MaxRetries,
                    delay.TotalSeconds);
                await Task.Delay(delay, timeProvider, cancellationToken);
            }
        }
    }

    private Task RecordFailureAsync(
        Guid runId,
        MutableProgress progress,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken) =>
        stateStore.FailRunAsync(
            runId,
            progress.Snapshot(),
            errorCode,
            safeMessage,
            timeProvider.GetUtcNow(),
            cancellationToken);

    private void ValidateRequest(MarketDataSynchronizationRequest request)
    {
        if (!string.Equals(request.Symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ProviderSymbolNotFound,
                "The requested market symbol is not configured.");
        }

        if (!settings.Timeframes.Contains(request.Timeframe))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                "The requested timeframe is not enabled for synchronization.");
        }

        if (request.FromUtc is { } fromUtc
            && request.ToUtc.ToUniversalTime() - fromUtc.ToUniversalTime() > TimeSpan.FromDays(settings.MaxQueryRangeDays))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.QueryRangeTooLarge,
                $"The synchronization range cannot exceed {settings.MaxQueryRangeDays} days.");
        }
    }

    private static bool IsTransient(string code) => code is
        MarketDataErrorCodes.ProviderConnectionFailed
        or MarketDataErrorCodes.ProviderDataRequestFailed
        or MarketDataErrorCodes.ProviderRateLimited
        or MarketDataErrorCodes.ProviderTimeout
        or MarketDataErrorCodes.ProviderUnavailable;

    private sealed class MutableProgress
    {
        public int BatchesProcessed { get; set; }

        public int Received { get; set; }

        public int Accepted { get; set; }

        public int Inserted { get; set; }

        public int Updated { get; set; }

        public int Skipped { get; set; }

        public int Rejected { get; set; }

        public MarketDataSyncProgress Snapshot() =>
            new(BatchesProcessed, Received, Accepted, Inserted, Updated, Skipped, Rejected);
    }
}
