using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.Analysts;

namespace XauAi.Infrastructure.Analysts;

internal sealed class AnalystSynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    AnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<AnalystSynchronizationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Scheduled analyst-data synchronization is disabled");
            return;
        }

        logger.LogInformation(
            "Scheduled analyst-data synchronization started for {Provider} at {IntervalMinutes} minute intervals",
            settings.Provider,
            settings.SyncIntervalMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IAnalystSynchronizationService>();
                await service.SynchronizeAsync(new AnalystSyncRequest(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Scheduled analyst-data synchronization failed safely; persisted analyst data remains available");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(settings.SyncIntervalMinutes),
                    timeProvider,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
