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
