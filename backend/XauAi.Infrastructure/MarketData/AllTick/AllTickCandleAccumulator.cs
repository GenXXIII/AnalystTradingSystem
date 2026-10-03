using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData.AllTick;

internal sealed class AllTickCandleAccumulator(
    string applicationSymbol,
    string providerSymbol,
    string providerKey,
    IReadOnlyCollection<MarketTimeframe> timeframes)
{
    private readonly Dictionary<MarketTimeframe, MutableCandle> _current = [];

    public IReadOnlyList<MarketCandleSnapshot> ApplyTick(
        DateTimeOffset timestampUtc,
        decimal price,
        decimal? volume,
        DateTimeOffset fetchedAtUtc)
    {
        if (price <= 0 || volume < 0)
        {
            return [];
        }

        var changed = new List<MarketCandleSnapshot>(timeframes.Count * 2);
        foreach (var timeframe in timeframes)
        {
            var openTime = timeframe.AlignDown(timestampUtc);
            if (!_current.TryGetValue(timeframe, out var candle))
            {
                candle = new MutableCandle(openTime, price, volume);
                _current[timeframe] = candle;
                changed.Add(ToSnapshot(timeframe, candle, fetchedAtUtc));
                continue;
            }

            if (openTime < candle.OpenTimeUtc)
            {
                continue;
            }

            if (openTime > candle.OpenTimeUtc)
            {
                // WebSocket-built candles remain provisional. The official batch endpoint
                // later reconciles and completes them before signal analysis consumes them.
                changed.Add(ToSnapshot(timeframe, candle, fetchedAtUtc));
                candle = new MutableCandle(openTime, price, volume);
                _current[timeframe] = candle;
            }
            else
            {
                candle.Apply(price, volume);
            }
        }

        return changed;
    }

    public IReadOnlyList<MarketCandleSnapshot> Snapshot(DateTimeOffset fetchedAtUtc) =>
        [.. _current
            .OrderBy(pair => pair.Key)
            .Select(pair => ToSnapshot(pair.Key, pair.Value, fetchedAtUtc))];

    private MarketCandleSnapshot ToSnapshot(
        MarketTimeframe timeframe,
        MutableCandle candle,
        DateTimeOffset fetchedAtUtc) =>
        new(
            applicationSymbol,
            providerSymbol,
            timeframe,
            candle.OpenTimeUtc,
            candle.OpenTimeUtc.Add(timeframe.Duration()),
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.Volume,
            null,
            null,
            IsComplete: false,
            "UTC",
            fetchedAtUtc,
            providerKey);

    private sealed class MutableCandle(DateTimeOffset openTimeUtc, decimal price, decimal? volume)
    {
        public DateTimeOffset OpenTimeUtc { get; } = openTimeUtc;

        public decimal Open { get; } = price;

        public decimal High { get; private set; } = price;

        public decimal Low { get; private set; } = price;

        public decimal Close { get; private set; } = price;

        public decimal? Volume { get; private set; } = volume;

        public void Apply(decimal price, decimal? volume)
        {
            High = Math.Max(High, price);
            Low = Math.Min(Low, price);
            Close = price;
            if (volume is not null)
            {
                Volume = (Volume ?? 0) + volume.Value;
            }
        }
    }
}
