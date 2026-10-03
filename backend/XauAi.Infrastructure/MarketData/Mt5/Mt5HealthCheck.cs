using Microsoft.Extensions.Diagnostics.HealthChecks;
using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData.Mt5;

internal sealed class Mt5HealthCheck(IMarketDataProvider provider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = await provider.GetStatusAsync(cancellationToken);
        var data = new Dictionary<string, object>
        {
            ["state"] = status.State.ToString(),
            ["enabled"] = status.Enabled,
            ["connected"] = status.Connected
        };

        return status.State switch
        {
            MarketDataProviderState.Disabled => HealthCheckResult.Healthy(status.Message, data),
            MarketDataProviderState.Available or MarketDataProviderState.Connected =>
                HealthCheckResult.Healthy(status.Message, data),
            _ => HealthCheckResult.Degraded(status.Message, data: data)
        };
    }
}
