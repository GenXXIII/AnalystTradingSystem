using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.TargetAnalysis;

namespace XauAi.Infrastructure.TargetAnalysis;

internal sealed class TargetAnalysisMonitoringWorker(
    IServiceScopeFactory scopeFactory,
    TargetAnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<TargetAnalysisMonitoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Target monitoring is disabled");
            return;
        }

        await MonitorAsync(stoppingToken);
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(settings.MonitorIntervalSeconds),
            timeProvider);
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
            await scope.ServiceProvider.GetRequiredService<ITargetAnalystService>()
                .MonitorActiveAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Target monitoring iteration failed");
        }
    }
}
