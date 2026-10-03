using Microsoft.EntityFrameworkCore;
using XauAi.Application.MarketData;
using XauAi.Domain.Market;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed class EfMarketDataQueryStore(
    XauAiDbContext dbContext,
    MarketDataReferenceResolver referenceResolver) : IMarketDataQueryStore
{
    public async Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(
        MarketDataQuery query,
        CancellationToken cancellationToken = default)
    {
        var references = await referenceResolver.RequireAsync(query.Symbol, query.Timeframe, cancellationToken);
        var candleQuery = BaseQuery(references)
            .Where(candle => candle.OpenTimeUtc >= query.FromUtc && candle.OpenTimeUtc <= query.ToUtc);
        if (query.CompletedOnly)
        {
            candleQuery = candleQuery.Where(candle => candle.IsComplete);
        }

        var candles = await candleQuery
            .OrderBy(candle => candle.OpenTimeUtc)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);
        return [.. candles.Select(candle => ToStored(query.Symbol, query.Timeframe, candle))];
    }

    public async Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        bool completedOnly,
        CancellationToken cancellationToken = default)
    {
        var references = await referenceResolver.RequireAsync(symbol, timeframe, cancellationToken);
        var query = BaseQuery(references);
        if (completedOnly)
        {
            query = query.Where(candle => candle.IsComplete);
        }

        var candles = await query
            .OrderByDescending(candle => candle.OpenTimeUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return [.. candles.Select(candle => ToStored(symbol, timeframe, candle))];
    }

    public async Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset atUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var references = await referenceResolver.RequireAsync(symbol, timeframe, cancellationToken);
        var candles = await BaseQuery(references)
            .Where(candle => candle.IsComplete && candle.CloseTimeUtc <= atUtc.ToUniversalTime())
            .OrderByDescending(candle => candle.OpenTimeUtc)
            .Take(limit)
            .OrderBy(candle => candle.OpenTimeUtc)
            .ToListAsync(cancellationToken);
        return [.. candles.Select(candle => ToStored(symbol, timeframe, candle))];
    }

    public async Task<MarketDataAvailability> GetAvailabilityAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        var references = await referenceResolver.RequireAsync(symbol, timeframe, cancellationToken);
        var query = BaseQuery(references);
        var count = await query.LongCountAsync(cancellationToken);
        if (count == 0)
        {
            return new MarketDataAvailability(symbol, timeframe, 0, null, null, null);
        }

        var oldest = await query.OrderBy(candle => candle.OpenTimeUtc)
            .Select(candle => candle.OpenTimeUtc)
            .FirstAsync(cancellationToken);
        var latest = await query.OrderByDescending(candle => candle.OpenTimeUtc)
            .Select(candle => candle.OpenTimeUtc)
            .FirstAsync(cancellationToken);
        var lastCompleted = await query.Where(candle => candle.IsComplete)
            .OrderByDescending(candle => candle.OpenTimeUtc)
            .Select(candle => (DateTimeOffset?)candle.OpenTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return new MarketDataAvailability(symbol, timeframe, count, oldest, latest, lastCompleted);
    }

    public async Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var references = await referenceResolver.RequireAsync(symbol, timeframe, cancellationToken);
        return await BaseQuery(references)
            .Where(candle => candle.OpenTimeUtc >= fromUtc && candle.OpenTimeUtc <= toUtc)
            .OrderBy(candle => candle.OpenTimeUtc)
            .Select(candle => candle.OpenTimeUtc)
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<MarketCandle> BaseQuery(MarketDataReferences references) =>
        dbContext.MarketCandles
            .AsNoTracking()
            .Where(candle =>
                candle.InstrumentId == references.InstrumentId
                && candle.TimeframeId == references.TimeframeId
                && candle.DataProviderId == references.ProviderId);

    private static StoredMarketCandle ToStored(
        string symbol,
        MarketTimeframe timeframe,
        MarketCandle candle) =>
        new(
            symbol,
            candle.ProviderSymbol,
            timeframe,
            candle.OpenTimeUtc,
            candle.CloseTimeUtc,
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.TickVolume,
            candle.RealVolume,
            candle.Spread,
            candle.IsComplete,
            candle.FetchedAtUtc);
}
