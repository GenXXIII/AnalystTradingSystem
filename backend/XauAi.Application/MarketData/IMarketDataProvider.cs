namespace XauAi.Application.MarketData;

public interface IMarketDataProvider
{
    Task<MarketDataProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<MarketQuote> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketCandleSnapshot>> GetCandlesAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}

public interface ILatestMarketCandleProvider
{
    Task<IReadOnlyList<MarketCandleSnapshot>> GetLatestCandlesAsync(
        string symbol,
        IReadOnlyCollection<MarketTimeframe> timeframes,
        CancellationToken cancellationToken = default);
}

public interface IReferenceMarketDataProvider : IMarketDataProvider
{
}
