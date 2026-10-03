namespace XauAi.Application.MarketData;

public interface IMarketCandleStore
{
    Task<StoredCandleCursor?> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset notAfterUtc,
        CancellationToken cancellationToken = default);

    Task<CandleSaveResult> SaveAsync(
        IReadOnlyCollection<MarketCandleSnapshot> candles,
        CancellationToken cancellationToken = default);
}
