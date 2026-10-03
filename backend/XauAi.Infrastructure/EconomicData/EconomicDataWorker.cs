using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.EconomicData;

namespace XauAi.Infrastructure.EconomicData;

internal sealed class EconomicDataWorker(
    IServiceScopeFactory scopeFactory,
    EconomicDataSettings settings,
    ILogger<EconomicDataWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IEconomicDataSynchronizationService>();
                var result = await service.SynchronizeAsync(new EconomicSyncRequest(), stoppingToken);
                logger.LogInformation(
                    "Scheduled economic synchronization finished; succeeded {Succeeded}, failed {Failed}, requests {RequestsMade}, received {RecordsReceived}",
                    result.Succeeded,
                    result.Failed,
                    result.RequestsMade,
                    result.RecordsReceived);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled economic synchronization failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(settings.SyncIntervalMinutes), stoppingToken);
        }
    }
}
