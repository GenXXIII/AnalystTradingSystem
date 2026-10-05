using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace XauAi.Application.Analysts;

internal sealed class AnalystSynchronizationService(
    IAnalystDataProvider provider,
    IAnalystItemNormalizer normalizer,
    IAnalystIngestionStore ingestionStore,
    IAnalystSyncStateStore stateStore,
    IAnalystRetryDelay retryDelay,
    AnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<AnalystSynchronizationService> logger) : IAnalystSynchronizationService
{
    private static readonly SemaphoreSlim SynchronizationGate = new(1, 1);

    public async Task<AnalystSyncResult> SynchronizeAsync(
        AnalystSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
        {
            throw new AnalystException(
                AnalystErrorCodes.Disabled,
                "Analyst-data synchronization is disabled by configuration.");
        }

        await SynchronizationGate.WaitAsync(cancellationToken);
        try
        {
            return await SynchronizeCoreAsync(request, cancellationToken);
        }
        finally
        {
            SynchronizationGate.Release();
        }
    }

    private async Task<AnalystSyncResult> SynchronizeCoreAsync(
        AnalystSyncRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var state = await stateStore.GetAsync(settings.ProviderKey, cancellationToken);
        var toUtc = (request.ToUtc ?? now).ToUniversalTime();
        var fromUtc = (request.FromUtc
            ?? state?.LastSuccessfulSyncAtUtc?.Subtract(TimeSpan.FromMinutes(settings.CollectionOverlapMinutes))
            ?? toUtc.Subtract(TimeSpan.FromDays(settings.InitialLookbackDays))).ToUniversalTime();
        ValidateWindow(fromUtc, toUtc, now);

        var startedAtUtc = timeProvider.GetUtcNow();
        var runId = await stateStore.StartAsync(
            settings.ProviderKey,
            fromUtc,
            toUtc,
            startedAtUtc,
            cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        var attempts = new ProviderAttemptMetrics();
        var received = 0;
        var skipped = 0;
        var rejected = 0;
        var normalized = new List<NormalizedAnalystItem>();

        logger.LogInformation(
            "Analyst synchronization {RunId} started for provider {Provider} from {FromUtc} to {ToUtc}; traceId {TraceId}",
            runId,
            settings.Provider,
            fromUtc,
            toUtc,
            Activity.Current?.TraceId.ToString() ?? "background");

        try
        {
            string? cursor = null;
            var seenCursors = new HashSet<string>(StringComparer.Ordinal);
            for (var pageNumber = 0; pageNumber < settings.MaximumPagesPerSync; pageNumber++)
            {
                var providerRequest = new AnalystProviderRequest(
                    fromUtc,
                    toUtc,
                    settings.ProviderPageSize,
                    cursor);
                var page = await FetchWithRetryAsync(providerRequest, attempts, cancellationToken);
                received += page.Items.Count;
                foreach (var providerItem in page.Items)
                {
                    var result = normalizer.Normalize(providerItem, timeProvider.GetUtcNow());
                    if (result.Item is null)
                    {
                        rejected++;
                        logger.LogDebug(
                            "Analyst provider item was rejected during deterministic normalization: {RejectionReason}",
                            result.RejectionReason);
                        continue;
                    }

                    if (result.Item.PublishedAtUtc < fromUtc || result.Item.PublishedAtUtc > toUtc)
                    {
                        skipped++;
                        continue;
                    }

                    normalized.Add(result.Item);
                }

                cursor = page.NextCursor;
                if (string.IsNullOrWhiteSpace(cursor) || !seenCursors.Add(cursor))
                {
                    break;
                }

                if (pageNumber == settings.MaximumPagesPerSync - 1)
                {
                    throw new AnalystException(
                        AnalystErrorCodes.InvalidResponse,
                        "The analyst provider result exceeded the configured pagination safety limit.");
                }
            }

            var persistence = await ingestionStore.PersistAsync(normalized, cancellationToken);
            skipped += persistence.Duplicates;
            stopwatch.Stop();
            var metrics = new AnalystRunMetrics(
                attempts.RequestsMade,
                attempts.RateLimitResponses,
                received,
                persistence.PublicationsInserted,
                persistence.PredictionsInserted,
                skipped,
                rejected,
                stopwatch.ElapsedMilliseconds);
            await stateStore.CompleteAsync(
                runId,
                settings.ProviderKey,
                timeProvider.GetUtcNow(),
                persistence.LatestPublishedAtUtc,
                persistence.LastExternalId,
                metrics,
                cancellationToken);
            logger.LogInformation(
                "Analyst synchronization {RunId} completed in {DurationMilliseconds} ms with {RequestCount} requests: {Received} received, {PublicationsInserted} publications and {PredictionsInserted} predictions inserted, {Skipped} skipped, {Rejected} rejected",
                runId,
                stopwatch.ElapsedMilliseconds,
                attempts.RequestsMade,
                received,
                persistence.PublicationsInserted,
                persistence.PredictionsInserted,
                skipped,
                rejected);
            return new AnalystSyncResult(
                runId,
                settings.Provider,
                fromUtc,
                toUtc,
                attempts.RequestsMade,
                attempts.RateLimitResponses,
                received,
                persistence.PublicationsInserted,
                persistence.PredictionsInserted,
                skipped,
                rejected,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var analystException = exception as AnalystException;
            var errorCode = analystException?.Code ?? AnalystErrorCodes.ProviderUnavailable;
            var safeMessage = analystException?.SafeMessage
                ?? "Analyst synchronization failed safely and existing analyst data remains available.";
            var metrics = new AnalystRunMetrics(
                attempts.RequestsMade,
                attempts.RateLimitResponses,
                received,
                0,
                0,
                skipped,
                rejected,
                stopwatch.ElapsedMilliseconds);
            try
            {
                await stateStore.FailAsync(
                    runId,
                    settings.ProviderKey,
                    timeProvider.GetUtcNow(),
                    metrics,
                    errorCode,
                    safeMessage,
                    cancellationToken);
            }
            catch (Exception stateException)
            {
                logger.LogError(stateException, "Analyst synchronization state could not record failure for run {RunId}", runId);
            }

            logger.LogWarning(
                exception,
                "Analyst synchronization {RunId} failed safely with code {ErrorCode} after {RequestCount} requests",
                runId,
                errorCode,
                attempts.RequestsMade);
            throw analystException ?? new AnalystException(
                AnalystErrorCodes.ProviderUnavailable,
                safeMessage,
                transient: true,
                innerException: exception);
        }
    }

    private async Task<AnalystProviderPage> FetchWithRetryAsync(
        AnalystProviderRequest request,
        ProviderAttemptMetrics metrics,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            metrics.RequestsMade++;
            try
            {
                return request.FromUtc < timeProvider.GetUtcNow().AddDays(-1)
                    ? await provider.GetHistoricalAnalystItemsAsync(request, cancellationToken)
                    : await provider.GetLatestAnalystItemsAsync(request, cancellationToken);
            }
            catch (AnalystException exception)
            {
                if (exception.Code == AnalystErrorCodes.RateLimited)
                {
                    metrics.RateLimitResponses++;
                }

                if (!exception.IsTransient || attempt >= settings.MaxRetries)
                {
                    throw;
                }

                var exponentialSeconds = settings.RetryBaseDelaySeconds * Math.Pow(2, attempt);
                var delay = exception.RetryAfter ?? TimeSpan.FromSeconds(exponentialSeconds);
                logger.LogWarning(
                    "Transient analyst provider failure {ErrorCode}; retry {RetryNumber} of {MaximumRetries} after {DelayMilliseconds} ms",
                    exception.Code,
                    attempt + 1,
                    settings.MaxRetries,
                    delay.TotalMilliseconds);
                await retryDelay.DelayAsync(delay, cancellationToken);
            }
        }
    }

    private void ValidateWindow(DateTimeOffset fromUtc, DateTimeOffset toUtc, DateTimeOffset nowUtc)
    {
        if (fromUtc >= toUtc)
        {
            throw new AnalystException(
                AnalystErrorCodes.InvalidRequest,
                "The analyst collection start must be before the end.");
        }

        if (toUtc > nowUtc.AddMinutes(1))
        {
            throw new AnalystException(
                AnalystErrorCodes.InvalidRequest,
                "The analyst collection end cannot be in the future.");
        }

        if (toUtc - fromUtc > TimeSpan.FromDays(settings.MaximumCollectionRangeDays))
        {
            throw new AnalystException(
                AnalystErrorCodes.InvalidRequest,
                $"The analyst collection range cannot exceed {settings.MaximumCollectionRangeDays} days.");
        }
    }

    private sealed class ProviderAttemptMetrics
    {
        public int RequestsMade { get; set; }

        public int RateLimitResponses { get; set; }
    }
}
