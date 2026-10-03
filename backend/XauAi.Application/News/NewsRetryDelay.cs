namespace XauAi.Application.News;

internal sealed class NewsRetryDelay(TimeProvider timeProvider) : INewsRetryDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);
}
