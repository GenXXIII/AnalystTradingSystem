namespace XauAi.Application.MarketData;

public interface IMarketDataSourceComparisonStore
{
    Task<MarketDataSourceComparison> CompareAsync(
        string symbol,
        MarketTimeframe timeframe,
        string primaryProviderKey,
        string referenceProviderKey,
        int limit,
        decimal toleranceBps,
        CancellationToken cancellationToken = default);
}

public interface IMarketDataQualityService
{
    Task<MarketDataSourceComparison> CompareAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken = default);
}
