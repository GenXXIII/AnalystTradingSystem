using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed class DisabledMarketCandleStore : IMarketCandleStore
{
    public Task<StoredCandleCursor?> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset notAfterUtc,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<CandleSaveResult> SaveAsync(
        IReadOnlyCollection<MarketCandleSnapshot> candles,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    private static MarketDataException Disabled() =>
        new(
            MarketDataErrorCodes.DatabaseDisabled,
            "Database persistence is disabled.");
}
