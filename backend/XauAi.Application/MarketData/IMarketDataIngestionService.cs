namespace XauAi.Application.MarketData;

public interface IMarketDataIngestionService
{
    Task<MarketDataSyncResult> SyncAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}
