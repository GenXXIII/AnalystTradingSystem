using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.UnitTests.TechnicalAnalysis;

public sealed class AnalysisComponentTests
{
    [Fact]
    public void Market_structure_identifies_higher_highs_and_higher_lows()
    {
        var candles = Candles(
            (9m, 10m, 8m, 9m),
            (10m, 12m, 9m, 11m),
            (9m, 11m, 7m, 10m),
            (12m, 14m, 9m, 13m),
            (10m, 12m, 8m, 11m),
            (14m, 16m, 10m, 15m),
            (13m, 15m, 11m, 14m));

        var result = new MarketStructureAnalyzer().Analyze(candles, swingWindow: 1);

        Assert.Equal(AnalyticalDirection.Bullish, result.Direction);
        Assert.Contains(result.Swings, swing => swing.Classification == SwingClassification.HigherHigh);
        Assert.Contains(result.Swings, swing => swing.Classification == SwingClassification.HigherLow);
    }

    [Fact]
    public void Support_and_resistance_groups_repeated_reactions_into_a_zone()
    {
        var candles = TechnicalAnalysisTestData.FromCloses([102m, 103m, 104m, 105m]);
        var swings = new[]
        {
            new MarketSwing(SwingKind.Low, SwingClassification.SwingLow, candles[0].OpenTimeUtc, 100.00m, 0),
            new MarketSwing(SwingKind.Low, SwingClassification.HigherLow, candles[1].OpenTimeUtc, 100.08m, 1),
            new MarketSwing(SwingKind.Low, SwingClassification.EqualLow, candles[2].OpenTimeUtc, 99.96m, 2)
        };

        var result = new SupportResistanceAnalyzer().Analyze(
            candles,
            MarketTimeframe.H1,
            swings,
            new TechnicalAnalysisSettings());

        var zone = Assert.Single(result);
        Assert.Equal(PriceZoneType.Support, zone.Type);
        Assert.Equal(3, zone.Touches);
        Assert.Equal("Moderate", zone.Strength);
    }

    [Fact]
    public void Price_action_detects_expansion_breakout_rejection_and_inside_range()
    {
        var expansion = Candles(
            (100m, 101m, 99m, 100m),
            (100m, 101m, 99m, 100m),
            (100m, 106m, 99.8m, 105.8m));
        var inside = Candles(
            (100m, 105m, 95m, 102m),
            (101m, 104m, 96m, 103m));
        var analyzer = new PriceActionAnalyzer();

        var expansionResult = analyzer.Analyze(expansion, new TechnicalAnalysisSettings { PriceActionLookback = 2 });
        var insideResult = analyzer.Analyze(inside, new TechnicalAnalysisSettings());

        Assert.True(expansionResult.RangeExpansion);
        Assert.True(expansionResult.BreakoutAboveRecentRange);
        Assert.True(expansionResult.MomentumCandle);
        Assert.True(insideResult.InsideRange);
    }

    [Fact]
    public void Volatility_regime_uses_atr_relative_to_its_baseline()
    {
        var calm = TechnicalAnalysisTestData.FromCloses(Enumerable.Repeat(100m, 50));
        var candles = calm.Concat(Candles((100m, 110m, 90m, 108m))).ToArray();
        var settings = new TechnicalAnalysisSettings { VolatilityLookback = 50 };
        var indicators = new IndicatorCalculator().Calculate(candles, settings);

        var result = new VolatilityAnalyzer().Analyze(candles, indicators, settings);

        Assert.NotEqual(VolatilityRegime.Unavailable, result.Regime);
        Assert.NotNull(result.AtrRelativeToBaseline);
        Assert.Contains(result.Evidence, value => value.StartsWith("AtrVsBaseline:", StringComparison.Ordinal));
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
