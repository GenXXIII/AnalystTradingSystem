using Microsoft.Extensions.Diagnostics.HealthChecks;
using XauAi.Application.Analysts;

namespace XauAi.Infrastructure.Analysts;

internal sealed class AnalystProviderHealthCheck(IAnalystDataProvider provider) : IHealthCheck
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
            AnalystProviderState.Available => HealthCheckResult.Healthy(status.Message, data),
            AnalystProviderState.Disabled => HealthCheckResult.Healthy(status.Message, data),
            _ => HealthCheckResult.Degraded(status.Message, data: data)
        };
    }
}
