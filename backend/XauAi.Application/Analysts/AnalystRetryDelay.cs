namespace XauAi.Application.Analysts;

internal sealed class AnalystRetryDelay(TimeProvider timeProvider) : IAnalystRetryDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);
}
