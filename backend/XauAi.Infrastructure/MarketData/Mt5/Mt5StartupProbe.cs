using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData.Mt5;

internal sealed class Mt5StartupProbe(
    IMarketDataProvider provider,
    ILogger<Mt5StartupProbe> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MT5 initialization check started");
        var status = await provider.GetStatusAsync(stoppingToken);
        if (status.State == MarketDataProviderState.Connected)
        {
            logger.LogInformation("MT5 initialization check succeeded");
        }
        else if (status.State == MarketDataProviderState.Disabled)
        {
            logger.LogInformation("MT5 initialization skipped because the integration is disabled");
        }
        else
        {
            logger.LogWarning(
                "MT5 initialization check completed with state {Mt5State}",
                status.State);
        }
    }
}
