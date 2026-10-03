namespace XauAi.Application.EconomicData;

internal sealed class EconomicRetryDelay : IEconomicRetryDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
