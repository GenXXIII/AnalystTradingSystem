using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData;

internal sealed class MarketDataSynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    IMarketDataProvider provider,
    MarketDataPipelineSettings settings,
    TimeProvider timeProvider,
    ILogger<MarketDataSynchronizationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.SyncEnabled)
        {
            logger.LogInformation("Scheduled market-data synchronization is disabled");
            return;
        }

        logger.LogInformation(
            "Scheduled market-data synchronization started for {Symbol} with {TimeframeCount} timeframes at {IntervalSeconds} second intervals",
            settings.Symbol,
            settings.Timeframes.Count,
            settings.SyncIntervalSeconds);

        await SynchronizeHistoryAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(settings.SyncIntervalSeconds),
                timeProvider,
                stoppingToken);

            if (provider is ILatestMarketCandleProvider latestProvider)
            {
                try
                {
                    await ReconcileLatestAsync(latestProvider, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (MarketDataException exception)
                {
                    logger.LogWarning(
                        "Scheduled latest-candle reconciliation failed with code {ErrorCode} for {Symbol}",
                        exception.Code,
                        settings.Symbol);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Scheduled latest-candle reconciliation failed unexpectedly for {Symbol}",
                        settings.Symbol);
                }

                continue;
            }

            await SynchronizeHistoryAsync(stoppingToken);
        }
    }

    private async Task SynchronizeHistoryAsync(CancellationToken cancellationToken)
    {
        foreach (var timeframe in settings.Timeframes)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var synchronization = scope.ServiceProvider.GetRequiredService<IMarketDataSynchronizationService>();
                await synchronization.SynchronizeAsync(
                    new MarketDataSynchronizationRequest(
                        settings.Symbol,
                        timeframe,
                        FromUtc: null,
                        ToUtc: timeProvider.GetUtcNow(),
                        settings.IncludeFormingCandle),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (MarketDataException exception)
            {
                logger.LogWarning(
                    "Scheduled synchronization failed with code {ErrorCode} for {Symbol} {Timeframe}",
                    exception.Code,
                    settings.Symbol,
                    timeframe.Code());
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Scheduled synchronization failed unexpectedly for {Symbol} {Timeframe}",
                    settings.Symbol,
                    timeframe.Code());
            }
        }
    }

    private async Task ReconcileLatestAsync(
        ILatestMarketCandleProvider latestProvider,
        CancellationToken cancellationToken)
    {
        var observedAtUtc = timeProvider.GetUtcNow();
        var candles = await latestProvider.GetLatestCandlesAsync(
            settings.Symbol,
            settings.Timeframes,
            cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMarketCandleStore>();
        foreach (var group in candles.GroupBy(candle => candle.Timeframe))
        {
            var accepted = group
                .Select(candle => MarketCandleValidation.ValidateAndNormalize(
                    candle,
                    settings.Symbol,
                    group.Key,
                    candle.OpenTimeUtc,
                    candle.OpenTimeUtc,
                    observedAtUtc))
                .Where(outcome => outcome.IsValid && outcome.Candle is not null)
                .Select(outcome => outcome.Candle!)
                .ToArray();
            await store.SaveAsync(accepted, cancellationToken);
        }
    }
}
