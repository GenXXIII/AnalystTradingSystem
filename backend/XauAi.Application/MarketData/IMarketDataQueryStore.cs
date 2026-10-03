namespace XauAi.Application.MarketData;

public interface IMarketDataQueryStore
{
    Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(
        MarketDataQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        bool completedOnly,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset atUtc,
        int limit,
        CancellationToken cancellationToken = default);

    Task<MarketDataAvailability> GetAvailabilityAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}
