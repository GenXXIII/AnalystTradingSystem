using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed class DisabledMarketDataSourceComparisonStore : IMarketDataSourceComparisonStore
{
    public Task<MarketDataSourceComparison> CompareAsync(
        string symbol,
        MarketTimeframe timeframe,
        string primaryProviderKey,
        string referenceProviderKey,
        int limit,
        decimal toleranceBps,
        CancellationToken cancellationToken = default) =>
        throw new MarketDataException(MarketDataErrorCodes.DatabaseDisabled, "Database persistence is disabled.");
}
