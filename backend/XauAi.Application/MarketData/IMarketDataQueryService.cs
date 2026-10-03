namespace XauAi.Application.MarketData;

public interface IMarketDataQueryService
{
    Task<MarketDataQueryResult> GetRangeAsync(
        MarketDataQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        bool completedOnly,
        CancellationToken cancellationToken = default);

    Task<StoredMarketCandle?> GetLastCompletedAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);

    Task<MarketDataPipelineStatus> GetStatusAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketDataGap>> GetGapsAsync(
        MarketDataQuery query,
        CancellationToken cancellationToken = default);
}
