using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace XauAi.Application.EconomicData;

internal sealed class EconomicDataSynchronizationService(
    IEconomicDataProvider provider,
    IEconomicSeriesStore seriesStore,
    IEconomicObservationStore observationStore,
    IEconomicSyncStateStore stateStore,
    IEconomicRetryDelay retryDelay,
    EconomicDataSettings settings,
    TimeProvider timeProvider,
    ILogger<EconomicDataSynchronizationService> logger) : IEconomicDataSynchronizationService
{
    private static readonly SemaphoreSlim SynchronizationGate = new(1, 1);

    public async Task<EconomicSyncResult> SynchronizeAsync(
        EconomicSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.Disabled,
                "Economic-data synchronization is disabled by configuration.");
        }

        var definitions = SelectDefinitions(request.ExternalSeriesId);
        await SynchronizationGate.WaitAsync(cancellationToken);
        try
        {
            var results = new List<EconomicSeriesSyncResult>(definitions.Count);
            foreach (var definition in definitions)
            {
                results.Add(await SynchronizeSeriesAsync(definition, request, cancellationToken));
            }

            return new EconomicSyncResult(
                results,
                results.Count(result => result.Status == "Succeeded"),
                results.Count(result => result.Status == "Failed"),
                results.Sum(result => result.RequestsMade),
                results.Sum(result => result.RecordsReceived),
                results.Sum(result => result.RecordsInserted),
                results.Sum(result => result.RecordsUpdated),
                results.Sum(result => result.RecordsSkipped));
        }
        finally
        {
            SynchronizationGate.Release();
        }
    }

    private async Task<EconomicSeriesSyncResult> SynchronizeSeriesAsync(
        EconomicSeriesDefinition definition,
        EconomicSyncRequest request,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = timeProvider.GetUtcNow();
        var stopwatch = Stopwatch.StartNew();
        var attempts = new ProviderAttempts();
        var recordsReceived = 0;
        var recordsInserted = 0;
        var recordsUpdated = 0;
        var recordsSkipped = 0;
        var runId = Guid.Empty;
        Guid? seriesId = null;
        EconomicSyncStateResult? state = null;
        DateOnly? from = null;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var to = request.To ?? today;

        logger.LogInformation(
            "Economic sync started for provider {Provider} series {Series} at {StartedAtUtc}; traceId {TraceId}",
            settings.ProviderKey,
            definition.ExternalSeriesId,
            startedAtUtc,
            Activity.Current?.TraceId.ToString() ?? "background");

        try
        {
            var existingSeries = await seriesStore.GetByExternalIdAsync(
                settings.ProviderKey,
                definition.ExternalSeriesId,
                cancellationToken);
            if (existingSeries is not null)
            {
                seriesId = existingSeries.Id;
                state = await stateStore.GetAsync(existingSeries.Id, cancellationToken);
                from = ResolveFrom(request, state, today);
                ValidateWindow(from.Value, to, today);
                runId = await stateStore.StartAsync(
                    existingSeries.Id,
                    from.Value,
                    to,
                    startedAtUtc,
                    cancellationToken);
            }

            var metadata = await ExecuteWithRetryAsync(
                token => provider.GetSeriesMetadataAsync(definition.ExternalSeriesId, token),
                attempts,
                cancellationToken);
            var series = await seriesStore.UpsertAsync(
                metadata,
                definition,
                settings.ProviderKey,
                timeProvider.GetUtcNow(),
                cancellationToken);
            seriesId = series.Id;
            if (runId == Guid.Empty)
            {
                state = await stateStore.GetAsync(series.Id, cancellationToken);
                from = ResolveFrom(request, state, today);
                ValidateWindow(from.Value, to, today);
                runId = await stateStore.StartAsync(
                    series.Id,
                    from.Value,
                    to,
                    startedAtUtc,
                    cancellationToken);
            }

            var offset = 0;
            DateOnly? latestObservationDate = state?.LastObservationDate;
            for (var pageNumber = 0; pageNumber < settings.MaximumPagesPerSeries; pageNumber++)
            {
                var page = await ExecuteWithRetryAsync(
                    token => provider.GetObservationsAsync(
                        new EconomicObservationProviderRequest(
                            definition.ExternalSeriesId,
                            from!.Value,
                            to,
                            offset,
                            settings.ProviderPageSize),
                        token),
                    attempts,
                    cancellationToken);
                recordsReceived += page.Observations.Count;

                var persisted = await observationStore.PersistAsync(
                    series.Id,
                    page.Observations,
                    timeProvider.GetUtcNow(),
                    cancellationToken);
                recordsInserted += persisted.Inserted;
                recordsUpdated += persisted.Updated;
                recordsSkipped += persisted.Skipped;
                if (persisted.LatestObservationDate > latestObservationDate)
                {
                    latestObservationDate = persisted.LatestObservationDate;
                }

                offset += page.Observations.Count;
                if (page.Observations.Count == 0
                    || page.Observations.Count < settings.ProviderPageSize
                    || offset >= page.TotalCount)
                {
                    break;
                }

                if (pageNumber == settings.MaximumPagesPerSeries - 1)
                {
                    throw new EconomicDataException(
                        EconomicDataErrorCodes.InvalidResponse,
                        "The economic provider result exceeded the configured page safety limit.");
                }
            }

            stopwatch.Stop();
            var completedAtUtc = timeProvider.GetUtcNow();
            var metrics = new EconomicSyncMetrics(
                attempts.RequestsMade,
                attempts.RateLimitResponses,
                recordsReceived,
                recordsInserted,
                recordsUpdated,
                recordsSkipped,
                stopwatch.ElapsedMilliseconds);
            await stateStore.CompleteAsync(
                runId,
                series.Id,
                completedAtUtc,
                latestObservationDate,
                metrics,
                cancellationToken);
            logger.LogInformation(
                "Economic sync completed for provider {Provider} series {Series}; requests {RequestsMade}, received {RecordsReceived}, inserted {RecordsInserted}, updated {RecordsUpdated}, skipped {RecordsSkipped}, durationMs {DurationMilliseconds}, status {Status}, traceId {TraceId}",
                settings.ProviderKey,
                definition.ExternalSeriesId,
                attempts.RequestsMade,
                recordsReceived,
                recordsInserted,
                recordsUpdated,
                recordsSkipped,
                stopwatch.ElapsedMilliseconds,
                "Succeeded",
                Activity.Current?.TraceId.ToString() ?? "background");

            return Result("Succeeded", null, null, completedAtUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var economicException = exception as EconomicDataException;
            var errorCode = economicException?.Code ?? EconomicDataErrorCodes.SyncFailed;
            var safeMessage = economicException?.SafeMessage ?? "Economic-data synchronization failed unexpectedly.";
            var failedAtUtc = timeProvider.GetUtcNow();
            var metrics = new EconomicSyncMetrics(
                attempts.RequestsMade,
                attempts.RateLimitResponses,
                recordsReceived,
                recordsInserted,
                recordsUpdated,
                recordsSkipped,
                stopwatch.ElapsedMilliseconds);
            if (runId != Guid.Empty && seriesId.HasValue)
            {
                await stateStore.FailAsync(
                    runId,
                    seriesId.Value,
                    failedAtUtc,
                    metrics,
                    errorCode,
                    safeMessage,
                    cancellationToken);
            }

            logger.LogError(
                exception,
                "Economic sync failed for provider {Provider} series {Series}; requests {RequestsMade}, received {RecordsReceived}, inserted {RecordsInserted}, updated {RecordsUpdated}, skipped {RecordsSkipped}, durationMs {DurationMilliseconds}, status {Status}, errorCode {ErrorCode}, traceId {TraceId}",
                settings.ProviderKey,
                definition.ExternalSeriesId,
                attempts.RequestsMade,
                recordsReceived,
                recordsInserted,
                recordsUpdated,
                recordsSkipped,
                stopwatch.ElapsedMilliseconds,
                "Failed",
                errorCode,
                Activity.Current?.TraceId.ToString() ?? "background");

            return Result("Failed", errorCode, safeMessage, failedAtUtc);
        }

        EconomicSeriesSyncResult Result(
            string status,
            string? errorCode,
            string? errorMessage,
            DateTimeOffset completedAtUtc) =>
            new(
                runId,
                settings.ProviderKey,
                definition.ExternalSeriesId,
                startedAtUtc,
                completedAtUtc,
                attempts.RequestsMade,
                attempts.RateLimitResponses,
                recordsReceived,
                recordsInserted,
                recordsUpdated,
                recordsSkipped,
                status,
                errorCode,
                errorMessage,
                stopwatch.ElapsedMilliseconds);
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ProviderAttempts attempts,
        CancellationToken cancellationToken)
    {
        for (var retry = 0; ; retry++)
        {
            attempts.RequestsMade++;
            try
            {
                return await operation(cancellationToken);
            }
            catch (EconomicDataException exception) when (exception.IsTransient && retry < settings.MaxRetries)
            {
                if (exception.Code == EconomicDataErrorCodes.RateLimited)
                {
                    attempts.RateLimitResponses++;
                }

                var exponentialSeconds = settings.RetryBaseDelaySeconds * Math.Pow(2, retry);
                var delay = exception.RetryAfter
                    ?? TimeSpan.FromSeconds(Math.Min(exponentialSeconds, 60));
                logger.LogWarning(
                    "Transient economic provider failure {ErrorCode}; retry {RetryNumber} of {MaximumRetries} after {DelayMilliseconds} ms",
                    exception.Code,
                    retry + 1,
                    settings.MaxRetries,
                    delay.TotalMilliseconds);
                await retryDelay.DelayAsync(delay, cancellationToken);
            }
            catch (EconomicDataException exception)
            {
                if (exception.Code == EconomicDataErrorCodes.RateLimited)
                {
                    attempts.RateLimitResponses++;
                }

                throw;
            }
        }
    }

    private IReadOnlyList<EconomicSeriesDefinition> SelectDefinitions(string? externalSeriesId)
    {
        if (string.IsNullOrWhiteSpace(externalSeriesId))
        {
            return settings.Series;
        }

        var definition = settings.Series.FirstOrDefault(item =>
            string.Equals(item.ExternalSeriesId, externalSeriesId.Trim(), StringComparison.OrdinalIgnoreCase));
        return definition is null
            ? throw new EconomicDataException(
                EconomicDataErrorCodes.InvalidRequest,
                "The requested economic series is not configured for synchronization.")
            : [definition];
    }

    private DateOnly ResolveFrom(
        EconomicSyncRequest request,
        EconomicSyncStateResult? state,
        DateOnly today) =>
        request.From
        ?? (state?.LastObservationDate?.AddDays(-settings.RevisionLookbackDays)
            ?? today.AddYears(-settings.InitialHistoryYears));

    private void ValidateWindow(DateOnly from, DateOnly to, DateOnly today)
    {
        if (from > to || to > today)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.InvalidRequest,
                "The economic synchronization date range is invalid or extends into the future.");
        }

        if (ExceedsYears(from, to, settings.MaximumQueryRangeYears))
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.InvalidRequest,
                $"The economic synchronization range cannot exceed {settings.MaximumQueryRangeYears} years.");
        }
    }

    private static bool ExceedsYears(DateOnly from, DateOnly to, int maximumYears)
    {
        var targetYear = from.Year + maximumYears;
        if (targetYear > DateOnly.MaxValue.Year)
        {
            return false;
        }

        var anniversary = new DateOnly(
            targetYear,
            from.Month,
            Math.Min(from.Day, DateTime.DaysInMonth(targetYear, from.Month)));
        return anniversary < to;
    }

    private sealed class ProviderAttempts
    {
        public int RequestsMade { get; set; }

        public int RateLimitResponses { get; set; }
    }
}
