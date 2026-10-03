namespace XauAi.Application.MarketData;

internal sealed class MarketDataIngestionService(
    IMarketDataSynchronizationService synchronizationService) : IMarketDataIngestionService
{
    public async Task<MarketDataSyncResult> SyncAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var result = await synchronizationService.SynchronizeAsync(
            new MarketDataSynchronizationRequest(
                symbol,
                timeframe,
                fromUtc,
                toUtc,
                IncludeFormingCandle: true),
            cancellationToken);

        return new MarketDataSyncResult(
            symbol,
            timeframe,
            result.RequestedFromUtc,
            result.RequestedToUtc,
            result.RequestedFromUtc,
            result.Received,
            result.Inserted,
            result.Updated,
            result.Skipped);
    }
}

public static class MarketDataRequestValidation
{
    public static void Validate(string symbol, DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                "A market symbol is required.");
        }

        if (fromUtc >= toUtc)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                "The market-data start time must be earlier than the end time.");
        }
    }
}
