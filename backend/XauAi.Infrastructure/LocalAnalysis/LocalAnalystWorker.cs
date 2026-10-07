using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.LocalAnalysis;

namespace XauAi.Infrastructure.LocalAnalysis;

internal sealed class LocalAnalystWorker(
    IServiceScopeFactory scopeFactory,
    LocalAnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<LocalAnalystWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Continuous local analyst is disabled");
            return;
        }

        logger.LogInformation(
            "Continuous local analyst started for {Symbol} across {TimeframeCount} timeframes at {IntervalSeconds} second intervals",
            settings.Symbol,
            settings.Timeframes.Count,
            settings.EvaluationIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var timeframe in settings.Timeframes)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var analyst = scope.ServiceProvider.GetRequiredService<ILocalAnalystService>();
                    await analyst.EvaluateAsync(settings.Symbol, timeframe, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (LocalAnalystException exception)
                {
                    logger.LogWarning(
                        "Local analyst evaluation failed with code {ErrorCode} for {Symbol} {Timeframe}",
                        exception.Code,
                        settings.Symbol,
                        timeframe);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Local analyst evaluation failed unexpectedly for {Symbol} {Timeframe}",
                        settings.Symbol,
                        timeframe);
                }
            }

            await Task.Delay(
                TimeSpan.FromSeconds(settings.EvaluationIntervalSeconds),
                timeProvider,
                stoppingToken);
        }
    }
}

