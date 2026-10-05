using XauAi.Application.MarketData;

namespace XauAi.UnitTests.MarketData;

public sealed class MarketCandleAggregatorTests
{
    [Fact]
    public void Minute_candles_build_higher_timeframe_ohlc_and_keep_provider_traceability()
    {
        var start = new DateTimeOffset(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        var minuteCandles = new[]
        {
            Candle(start, 2675m, 2677m, 2674m, 2676m, 1m),
            Candle(start.AddMinutes(1), 2676m, 2678m, 2675m, 2677m, 2m),
            Candle(start.AddMinutes(2), 2677m, 2679m, 2673m, 2674m, 3m),
            Candle(start.AddMinutes(3), 2674m, 2676m, 2672m, 2675m, 4m),
            Candle(start.AddMinutes(4), 2675m, 2680m, 2674m, 2679m, 5m)
        };

        var result = MarketCandleAggregator.Aggregate(
            minuteCandles,
            MarketTimeframe.M5,
            start.AddMinutes(6));

        var candle = Assert.Single(result);
        Assert.Equal(2675m, candle.Open);
        Assert.Equal(2680m, candle.High);
        Assert.Equal(2672m, candle.Low);
        Assert.Equal(2679m, candle.Close);
        Assert.Equal(15m, candle.TickVolume);
        Assert.Equal("twelvedata", candle.ProviderKey);
        Assert.True(candle.IsComplete);
    }

    [Fact]
    public void Current_target_bucket_stays_incomplete()
    {
        var start = new DateTimeOffset(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);

        var candle = Assert.Single(MarketCandleAggregator.Aggregate(
            [Candle(start, 2675m, 2676m, 2674m, 2675.5m, 1m)],
            MarketTimeframe.H1,
            start.AddMinutes(1)));

        Assert.False(candle.IsComplete);
    }

    private static MarketCandleSnapshot Candle(
        DateTimeOffset openTime,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal volume) =>
        new(
            "XAUUSD",
            "XAU/USD",
            MarketTimeframe.M1,
            openTime,
            openTime.AddMinutes(1),
            open,
            high,
            low,
            close,
            volume,
            null,
            null,
            true,
            "UTC",
            openTime.AddHours(1),
            "twelvedata");
}
