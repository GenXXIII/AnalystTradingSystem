using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed class DisabledMarketDataQueryStore : IMarketDataQueryStore
{
    public Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(
        MarketDataQuery query,
        CancellationToken cancellationToken = default) => throw Disabled();

    public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        bool completedOnly,
        CancellationToken cancellationToken = default) => throw Disabled();

    public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset atUtc,
        int limit,
        CancellationToken cancellationToken = default) => throw Disabled();

    public Task<MarketDataAvailability> GetAvailabilityAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default) => throw Disabled();

    public Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default) => throw Disabled();

    private static MarketDataException Disabled() =>
        new(MarketDataErrorCodes.DatabaseDisabled, "Database persistence is disabled.");
}

internal sealed class DisabledMarketDataSyncStateStore : IMarketDataSyncStateStore
{
    public Task<Guid> StartRunAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default) => throw Disabled();

    public Task CompleteRunAsync(
        Guid runId,
        MarketDataPipelineResult result,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default) => throw Disabled();

    public Task FailRunAsync(
        Guid runId,
        MarketDataSyncProgress progress,
        string errorCode,
        string safeMessage,
        DateTimeOffset failedAtUtc,
        CancellationToken cancellationToken = default) => throw Disabled();

    public Task<MarketDataSyncStatus?> GetStatusAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default) => throw Disabled();

    private static MarketDataException Disabled() =>
        new(MarketDataErrorCodes.DatabaseDisabled, "Database persistence is disabled.");
}
