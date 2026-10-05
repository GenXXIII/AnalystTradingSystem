namespace XauAi.Application.MarketData;

internal sealed class MarketDataQualityService(
    IMarketDataSourceComparisonStore comparisonStore,
    MarketDataPipelineSettings settings) : IMarketDataQualityService
{
    public Task<MarketDataSourceComparison> CompareAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase)
            || !settings.Timeframes.Contains(timeframe))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                "The requested symbol or timeframe is not configured.");
        }

        if (limit is < 1 or > 500)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.QueryLimitExceeded,
                "The source-comparison limit must be between 1 and 500.");
        }

        if (!settings.ReferenceDataEnabled)
        {
            return Task.FromResult(new MarketDataSourceComparison(
                settings.Symbol,
                timeframe,
                settings.ProviderKey,
                settings.ReferenceProviderKey,
                ReferenceEnabled: false,
                limit,
                MatchedCandles: 0,
                LatestComparedOpenTimeUtc: null,
                LatestPrimaryClose: null,
                LatestReferenceClose: null,
                LatestDeviationBps: null,
                MaximumDeviationBps: null,
                settings.ReferenceMaximumCloseDeviationBps,
                IsWithinTolerance: null));
        }

        return comparisonStore.CompareAsync(
            settings.Symbol,
            timeframe,
            settings.ProviderKey,
            settings.ReferenceProviderKey,
            limit,
            settings.ReferenceMaximumCloseDeviationBps,
            cancellationToken);
    }
}
