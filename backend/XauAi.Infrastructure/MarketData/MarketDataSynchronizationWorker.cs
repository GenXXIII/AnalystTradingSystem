using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData;

internal sealed class MarketDataSynchronizationWorker(
    IServiceScopeFactory scopeFactory,
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

        while (!stoppingToken.IsCancellationRequested)
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
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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

            await Task.Delay(
                TimeSpan.FromSeconds(settings.SyncIntervalSeconds),
                timeProvider,
                stoppingToken);
        }
    }
}
