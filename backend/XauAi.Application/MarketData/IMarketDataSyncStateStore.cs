namespace XauAi.Application.MarketData;

public interface IMarketDataSyncStateStore
{
    Task<Guid> StartRunAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default);

    Task CompleteRunAsync(
        Guid runId,
        MarketDataPipelineResult result,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default);

    Task FailRunAsync(
        Guid runId,
        MarketDataSyncProgress progress,
        string errorCode,
        string safeMessage,
        DateTimeOffset failedAtUtc,
        CancellationToken cancellationToken = default);

    Task<MarketDataSyncStatus?> GetStatusAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);
}
