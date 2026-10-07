using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.UnitTests.LocalAnalysis;

public sealed class LocalSignalEngineTests
{
    private static readonly DateTimeOffset StartUtc = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Bullish_confluence_produces_explainable_buy()
    {
        var settings = new LocalAnalystSettings { EntryScoreThreshold = 4m };
        var engine = new LocalSignalEngine(settings);
        var candles = Candles(rising: true);
        var analysis = Analysis(candles, AnalyticalDirection.Bullish, bullish: true);

        var result = engine.Evaluate(candles, analysis, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));

        Assert.Equal(LocalSignalState.Buy, result.State);
        Assert.True(result.Score >= settings.EntryScoreThreshold);
        Assert.Equal(settings.MaximumScore, result.MaxScore);
        Assert.InRange(result.Confidence, 0m, 1m);
        Assert.Contains(result.Conditions, value => value.Component == "Trend" && value.LongMatched);
        Assert.Contains(result.Conditions, value => value.Component == "MomentumFlow" && value.LongMatched);
        Assert.NotNull(result.InvalidationPrice);
        Assert.NotNull(result.TargetPrice);
    }

    [Fact]
    public void Bearish_confluence_produces_sell()
    {
        var engine = new LocalSignalEngine(new LocalAnalystSettings());
        var candles = Candles(rising: false);
        var analysis = Analysis(candles, AnalyticalDirection.Bearish, bullish: false);

        var result = engine.Evaluate(candles, analysis, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));

        Assert.Equal(LocalSignalState.Sell, result.State);
        Assert.Contains(result.Conditions, value => value.Component == "Structure" && value.ShortMatched);
        Assert.Contains(result.Conditions, value => value.Component == "Candle" && value.ShortMatched);
        Assert.True(result.InvalidationPrice > result.SignalPrice);
        Assert.True(result.TargetPrice < result.SignalPrice);
    }

    [Fact]
    public void Conflicting_evidence_produces_nothing_with_reason()
    {
        var engine = new LocalSignalEngine(new LocalAnalystSettings());
        var candles = Candles(rising: true);
        var analysis = Analysis(candles, AnalyticalDirection.Conflicting, bullish: true) with
        {
            Trend = NeutralTrend(),
            PriceAction = EmptyPriceAction(),
            CandlestickPatterns = [],
            SupportResistance = [],
            Momentum = NeutralMomentum(),
            MarketStructure = new MarketStructureResult(
                AnalyticalDirection.Conflicting,
                "MixedStructure",
                [],
                AnalysisReadiness.Ready)
        };

        var result = engine.Evaluate(candles, analysis, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));

        Assert.Equal(LocalSignalState.Nothing, result.State);
        Assert.Equal("DIRECTIONAL_CONFLICT", result.Reason);
        Assert.Null(result.InvalidationPrice);
        Assert.Null(result.TargetPrice);
    }

    [Fact]
    public void Structure_break_against_prior_direction_is_classified_as_choch()
    {
        var engine = new LocalSignalEngine(new LocalAnalystSettings());
        var candles = Candles(rising: true);
        var latest = candles[^1];
        var bearish = Analysis(candles, AnalyticalDirection.Bearish, bullish: true) with
        {
            MarketStructure = new MarketStructureResult(
                AnalyticalDirection.Bearish,
                "LowerHighsAndLowerLows",
                [
                    new MarketSwing(SwingKind.High, SwingClassification.LowerHigh, latest.OpenTimeUtc.AddMinutes(-10), latest.Close - 1m, 200),
                    new MarketSwing(SwingKind.Low, SwingClassification.LowerLow, latest.OpenTimeUtc.AddMinutes(-5), latest.Close - 3m, 201)
                ],
                AnalysisReadiness.Ready)
        };

        var result = engine.Evaluate(candles, bearish, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));

        Assert.Equal("BullishCHoCH", result.StructureState);
    }

    [Fact]
    public void Same_snapshot_and_configuration_produce_same_decision()
    {
        var engine = new LocalSignalEngine(new LocalAnalystSettings());
        var candles = Candles(rising: true);
        var analysis = Analysis(candles, AnalyticalDirection.Bullish, bullish: true);

        var first = engine.Evaluate(candles, analysis, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));
        var second = engine.Evaluate(candles, analysis, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.SignalCandleId, second.SignalCandleId);
        Assert.Equal(
            first.Conditions.Select(value => (value.Component, value.State, value.LongMatched, value.ShortMatched, value.Weight, Evidence: string.Join('|', value.Evidence))),
            second.Conditions.Select(value => (value.Component, value.State, value.LongMatched, value.ShortMatched, value.Weight, Evidence: string.Join('|', value.Evidence))));
    }

    [Fact]
    public void Configured_threshold_controls_confirmation()
    {
        var candles = Candles(rising: true);
        var analysis = Analysis(candles, AnalyticalDirection.Bullish, bullish: true) with
        {
            PriceAction = EmptyPriceAction(),
            CandlestickPatterns = [],
            SupportResistance = []
        };
        var permissive = new LocalSignalEngine(new LocalAnalystSettings { EntryScoreThreshold = 3m });
        var strict = new LocalSignalEngine(new LocalAnalystSettings { EntryScoreThreshold = 6m });

        var permitted = permissive.Evaluate(candles, analysis, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));
        var rejected = strict.Evaluate(candles, analysis, "XAUUSD", MarketTimeframe.M5, StartUtc.AddDays(1));

        Assert.Equal(LocalSignalState.Buy, permitted.State);
        Assert.Equal(LocalSignalState.Nothing, rejected.State);
    }

    private static IReadOnlyList<StoredMarketCandle> Candles(bool rising)
    {
        var candles = new List<StoredMarketCandle>();
        for (var index = 0; index < 205; index++)
        {
            var openTime = StartUtc.AddMinutes(index * 5);
            var step = rising ? index * 0.2m : -index * 0.2m;
            var close = 2000m + step;
            var open = close + (rising ? -0.1m : 0.1m);
            candles.Add(new StoredMarketCandle(
                "XAUUSD",
                "GOLD",
                MarketTimeframe.M5,
                openTime,
                openTime.AddMinutes(5),
                open,
                Math.Max(open, close) + 0.4m,
                Math.Min(open, close) - 0.4m,
                close,
                100 + index,
                null,
                null,
                true,
                openTime.AddMinutes(5)));
        }

        return candles;
    }

    private static TechnicalAnalysisResult Analysis(
        IReadOnlyList<StoredMarketCandle> candles,
        AnalyticalDirection structureDirection,
        bool bullish)
    {
        var latest = candles[^1];
        var direction = bullish ? AnalyticalDirection.Bullish : AnalyticalDirection.Bearish;
        var patternDirection = bullish ? PatternDirection.Bullish : PatternDirection.Bearish;
        var zoneType = bullish ? PriceZoneType.Support : PriceZoneType.Resistance;
        var zoneCenter = latest.Close + (bullish ? -0.25m : 0.25m);
        var indicators = Indicators(direction, bullish);
        var trend = new TrendAnalysisResult(
            direction,
            "Strong",
            bullish ? "BullishStack" : "BearishStack",
            bullish ? "AboveAllAvailableEma" : "BelowAllAvailableEma",
            indicators.Sma,
            indicators.Ema);
        var structure = new MarketStructureResult(
            structureDirection,
            bullish ? "HigherHighsAndHigherLows" : "LowerHighsAndLowerLows",
            [
                new MarketSwing(
                    SwingKind.High,
                    bullish ? SwingClassification.HigherHigh : SwingClassification.LowerHigh,
                    latest.OpenTimeUtc.AddMinutes(-10),
                    latest.Close + (bullish ? -1m : 3m),
                    202),
                new MarketSwing(
                    SwingKind.Low,
                    bullish ? SwingClassification.HigherLow : SwingClassification.LowerLow,
                    latest.OpenTimeUtc.AddMinutes(-5),
                    latest.Close + (bullish ? -3m : 1m),
                    203)
            ],
            AnalysisReadiness.Ready);
        var priceAction = new PriceActionResult(
            true,
            false,
            bullish,
            !bullish,
            bullish,
            !bullish,
            true,
            false,
            false,
            [bullish ? "BullishExpansion" : "BearishExpansion"]);
        return new TechnicalAnalysisResult(
            "XAUUSD",
            MarketTimeframe.M5,
            latest.CloseTimeUtc,
            latest.CloseTimeUtc,
            trend,
            new MomentumAnalysisResult(indicators.Rsi, indicators.Macd, indicators.Stochastic),
            new VolatilityAnalysisResult(VolatilityRegime.Normal, 1m, 1m, indicators.Atr, indicators.BollingerBands, []),
            structure,
            [new SupportResistanceZone(zoneCenter - 0.1m, zoneCenter + 0.1m, zoneCenter, zoneType, "Strong", 4, [MarketTimeframe.M5], 0.01m)],
            [new CandlestickPatternResult(
                bullish ? "BullishEngulfing" : "BearishEngulfing",
                patternDirection,
                MarketTimeframe.M5,
                latest.OpenTimeUtc,
                0.9m,
                ["DeterministicFixture"])],
            new CandleQuality(latest.OpenTimeUtc, 0.1m, 0.4m, 0.4m, 0.9m, 0.11m, 4m, 4m, 1m),
            priceAction,
            indicators,
            new TechnicalConfluenceResult([], [], [], [], [], [], []),
            [],
            new AnalysisDiagnostics(1, candles.Count, candles.Count, 0, 0, 0, 10, 0, latest.CloseTimeUtc));
    }

    private static IndicatorSet Indicators(AnalyticalDirection direction, bool bullish)
    {
        var ema = bullish
            ? new[] { Value("EMA", 20, 2040m, 2039m), Value("EMA", 50, 2030m, 2029m), Value("EMA", 200, 2000m, 1999m) }
            : new[] { Value("EMA", 20, 1960m, 1961m), Value("EMA", 50, 1970m, 1971m), Value("EMA", 200, 2000m, 2001m) };
        return new IndicatorSet(
            [],
            ema,
            new RsiResult(14, bullish ? 60m : 40m, bullish ? 58m : 42m, IndicatorZone.Neutral, direction, AnalysisReadiness.Ready),
            new MacdResult(12, 26, 9, bullish ? 2m : -2m, bullish ? 1m : -1m, bullish ? 1m : -1m, 0.5m, bullish ? CrossoverState.Bullish : CrossoverState.Bearish, direction, AnalysisReadiness.Ready),
            new AtrResult(14, 2m, 1.8m, AnalysisReadiness.Ready),
            new AdxResult(14, 30m, bullish ? 25m : 10m, bullish ? 10m : 25m, "Strong", direction, AnalysisReadiness.Ready),
            new BollingerBandsResult(20, 2m, 2000m, 2010m, 1990m, 20m, 0.5m, AnalysisReadiness.Ready),
            new StochasticResult(14, 3, bullish ? 60m : 40m, bullish ? 55m : 45m, bullish ? CrossoverState.Bullish : CrossoverState.Bearish, IndicatorZone.Neutral, AnalysisReadiness.Ready));
    }

    private static IndicatorValueResult Value(string name, int period, decimal value, decimal previous) =>
        new(name, period, value, previous, AnalysisReadiness.Ready);

    private static TrendAnalysisResult NeutralTrend() =>
        new(AnalyticalDirection.Neutral, "Unavailable", "Mixed", "Mixed", [], []);

    private static MomentumAnalysisResult NeutralMomentum() =>
        new(
            new RsiResult(14, 50m, 50m, IndicatorZone.Neutral, AnalyticalDirection.Neutral, AnalysisReadiness.Ready),
            new MacdResult(12, 26, 9, 0m, 0m, 0m, 0m, CrossoverState.None, AnalyticalDirection.Neutral, AnalysisReadiness.Ready),
            new StochasticResult(14, 3, 50m, 50m, CrossoverState.None, IndicatorZone.Neutral, AnalysisReadiness.Ready));

    private static PriceActionResult EmptyPriceAction() =>
        new(false, false, false, false, false, false, false, true, true, []);
}
