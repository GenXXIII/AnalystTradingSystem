using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class StrategyFeatureAnalyzer : IStrategyFeatureAnalyzer
{
    private static readonly string[] CoveredFamilies =
    [
        "Candlestick and classical price action",
        "SMC and selected ICT price models",
        "Wyckoff accumulation and distribution",
        "VSA and broker tick-volume flow",
        "Support, resistance, Dow structure, and opening range",
        "RSI, MACD, EMA/SMA, ADX, ATR, Bollinger Bands, and Stochastic",
        "Trend following, mean reversion, momentum, and volatility breakout",
        "Fibonacci retracement context",
        "OP and volatility-normalized KTR levels"
    ];

    private static readonly string[] UnavailableCapabilities =
    [
        "True bid/ask delta and CVD are unavailable from OHLC plus broker tick volume.",
        "Footprint imbalance and verified absorption require transaction-side data.",
        "Exchange POC, VAH, VAL, and volume nodes require an exchange volume profile feed.",
        "SMT divergence requires a separately validated correlated-market feed.",
        "Elliott Wave and harmonic labels are not emitted without deterministic pivot validation.",
        "Macro, news, yields, and USD context remain independent News and evidence inputs."
    ];

    public StrategyAnalysisResult Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe,
        IndicatorSet indicators,
        TrendAnalysisResult trend,
        MarketStructureResult structure,
        PriceActionResult priceAction,
        VolatilityAnalysisResult volatility,
        IReadOnlyList<SupportResistanceZone> levels,
        IReadOnlyList<CandlestickPatternResult> patterns)
    {
        var latest = candles[^1];
        var flow = AnalyzeFlow(candles, priceAction);
        var ktr = BuildKtr(candles, timeframe, indicators.Atr.Value);
        var setups = new List<StrategySetupResult>();

        foreach (var pattern in patterns.Where(pattern => pattern.CandleTimeUtc == latest.OpenTimeUtc))
        {
            Add(setups, "Candlestick", pattern.Pattern, ToDirection(pattern.Direction), "Detected", pattern.Quality,
                pattern.SupportingConditions);
        }

        AddPriceAction(setups, priceAction, flow);
        AddSmartMoneyAndWyckoff(setups, candles, priceAction, flow);
        AddIndicatorAndQuantitative(setups, latest, indicators, trend, structure, volatility);
        AddFibonacciContext(setups, latest.Close, structure.Swings);
        AddLevelContext(setups, latest, levels, indicators.Atr.Value);

        return new StrategyAnalysisResult(setups, flow, ktr, CoveredFamilies, UnavailableCapabilities);
    }

    private static FlowFeatureResult AnalyzeFlow(
        IReadOnlyList<StoredMarketCandle> candles,
        PriceActionResult priceAction)
    {
        var latest = candles[^1];
        var baseline = candles.TakeLast(Math.Min(21, candles.Count)).SkipLast(1)
            .Where(candle => candle.TickVolume.HasValue)
            .ToArray();
        var averageVolume = baseline.Length == 0 ? (decimal?)null : baseline.Average(candle => candle.TickVolume!.Value);
        decimal? volumeRatio = latest.TickVolume.HasValue && averageVolume is > 0m
            ? latest.TickVolume.Value / averageVolume.Value
            : null;
        var pressureWindow = candles.TakeLast(Math.Min(8, candles.Count))
            .Where(candle => candle.TickVolume.HasValue)
            .ToArray();
        var totalActivity = pressureWindow.Sum(candle => candle.TickVolume!.Value);
        var signedActivity = pressureWindow.Sum(candle =>
            candle.TickVolume!.Value * (candle.Close > candle.Open ? 1m : candle.Close < candle.Open ? -1m : 0m));
        var pressure = totalActivity <= 0m ? (decimal?)null : signedActivity / totalActivity;
        var direction = pressure switch
        {
            > 0.12m => AnalyticalDirection.Bullish,
            < -0.12m => AnalyticalDirection.Bearish,
            null => AnalyticalDirection.Neutral,
            _ => AnalyticalDirection.Transition
        };
        var comparison = candles.Count >= 6 ? candles[^6] : candles[0];
        var priceDirection = latest.Close > comparison.Close ? AnalyticalDirection.Bullish
            : latest.Close < comparison.Close ? AnalyticalDirection.Bearish
            : AnalyticalDirection.Neutral;
        var divergence = priceDirection == AnalyticalDirection.Bullish && direction == AnalyticalDirection.Bearish
            ? "BearishPriceFlowDivergence"
            : priceDirection == AnalyticalDirection.Bearish && direction == AnalyticalDirection.Bullish
                ? "BullishPriceFlowDivergence"
                : "NoneDetected";
        var range = Math.Max(latest.High - latest.Low, 0.0000001m);
        var bodyRatio = Math.Abs(latest.Close - latest.Open) / range;
        var absorption = volumeRatio >= 1.5m && bodyRatio <= 0.35m;
        var upperWick = latest.High - Math.Max(latest.Open, latest.Close);
        var lowerWick = Math.Min(latest.Open, latest.Close) - latest.Low;
        var exhaustion = volumeRatio >= 1.5m && Math.Max(upperWick, lowerWick) / range >= 0.50m;
        var breakoutConfirmed = (priceAction.BreakoutAboveRecentRange || priceAction.BreakoutBelowRecentRange)
            && volumeRatio >= 1.10m;
        var evidence = new List<string>
        {
            $"TickVolumeRatio:{Format(volumeRatio)}",
            $"DirectionalPressureProxy:{Format(pressure)}",
            $"PriceDirection:{priceDirection}",
            $"Divergence:{divergence}",
            $"AbsorptionCandidate:{absorption}",
            $"ExhaustionCandidate:{exhaustion}",
            $"BreakoutActivityConfirmed:{breakoutConfirmed}",
            "TrueBidAskDeltaAvailable:False"
        };
        return new FlowFeatureResult(
            "BrokerTickVolumePriceProxy",
            false,
            volumeRatio,
            pressure,
            direction,
            divergence,
            absorption,
            exhaustion,
            breakoutConfirmed,
            evidence);
    }

    private static KtrLevelSet BuildKtr(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe,
        decimal? atr)
    {
        var latest = candles[^1];
        var sessionCandles = candles.Where(candle => candle.OpenTimeUtc.UtcDateTime.Date == latest.OpenTimeUtc.UtcDateTime.Date).ToArray();
        var openingPrice = sessionCandles.FirstOrDefault()?.Open ?? latest.Open;
        var recentAverageRange = candles.TakeLast(Math.Min(20, candles.Count))
            .Average(candle => Math.Max(candle.High - candle.Low, 0.0000001m));
        var baseAtr = atr is > 0m ? atr.Value : recentAverageRange;
        var barsPerDay = Math.Max(1d, 1_440d / Math.Max(1d, timeframe.Duration().TotalMinutes));
        var unit = Math.Max(0.01m, baseAtr * (decimal)Math.Sqrt(barsPerDay) / 3m);
        var levels = Enumerable.Range(-3, 7)
            .Where(multiple => multiple != 0)
            .Select(multiple => new KtrLevelResult(
                multiple > 0 ? $"KTR+{multiple}" : $"KTR{multiple}",
                openingPrice + (unit * multiple),
                multiple))
            .Prepend(new KtrLevelResult("OP", openingPrice, 0))
            .ToArray();
        return new KtrLevelSet(
            openingPrice,
            unit,
            "UTC session OP; KTR unit = selected-timeframe ATR x sqrt(bars per day) / 3",
            levels);
    }

    private static void AddPriceAction(
        ICollection<StrategySetupResult> setups,
        PriceActionResult priceAction,
        FlowFeatureResult flow)
    {
        if (priceAction.BreakoutAboveRecentRange)
        {
            Add(setups, "ClassicalPriceAction", "BullishBreakout", AnalyticalDirection.Bullish,
                flow.BreakoutConfirmed ? "Confirmed" : "Watching", flow.BreakoutConfirmed ? 0.78m : 0.58m,
                ["CloseBeyondRecentRange", $"TickVolumeRatio:{Format(flow.TickVolumeRatio)}"]);
        }
        if (priceAction.BreakoutBelowRecentRange)
        {
            Add(setups, "ClassicalPriceAction", "BearishBreakout", AnalyticalDirection.Bearish,
                flow.BreakoutConfirmed ? "Confirmed" : "Watching", flow.BreakoutConfirmed ? 0.78m : 0.58m,
                ["CloseBeyondRecentRange", $"TickVolumeRatio:{Format(flow.TickVolumeRatio)}"]);
        }
        if (priceAction.RangeContraction || priceAction.Consolidating)
        {
            Add(setups, "Volatility", "Compression", AnalyticalDirection.Neutral, "Watching", 0.55m,
                priceAction.Evidence);
        }
        if (flow.AbsorptionCandidate)
        {
            Add(setups, "VSA", "HighActivityLowProgress", AnalyticalDirection.Transition, "Candidate", 0.62m,
                flow.Evidence);
        }
        if (flow.ExhaustionCandidate)
        {
            Add(setups, "VSA", "ActivityExhaustion", AnalyticalDirection.Transition, "Candidate", 0.62m,
                flow.Evidence);
        }
    }

    private static void AddSmartMoneyAndWyckoff(
        ICollection<StrategySetupResult> setups,
        IReadOnlyList<StoredMarketCandle> candles,
        PriceActionResult priceAction,
        FlowFeatureResult flow)
    {
        if (candles.Count < 3) return;
        var latest = candles[^1];
        var first = candles[^3];
        if (latest.Low > first.High)
        {
            Add(setups, "SMC/ICT", "BullishFVG", AnalyticalDirection.Bullish, "Detected", 0.64m,
                [$"Gap:{first.High:F3}-{latest.Low:F3}", "ThreeCandleImbalance"]);
        }
        if (latest.High < first.Low)
        {
            Add(setups, "SMC/ICT", "BearishFVG", AnalyticalDirection.Bearish, "Detected", 0.64m,
                [$"Gap:{latest.High:F3}-{first.Low:F3}", "ThreeCandleImbalance"]);
        }

        var prior = candles.TakeLast(Math.Min(13, candles.Count)).SkipLast(1).ToArray();
        if (prior.Length == 0) return;
        var priorLow = prior.Min(candle => candle.Low);
        var priorHigh = prior.Max(candle => candle.High);
        var bullishSweep = latest.Low < priorLow && latest.Close > priorLow;
        var bearishSweep = latest.High > priorHigh && latest.Close < priorHigh;
        if (bullishSweep)
        {
            Add(setups, "SMC/ICT", "SellSideLiquiditySweep", AnalyticalDirection.Bullish, "Detected", 0.72m,
                [$"SweptLow:{priorLow:F3}", "CloseReclaimedLevel"]);
            Add(setups, "Wyckoff", "SpringCandidate", AnalyticalDirection.Bullish,
                flow.TickVolumeRatio >= 1m ? "ConfirmedByActivity" : "Candidate", 0.68m,
                [$"RangeLow:{priorLow:F3}", $"TickVolumeRatio:{Format(flow.TickVolumeRatio)}"]);
        }
        if (bearishSweep)
        {
            Add(setups, "SMC/ICT", "BuySideLiquiditySweep", AnalyticalDirection.Bearish, "Detected", 0.72m,
                [$"SweptHigh:{priorHigh:F3}", "CloseRejectedLevel"]);
            Add(setups, "Wyckoff", "UpthrustCandidate", AnalyticalDirection.Bearish,
                flow.TickVolumeRatio >= 1m ? "ConfirmedByActivity" : "Candidate", 0.68m,
                [$"RangeHigh:{priorHigh:F3}", $"TickVolumeRatio:{Format(flow.TickVolumeRatio)}"]);
        }
        if (priceAction.MomentumCandle)
        {
            Add(setups, "SMC/ICT", "Displacement", latest.Close > latest.Open
                    ? AnalyticalDirection.Bullish : AnalyticalDirection.Bearish,
                "Detected", 0.70m, ["LargeBody", "RangeExpansion", "DirectionalClose"]);
        }

        var inverseGap = FindInverseFvg(candles);
        if (inverseGap is not null)
        {
            setups.Add(inverseGap);
        }
    }

    private static StrategySetupResult? FindInverseFvg(IReadOnlyList<StoredMarketCandle> candles)
    {
        if (candles.Count < 5) return null;
        var latest = candles[^1];
        for (var index = candles.Count - 2; index >= Math.Max(2, candles.Count - 22); index--)
        {
            var left = candles[index - 2];
            var right = candles[index];
            if (right.Low > left.High && latest.Close < left.High)
            {
                return new StrategySetupResult("SMC/ICT", "BearishIFVG", AnalyticalDirection.Bearish,
                    "Detected", 0.66m, [$"FormerBullishGap:{left.High:F3}-{right.Low:F3}", "CloseThroughFarBoundary"]);
            }
            if (right.High < left.Low && latest.Close > left.Low)
            {
                return new StrategySetupResult("SMC/ICT", "BullishIFVG", AnalyticalDirection.Bullish,
                    "Detected", 0.66m, [$"FormerBearishGap:{right.High:F3}-{left.Low:F3}", "CloseThroughFarBoundary"]);
            }
        }
        return null;
    }

    private static void AddIndicatorAndQuantitative(
        ICollection<StrategySetupResult> setups,
        StoredMarketCandle latest,
        IndicatorSet indicators,
        TrendAnalysisResult trend,
        MarketStructureResult structure,
        VolatilityAnalysisResult volatility)
    {
        var momentum = indicators.Macd.Momentum;
        if (trend.Direction is AnalyticalDirection.Bullish or AnalyticalDirection.Bearish
            && momentum == trend.Direction)
        {
            Add(setups, "Quantitative", "TrendMomentumAlignment", trend.Direction, "Detected", 0.70m,
                [$"EMA:{trend.EmaAlignment}", $"MACD:{momentum}", $"ADX:{indicators.Adx.Strength}"]);
        }
        if (structure.Direction is AnalyticalDirection.Bullish or AnalyticalDirection.Bearish)
        {
            Add(setups, "DowTheory", "SwingStructureTrend", structure.Direction, "Detected", 0.65m,
                [$"Structure:{structure.Structure}", $"Readiness:{structure.Readiness}"]);
        }
        var percentB = indicators.BollingerBands.PercentB;
        if (percentB < 0m && indicators.Rsi.Zone == IndicatorZone.Oversold)
        {
            Add(setups, "Quantitative", "BullishMeanReversion", AnalyticalDirection.Bullish, "Candidate", 0.62m,
                [$"BollingerPercentB:{Format(percentB)}", $"RSI:{Format(indicators.Rsi.Value)}"]);
        }
        if (percentB > 1m && indicators.Rsi.Zone == IndicatorZone.Overbought)
        {
            Add(setups, "Quantitative", "BearishMeanReversion", AnalyticalDirection.Bearish, "Candidate", 0.62m,
                [$"BollingerPercentB:{Format(percentB)}", $"RSI:{Format(indicators.Rsi.Value)}"]);
        }
        if (volatility.Regime is VolatilityRegime.High or VolatilityRegime.VeryHigh)
        {
            Add(setups, "Quantitative", "HighVolatilityRegime", latest.Close > latest.Open
                    ? AnalyticalDirection.Bullish : latest.Close < latest.Open
                        ? AnalyticalDirection.Bearish : AnalyticalDirection.Neutral,
                "RiskContext", 0.50m, volatility.Evidence);
        }
    }

    private static void AddFibonacciContext(
        ICollection<StrategySetupResult> setups,
        decimal close,
        IReadOnlyList<MarketSwing> swings)
    {
        var high = swings.Where(swing => swing.Kind == SwingKind.High).TakeLast(1).FirstOrDefault();
        var low = swings.Where(swing => swing.Kind == SwingKind.Low).TakeLast(1).FirstOrDefault();
        if (high is null || low is null || high.Price <= low.Price) return;
        var range = high.Price - low.Price;
        var retracement = (high.Price - close) / range;
        if (retracement is >= 0.50m and <= 0.786m)
        {
            var direction = high.CandleTimeUtc > low.CandleTimeUtc
                ? AnalyticalDirection.Bullish : AnalyticalDirection.Bearish;
            Add(setups, "Fibonacci", "RetracementConfluenceZone", direction, "Watching", 0.55m,
                [$"Retracement:{retracement:P1}", $"SwingLow:{low.Price:F3}", $"SwingHigh:{high.Price:F3}"]);
        }
    }

    private static void AddLevelContext(
        ICollection<StrategySetupResult> setups,
        StoredMarketCandle latest,
        IReadOnlyList<SupportResistanceZone> levels,
        decimal? atr)
    {
        var tolerance = Math.Max(atr ?? 0m, latest.Close * 0.0005m);
        var nearest = levels.OrderBy(level => Math.Abs(level.Center - latest.Close)).FirstOrDefault();
        if (nearest is null || Math.Abs(nearest.Center - latest.Close) > tolerance) return;
        var direction = nearest.Type == PriceZoneType.Support ? AnalyticalDirection.Bullish
            : nearest.Type == PriceZoneType.Resistance ? AnalyticalDirection.Bearish
            : AnalyticalDirection.Transition;
        Add(setups, "ClassicalPriceAction", $"{nearest.Type}ReactionZone", direction, "Watching", 0.58m,
            [$"Level:{nearest.Center:F3}", $"Touches:{nearest.Touches}", $"Strength:{nearest.Strength}"]);
    }

    private static void Add(
        ICollection<StrategySetupResult> setups,
        string family,
        string strategy,
        AnalyticalDirection direction,
        string state,
        decimal quality,
        IEnumerable<string> evidence) =>
        setups.Add(new StrategySetupResult(family, strategy, direction, state, Math.Clamp(quality, 0m, 1m), [.. evidence]));

    private static AnalyticalDirection ToDirection(PatternDirection direction) => direction switch
    {
        PatternDirection.Bullish => AnalyticalDirection.Bullish,
        PatternDirection.Bearish => AnalyticalDirection.Bearish,
        _ => AnalyticalDirection.Neutral
    };

    private static string Format(decimal? value) => value.HasValue ? value.Value.ToString("0.###") : "Unavailable";
}
