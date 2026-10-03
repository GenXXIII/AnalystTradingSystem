using XauAi.Application.MarketData;

namespace XauAi.UnitTests.TechnicalAnalysis;

internal static class TechnicalAnalysisTestData
{
    internal static readonly DateTimeOffset StartUtc = new(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);

    internal static IReadOnlyList<StoredMarketCandle> FromCloses(
        IEnumerable<decimal> closes,
        MarketTimeframe timeframe = MarketTimeframe.H1) =>
        [.. closes.Select((close, index) => Candle(
            index,
            close - 0.25m,
            close + 0.5m,
            close - 0.5m,
            close,
            timeframe))];

    internal static StoredMarketCandle Candle(
        int index,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        MarketTimeframe timeframe = MarketTimeframe.H1,
        bool complete = true)
    {
        var openTime = StartUtc.AddTicks(timeframe.Duration().Ticks * index);
        return new StoredMarketCandle(
            "XAUUSD",
            "XAUUSD.test",
            timeframe,
            openTime,
            openTime.Add(timeframe.Duration()),
            open,
            high,
            low,
            close,
            100 + index,
            0,
            20,
            complete,
            openTime.Add(timeframe.Duration()));
    }
}
