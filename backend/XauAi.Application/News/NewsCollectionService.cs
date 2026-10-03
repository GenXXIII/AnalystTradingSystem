using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace XauAi.Application.News;

internal sealed class NewsCollectionService(
    INewsProvider provider,
    INewsArticleNormalizer normalizer,
    INewsArticleStore articleStore,
    INewsCollectionStateStore stateStore,
    INewsRetryDelay retryDelay,
    NewsSettings settings,
    TimeProvider timeProvider,
    ILogger<NewsCollectionService> logger) : INewsCollectionService
{
    public async Task<NewsCollectionResult> CollectAsync(
        NewsCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
        {
            throw new NewsException(NewsErrorCodes.Disabled, "News collection is disabled by configuration.");
        }

        var now = timeProvider.GetUtcNow();
        var state = await stateStore.GetAsync(settings.ProviderKey, cancellationToken);
        var toUtc = (request.ToUtc ?? now).ToUniversalTime();
        var fromUtc = (request.FromUtc
            ?? state?.LastSuccessfulCollectionAtUtc?.Subtract(TimeSpan.FromMinutes(settings.CollectionOverlapMinutes))
            ?? toUtc.Subtract(TimeSpan.FromHours(settings.InitialLookbackHours))).ToUniversalTime();
        ValidateWindow(fromUtc, toUtc, now);
        var useArchive = fromUtc < now.AddHours(-48);
        if (useArchive && !settings.ArchiveEnabled)
        {
            throw new NewsException(
                NewsErrorCodes.InvalidRequest,
                "Historical news collection requires archive access to be enabled for the configured provider plan.");
        }

        var startedAtUtc = timeProvider.GetUtcNow();
        var runId = await stateStore.StartAsync(
            settings.ProviderKey,
            fromUtc,
            toUtc,
            startedAtUtc,
            cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        var requestsMade = 0;
        var rateLimitResponses = 0;
        var providerAttempts = new ProviderAttemptMetrics();
        var received = 0;
        var skipped = 0;
        var rejected = 0;
        var normalized = new List<NormalizedNewsArticle>();

        try
        {
            string? pageToken = null;
            var seenPageTokens = new HashSet<string>(StringComparer.Ordinal);
            for (var pageNumber = 0; pageNumber < settings.MaximumPagesPerCollection; pageNumber++)
            {
                var page = await FetchWithRetryAsync(
                    new NewsProviderRequest(
                        fromUtc,
                        toUtc,
                        settings.ProviderQuery,
                        settings.Language,
                        settings.PageSize,
                        pageToken,
                        useArchive),
                    providerAttempts,
                    cancellationToken);
                requestsMade = providerAttempts.RequestsMade;
                rateLimitResponses = providerAttempts.RateLimitResponses;
                received += page.Articles.Count;

                foreach (var providerArticle in page.Articles)
                {
                    var result = normalizer.Normalize(providerArticle, timeProvider.GetUtcNow());
                    if (result.Article is null)
                    {
                        rejected++;
                        continue;
                    }

                    if (result.Article.PublishedAtUtc < fromUtc || result.Article.PublishedAtUtc > toUtc)
                    {
                        skipped++;
                        continue;
                    }

                    if (result.Article.Relevance < settings.MinimumRelevance)
                    {
                        skipped++;
                        continue;
                    }

                    normalized.Add(result.Article);
                }

                pageToken = page.NextPageToken;
                if (string.IsNullOrWhiteSpace(pageToken) || !seenPageTokens.Add(pageToken))
                {
                    break;
                }
            }

            var persistence = await articleStore.PersistAsync(normalized, cancellationToken);
            skipped += persistence.Duplicates;
            stopwatch.Stop();
            var metrics = new NewsRunMetrics(
                requestsMade,
                rateLimitResponses,
                received,
                persistence.Inserted,
                skipped,
                rejected,
                stopwatch.ElapsedMilliseconds);
            await stateStore.CompleteAsync(
                runId,
                settings.ProviderKey,
                timeProvider.GetUtcNow(),
                metrics,
                cancellationToken);
            logger.LogInformation(
                "News collection {RunId} completed in {DurationMilliseconds} ms with {RequestCount} provider requests: {Received} received, {Inserted} inserted, {Skipped} skipped, {Rejected} rejected",
                runId,
                stopwatch.ElapsedMilliseconds,
                requestsMade,
                received,
                persistence.Inserted,
                skipped,
                rejected);
            return new NewsCollectionResult(
                runId,
                settings.Provider,
                fromUtc,
                toUtc,
                requestsMade,
                rateLimitResponses,
                received,
                persistence.Inserted,
                skipped,
                rejected,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            requestsMade = providerAttempts.RequestsMade;
            rateLimitResponses = providerAttempts.RateLimitResponses;
            var newsException = exception as NewsException;
            var errorCode = newsException?.Code ?? NewsErrorCodes.ProviderUnavailable;
            var safeMessage = newsException?.SafeMessage ?? "News collection failed safely and existing news remains available.";
            var metrics = new NewsRunMetrics(
                requestsMade,
                rateLimitResponses,
                received,
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
                logger.LogError(stateException, "News collection state could not record failure for run {RunId}", runId);
            }

            logger.LogWarning(
                exception,
                "News collection {RunId} failed safely with code {ErrorCode} after {RequestCount} provider requests",
                runId,
                errorCode,
                requestsMade);
            throw newsException ?? new NewsException(
                NewsErrorCodes.ProviderUnavailable,
                safeMessage,
                transient: true,
                innerException: exception);
        }
    }

    private async Task<NewsProviderPage> FetchWithRetryAsync(
        NewsProviderRequest request,
        ProviderAttemptMetrics metrics,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            metrics.RequestsMade++;
            try
            {
                return await provider.GetNewsAsync(request, cancellationToken);
            }
            catch (NewsException exception)
            {
                if (exception.Code == NewsErrorCodes.RateLimited)
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
                    "Transient news provider failure {ErrorCode}; retry {RetryNumber} of {MaximumRetries} after {DelayMilliseconds} ms",
                    exception.Code,
                    attempt + 1,
                    settings.MaxRetries,
                    delay.TotalMilliseconds);
                await retryDelay.DelayAsync(delay, cancellationToken);
            }
        }
    }

    private sealed class ProviderAttemptMetrics
    {
        public int RequestsMade { get; set; }

        public int RateLimitResponses { get; set; }
    }

    private void ValidateWindow(DateTimeOffset fromUtc, DateTimeOffset toUtc, DateTimeOffset nowUtc)
    {
        if (fromUtc >= toUtc)
        {
            throw new NewsException(NewsErrorCodes.InvalidRequest, "The news collection start must be before the end.");
        }

        if (toUtc > nowUtc.AddMinutes(1))
        {
            throw new NewsException(NewsErrorCodes.InvalidRequest, "The news collection end cannot be in the future.");
        }

        if (toUtc - fromUtc > TimeSpan.FromDays(settings.MaximumCollectionRangeDays))
        {
            throw new NewsException(
                NewsErrorCodes.InvalidRequest,
                $"The news collection range cannot exceed {settings.MaximumCollectionRangeDays} days.");
        }
    }
}
