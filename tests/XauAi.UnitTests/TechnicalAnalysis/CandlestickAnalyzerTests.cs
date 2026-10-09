using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.UnitTests.TechnicalAnalysis;

public sealed class CandlestickAnalyzerTests
{
    private readonly CandlestickAnalyzer analyzer = new();

    public static TheoryData<string, IReadOnlyList<StoredMarketCandle>> PatternCases => new()
    {
        { "Doji", Candles((100m, 101m, 99m, 100.05m)) },
        { "Hammer", Candles((105m, 106m, 104m, 104.5m), (103m, 104m, 102m, 102.5m), (101m, 102m, 100m, 100.5m), (100.2m, 101m, 97.5m, 100.8m)) },
        { "InvertedHammer", Candles((105m, 106m, 104m, 104.5m), (103m, 104m, 102m, 102.5m), (101m, 102m, 100m, 100.5m), (100.8m, 103.5m, 100m, 100.2m)) },
        { "HangingMan", Candles((95m, 96m, 94m, 95.5m), (97m, 98m, 96m, 97.5m), (99m, 100m, 98m, 99.5m), (100.2m, 101m, 97.5m, 100.8m)) },
        { "ShootingStar", Candles((95m, 96m, 94m, 95.5m), (97m, 98m, 96m, 97.5m), (99m, 100m, 98m, 99.5m), (100.8m, 103.5m, 100m, 100.2m)) },
        { "BullishEngulfing", Candles((102m, 102.5m, 99.5m, 100m), (99.5m, 103m, 99m, 102.5m)) },
        { "BearishEngulfing", Candles((100m, 102.5m, 99.5m, 102m), (102.5m, 103m, 99m, 99.5m)) },
        { "MorningStar", Candles((103m, 103.5m, 99.5m, 100m), (100.2m, 100.5m, 99.8m, 100.1m), (100m, 102.5m, 99.8m, 102m)) },
        { "EveningStar", Candles((100m, 103.5m, 99.5m, 103m), (102.9m, 103.2m, 102.5m, 102.8m), (103m, 103.1m, 100m, 100.5m)) },
        { "SpinningTop", Candles((100m, 103m, 97m, 101m)) },
        { "BullishHarami", Candles((105m, 106m, 99m, 100m), (101m, 104m, 100.5m, 103m)) },
        { "BearishHarami", Candles((100m, 106m, 99m, 105m), (104m, 104.5m, 101m, 102m)) },
        { "PiercingLine", Candles((105m, 106m, 99m, 100m), (99m, 104m, 98m, 103m)) },
        { "DarkCloudCover", Candles((100m, 106m, 99m, 105m), (106m, 107m, 101m, 102m)) },
        { "ThreeWhiteSoldiers", Candles((100m, 103m, 99m, 102m), (101m, 104m, 100m, 103m), (102m, 105m, 101m, 104m)) },
        { "ThreeBlackCrows", Candles((104m, 105m, 101m, 102m), (103m, 104m, 100m, 101m), (102m, 103m, 99m, 100m)) },
        { "InsideBar", Candles((100m, 105m, 95m, 102m), (101m, 104m, 96m, 103m)) },
        { "OutsideBar", Candles((100m, 103m, 97m, 102m), (101m, 105m, 95m, 104m)) }
    };

    [Theory]
    [MemberData(nameof(PatternCases))]
    public void Detects_required_pattern(string expected, IReadOnlyList<StoredMarketCandle> candles)
    {
        var result = analyzer.Analyze(candles, MarketTimeframe.H1);

        Assert.Contains(result.Patterns, pattern => pattern.Pattern == expected);
        Assert.All(result.Patterns, pattern => Assert.InRange(pattern.Quality, 0m, 1m));
        Assert.NotNull(result.LatestQuality);
    }

    [Fact]
    public void Candle_quality_contains_body_wicks_range_and_relative_range()
    {
        var result = analyzer.Analyze(Candles((100m, 104m, 98m, 102m)), MarketTimeframe.H1);

        Assert.Equal(2m, result.LatestQuality!.Body);
        Assert.Equal(2m, result.LatestQuality.UpperWick);
        Assert.Equal(2m, result.LatestQuality.LowerWick);
        Assert.Equal(6m, result.LatestQuality.Range);
        Assert.Equal(1m / 3m, result.LatestQuality.BodyToRangeRatio);
    }

    private static IReadOnlyList<StoredMarketCandle> Candles(
        params (decimal Open, decimal High, decimal Low, decimal Close)[] values) =>
        [.. values.Select((value, index) => TechnicalAnalysisTestData.Candle(
            index,
            value.Open,
            value.High,
            value.Low,
            value.Close))];
}
