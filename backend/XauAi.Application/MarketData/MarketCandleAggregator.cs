namespace XauAi.Application.MarketData;

public static class MarketCandleAggregator
{
    public static IReadOnlyList<MarketCandleSnapshot> Aggregate(
        IReadOnlyCollection<MarketCandleSnapshot> minuteCandles,
        MarketTimeframe targetTimeframe,
        DateTimeOffset observedAtUtc)
    {
        if (minuteCandles.Count == 0)
        {
            return [];
        }

        if (minuteCandles.Any(candle => candle.Timeframe != MarketTimeframe.M1))
        {
            throw new ArgumentException("Only M1 candles can be aggregated.", nameof(minuteCandles));
        }

        var ordered = minuteCandles
            .OrderBy(candle => candle.OpenTimeUtc)
            .DistinctBy(candle => candle.OpenTimeUtc)
            .ToArray();
        if (targetTimeframe == MarketTimeframe.M1)
        {
            return ordered;
        }

        var first = ordered[0];
        if (ordered.Any(candle =>
                !string.Equals(candle.Symbol, first.Symbol, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(candle.ProviderSymbol, first.ProviderSymbol, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(candle.ProviderKey, first.ProviderKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Candles from different symbols or providers cannot be aggregated together.", nameof(minuteCandles));
        }

        var fetchedAtUtc = ordered.Max(candle => candle.FetchedAtUtc);
        var nowUtc = observedAtUtc.ToUniversalTime();
        return [.. ordered
            .GroupBy(candle => targetTimeframe.AlignDown(candle.OpenTimeUtc))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var values = group.OrderBy(candle => candle.OpenTimeUtc).ToArray();
                var closeTimeUtc = group.Key.Add(targetTimeframe.Duration());
                return new MarketCandleSnapshot(
                    first.Symbol,
                    first.ProviderSymbol,
                    targetTimeframe,
                    group.Key,
                    closeTimeUtc,
                    values[0].Open,
                    values.Max(candle => candle.High),
                    values.Min(candle => candle.Low),
                    values[^1].Close,
                    SumNullable(values.Select(candle => candle.TickVolume)),
                    SumNullable(values.Select(candle => candle.RealVolume)),
                    values[^1].Spread,
                    closeTimeUtc <= nowUtc && values.All(candle => candle.IsComplete),
                    "UTC",
                    fetchedAtUtc,
                    first.ProviderKey);
            })];
    }

    private static decimal? SumNullable(IEnumerable<decimal?> values)
    {
        var materialized = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return materialized.Length == 0 ? null : materialized.Sum();
    }
}
