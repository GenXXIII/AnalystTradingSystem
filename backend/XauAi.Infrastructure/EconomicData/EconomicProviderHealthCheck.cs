using Microsoft.Extensions.Diagnostics.HealthChecks;
using XauAi.Application.EconomicData;

namespace XauAi.Infrastructure.EconomicData;

internal sealed class EconomicProviderHealthCheck(IEconomicDataProvider provider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = await provider.GetStatusAsync(cancellationToken);
        var data = new Dictionary<string, object>
        {
            ["state"] = status.State.ToString(),
            ["enabled"] = status.Enabled
        };
        return status.State switch
        {
            EconomicProviderState.Available => HealthCheckResult.Healthy(status.Message, data),
            EconomicProviderState.Disabled => HealthCheckResult.Healthy(status.Message, data),
            _ => HealthCheckResult.Degraded(status.Message, data: data)
        };
    }
}
