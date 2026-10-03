using XauAi.Application.MarketData;

namespace XauAi.UnitTests.MarketData;

public sealed class MarketDataGapDetectorTests
{
    [Fact]
    public void Weekday_missing_interval_is_reported_without_fabricating_a_candle()
    {
        var start = new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);

        var gaps = MarketDataGapDetector.Detect(
            "XAUUSD",
            MarketTimeframe.M5,
            [start, start.AddMinutes(5), start.AddMinutes(15)],
            new DefaultMarketSessionCalendar(),
            100);

        var gap = Assert.Single(gaps);
        Assert.Equal(start.AddMinutes(10), gap.ExpectedOpenTimeUtc);
        Assert.Equal("UnverifiedMissing", gap.Classification);
    }

    [Fact]
    public void Weekend_closure_is_not_reported_as_a_data_gap()
    {
        var fridayClose = new DateTimeOffset(2026, 10, 2, 21, 0, 0, TimeSpan.Zero);
        var sundayOpen = new DateTimeOffset(2026, 10, 4, 22, 0, 0, TimeSpan.Zero);

        var gaps = MarketDataGapDetector.Detect(
            "XAUUSD",
            MarketTimeframe.H1,
            [fridayClose, sundayOpen],
            new DefaultMarketSessionCalendar(),
            100);

        Assert.Empty(gaps);
    }

    [Fact]
    public void Observed_xauusd_daily_maintenance_window_is_not_reported_as_a_gap()
    {
        var beforeMaintenance = new DateTimeOffset(2026, 9, 28, 20, 55, 0, TimeSpan.Zero);
        var afterMaintenance = new DateTimeOffset(2026, 9, 28, 22, 5, 0, TimeSpan.Zero);

        var gaps = MarketDataGapDetector.Detect(
            "XAUUSD",
            MarketTimeframe.M5,
            [beforeMaintenance, afterMaintenance],
            new DefaultMarketSessionCalendar(),
            100);

        Assert.Empty(gaps);
    }

    [Fact]
    public void Result_count_is_bounded()
    {
        var start = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var gaps = MarketDataGapDetector.Detect(
            "XAUUSD",
            MarketTimeframe.M1,
            [start, start.AddHours(1)],
            new DefaultMarketSessionCalendar(),
            5);

        Assert.Equal(5, gaps.Count);
    }
}
