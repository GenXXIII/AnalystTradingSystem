using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.News;

namespace XauAi.Infrastructure.News;

internal sealed class NewsCollectionWorker(
    IServiceScopeFactory scopeFactory,
    NewsSettings settings,
    TimeProvider timeProvider,
    ILogger<NewsCollectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Scheduled news collection is disabled");
            return;
        }

        logger.LogInformation(
            "Scheduled news collection started for {Provider} at {IntervalSeconds} second intervals",
            settings.Provider,
            settings.CollectionIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<INewsCollectionService>();
                await service.CollectAsync(new NewsCollectionRequest(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled news collection failed safely; persisted news remains available");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(settings.CollectionIntervalSeconds),
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
