using Microsoft.EntityFrameworkCore;
using XauAi.Application.MarketData;
using XauAi.Domain.Market;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed class EfMarketDataSourceComparisonStore(
    XauAiDbContext dbContext,
    MarketDataReferenceResolver referenceResolver) : IMarketDataSourceComparisonStore
{
    public async Task<MarketDataSourceComparison> CompareAsync(
        string symbol,
        MarketTimeframe timeframe,
        string primaryProviderKey,
        string referenceProviderKey,
        int limit,
        decimal toleranceBps,
        CancellationToken cancellationToken = default)
    {
        var primaryReferences = await referenceResolver.FindAsync(
            symbol,
            timeframe,
            primaryProviderKey,
            cancellationToken);
        var referenceReferences = await referenceResolver.FindAsync(
            symbol,
            timeframe,
            referenceProviderKey,
            cancellationToken);
        if (primaryReferences is null || referenceReferences is null)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.DatabaseDisabled,
                "Required market reference data is unavailable.");
        }

        var primary = await LatestAsync(primaryReferences, limit, cancellationToken);
        var reference = await LatestAsync(referenceReferences, limit, cancellationToken);
        var matches = primary
            .Join(
                reference,
                left => left.OpenTimeUtc,
                right => right.OpenTimeUtc,
                (left, right) => new Match(
                    left.OpenTimeUtc,
                    left.Close,
                    right.Close,
                    DeviationBps(left.Close, right.Close)))
            .OrderBy(match => match.OpenTimeUtc)
            .ToArray();
        var latest = matches.LastOrDefault();
        var maximumDeviation = matches.Length == 0 ? (decimal?)null : matches.Max(match => match.DeviationBps);
        return new MarketDataSourceComparison(
            symbol,
            timeframe,
            primaryProviderKey,
            referenceProviderKey,
            ReferenceEnabled: true,
            limit,
            matches.Length,
            latest?.OpenTimeUtc,
            latest?.PrimaryClose,
            latest?.ReferenceClose,
            latest?.DeviationBps,
            maximumDeviation,
            toleranceBps,
            maximumDeviation is null ? null : maximumDeviation <= toleranceBps);
    }

    private async Task<IReadOnlyList<MarketCandle>> LatestAsync(
        MarketDataReferences references,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.MarketCandles
            .AsNoTracking()
            .Where(candle =>
                candle.InstrumentId == references.InstrumentId
                && candle.TimeframeId == references.TimeframeId
                && candle.DataProviderId == references.ProviderId
                && candle.IsComplete)
            .OrderByDescending(candle => candle.OpenTimeUtc)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

    private static decimal DeviationBps(decimal primaryClose, decimal referenceClose) =>
        primaryClose == 0 ? 0 : Math.Abs(referenceClose - primaryClose) / primaryClose * 10000m;

    private sealed record Match(
        DateTimeOffset OpenTimeUtc,
        decimal PrimaryClose,
        decimal ReferenceClose,
        decimal DeviationBps);
}
