using Microsoft.Extensions.Diagnostics.HealthChecks;
using XauAi.Application.News;

namespace XauAi.Infrastructure.News;

internal sealed class NewsProviderHealthCheck(INewsProvider provider) : IHealthCheck
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
            NewsProviderState.Available => HealthCheckResult.Healthy(status.Message, data),
            NewsProviderState.Disabled => HealthCheckResult.Healthy(status.Message, data),
            _ => HealthCheckResult.Degraded(status.Message, data: data)
        };
    }
}
