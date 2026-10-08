using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.FullAnalysis;

namespace XauAi.Infrastructure.FullAnalysis;

internal sealed class FullAnalysisMonitoringWorker(
    IServiceScopeFactory scopeFactory,
    FullAnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<FullAnalysisMonitoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Full Analyst monitoring is disabled");
            return;
        }

        await MonitorAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.MonitorIntervalSeconds), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await MonitorAsync(stoppingToken);
        }
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IFullAnalystService>().MonitorActiveAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Full Analyst monitoring iteration failed");
        }
    }
}
