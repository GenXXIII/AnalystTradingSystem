using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.Application.LocalAnalysis;

internal sealed class LocalSignalEngine(LocalAnalystSettings settings) : ILocalSignalEngine
{
    public LocalSignalDecision Evaluate(
        IReadOnlyList<StoredMarketCandle> candles,
        TechnicalAnalysisResult analysis,
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset evaluatedAtUtc)
    {
        var latest = candles[^1];
        var atr = analysis.Indicators.Atr.Value ?? Math.Max(latest.High - latest.Low, 0.01m);
        var recentHigh = analysis.MarketStructure.Swings
            .Where(swing => swing.Kind == SwingKind.High && swing.CandleTimeUtc < latest.OpenTimeUtc)
            .OrderByDescending(swing => swing.CandleTimeUtc)
            .FirstOrDefault();
        var recentLow = analysis.MarketStructure.Swings
            .Where(swing => swing.Kind == SwingKind.Low && swing.CandleTimeUtc < latest.OpenTimeUtc)
            .OrderByDescending(swing => swing.CandleTimeUtc)
            .FirstOrDefault();

        var structure = StructureCondition(analysis, latest, recentHigh, recentLow);
        var trend = TrendCondition(analysis);
        var liquidity = LiquidityCondition(analysis, candles, latest, recentHigh, recentLow, atr);
        var candle = CandleCondition(analysis, latest, structure, trend);
        var momentum = MomentumCondition(analysis, candles);
        var ktr = KtrCondition(analysis, latest, atr);
        var conditions = new[] { structure, trend, liquidity, candle, momentum, ktr };
        var longScore = conditions.Where(value => value.LongMatched).Sum(value => value.Weight);
        var shortScore = conditions.Where(value => value.ShortMatched).Sum(value => value.Weight);
        var maxScore = settings.MaximumScore;
        var state = Decide(longScore, shortScore, analysis.Volatility.Regime);
        var score = state switch
        {
            LocalSignalState.Buy => longScore,
            LocalSignalState.Sell => shortScore,
            _ => Math.Max(longScore, shortScore)
        };
        var confidence = maxScore == 0m ? 0m : Math.Round(score / maxScore, 6);
        decimal? invalidation = state switch
        {
            LocalSignalState.Buy => BuyInvalidation(latest.Close, recentLow?.Price, atr),
            LocalSignalState.Sell => SellInvalidation(latest.Close, recentHigh?.Price, atr),
            _ => null
        };
        decimal? target = state switch
        {
            LocalSignalState.Buy => latest.Close + (atr * settings.TargetAtrMultiplier),
            LocalSignalState.Sell => latest.Close - (atr * settings.TargetAtrMultiplier),
            _ => null
        };
        var reason = state == LocalSignalState.Nothing
            ? NothingReason(longScore, shortScore, analysis.Volatility.Regime)
            : null;
        var validUntil = latest.CloseTimeUtc.Add(
            TimeSpan.FromTicks(timeframe.Duration().Ticks * settings.ValidityFor(timeframe)));

        return new LocalSignalDecision(
            symbol,
            timeframe,
            BuildCandleId(symbol, timeframe, latest.OpenTimeUtc),
            latest.OpenTimeUtc,
            latest.CloseTimeUtc,
            state,
            latest.Close,
            score,
            maxScore,
            confidence,
            structure.State,
            liquidity.State,
            candle.State,
            momentum.State,
            ktr.State,
            analysis.Volatility.Regime.ToString(),
            invalidation,
            target,
            reason,
            validUntil,
            conditions);
    }

    private LocalSignalCondition StructureCondition(
        TechnicalAnalysisResult analysis,
        StoredMarketCandle latest,
        MarketSwing? recentHigh,
        MarketSwing? recentLow)
    {
        var brokeHigh = recentHigh is not null && latest.Close > recentHigh.Price;
        var brokeLow = recentLow is not null && latest.Close < recentLow.Price;
        var state = brokeHigh
            ? analysis.MarketStructure.Direction == AnalyticalDirection.Bearish ? "BullishCHoCH" : "BullishBOS"
            : brokeLow
                ? analysis.MarketStructure.Direction == AnalyticalDirection.Bullish ? "BearishCHoCH" : "BearishBOS"
                : analysis.MarketStructure.Direction switch
                {
                    AnalyticalDirection.Bullish => "BullishStructure",
                    AnalyticalDirection.Bearish => "BearishStructure",
                    AnalyticalDirection.Conflicting => "Transition",
                    _ => "RangeOrUnconfirmed"
                };
        return new LocalSignalCondition(
            "Structure",
            state,
            brokeHigh || analysis.MarketStructure.Direction == AnalyticalDirection.Bullish,
            brokeLow || analysis.MarketStructure.Direction == AnalyticalDirection.Bearish,
            settings.StructureWeight,
            [$"SwingSequence:{analysis.MarketStructure.Structure}", $"Readiness:{analysis.MarketStructure.Readiness}",
                .. StrategyEvidence(analysis, "SMC/ICT", "DowTheory")]);
    }

    private LocalSignalCondition TrendCondition(TechnicalAnalysisResult analysis)
    {
        var bullish = analysis.Trend.Direction == AnalyticalDirection.Bullish;
        var bearish = analysis.Trend.Direction == AnalyticalDirection.Bearish;
        return new LocalSignalCondition(
            "Trend",
            bullish ? "BullishEmaAlignment" : bearish ? "BearishEmaAlignment" : analysis.Trend.Direction.ToString(),
            bullish,
            bearish,
            settings.TrendWeight,
            [$"EmaAlignment:{analysis.Trend.EmaAlignment}", $"PriceVsEma:{analysis.Trend.PriceVsEma}"]);
    }

    private LocalSignalCondition LiquidityCondition(
        TechnicalAnalysisResult analysis,
        IReadOnlyList<StoredMarketCandle> candles,
        StoredMarketCandle latest,
        MarketSwing? recentHigh,
        MarketSwing? recentLow,
        decimal atr)
    {
        var previousSession = candles
            .Where(candle => candle.OpenTimeUtc.Date < latest.OpenTimeUtc.Date)
            .Where(candle => candle.OpenTimeUtc.Date == latest.OpenTimeUtc.Date.AddDays(-1))
            .ToArray();
        var previousSessionHigh = previousSession.Length == 0 ? (decimal?)null : previousSession.Max(candle => candle.High);
        var previousSessionLow = previousSession.Length == 0 ? (decimal?)null : previousSession.Min(candle => candle.Low);
        var bullishSweep = recentLow is not null && latest.Low < recentLow.Price && latest.Close > recentLow.Price
            || previousSessionLow.HasValue && latest.Low < previousSessionLow.Value && latest.Close > previousSessionLow.Value;
        var bearishSweep = recentHigh is not null && latest.High > recentHigh.Price && latest.Close < recentHigh.Price
            || previousSessionHigh.HasValue && latest.High > previousSessionHigh.Value && latest.Close < previousSessionHigh.Value;
        var equalLow = analysis.MarketStructure.Swings.Any(swing =>
            swing.Kind == SwingKind.Low && Math.Abs(swing.Price - latest.Low) <= atr * settings.EqualLevelToleranceAtr);
        var equalHigh = analysis.MarketStructure.Swings.Any(swing =>
            swing.Kind == SwingKind.High && Math.Abs(swing.Price - latest.High) <= atr * settings.EqualLevelToleranceAtr);
        var bullish = bullishSweep || analysis.PriceAction.BullishRejection && equalLow;
        var bearish = bearishSweep || analysis.PriceAction.BearishRejection && equalHigh;
        var state = bullishSweep ? "BullishLiquiditySweep"
            : bearishSweep ? "BearishLiquiditySweep"
            : bullish ? "BullishEqualLowRejection"
            : bearish ? "BearishEqualHighRejection"
            : equalLow && equalHigh ? "TwoSidedLiquidity"
            : equalLow ? "EqualLowsObserved"
            : equalHigh ? "EqualHighsObserved"
            : "NoConfirmedSweep";
        var evidence = new List<string>
        {
            $"EqualHighObserved:{equalHigh}",
            $"EqualLowObserved:{equalLow}",
            $"BullishRejection:{analysis.PriceAction.BullishRejection}",
            $"BearishRejection:{analysis.PriceAction.BearishRejection}",
            $"PreviousSessionHigh:{previousSessionHigh:F3}",
            $"PreviousSessionLow:{previousSessionLow:F3}",
            "DerivedFromPriceOnly:True"
        };
        evidence.AddRange(StrategyEvidence(analysis, "SMC/ICT", "Wyckoff"));
        return new LocalSignalCondition("Liquidity", state, bullish, bearish, settings.LiquidityWeight, evidence);
    }

    private LocalSignalCondition CandleCondition(
        TechnicalAnalysisResult analysis,
        StoredMarketCandle latest,
        LocalSignalCondition structure,
        LocalSignalCondition trend)
    {
        var latestPatterns = analysis.CandlestickPatterns
            .Where(pattern => pattern.CandleTimeUtc == latest.OpenTimeUtc)
            .ToArray();
        var contextualSetups = analysis.Strategies?.Setups
            .Where(setup => setup.Family is "Candlestick" or "ClassicalPriceAction" or "SMC/ICT" or "Wyckoff")
            .Where(setup => setup.Quality >= 0.55m)
            .ToArray() ?? [];
        var rawBullish = latest.Close > latest.Open
            && (latestPatterns.Any(pattern => pattern.Direction == PatternDirection.Bullish)
                || contextualSetups.Any(setup => setup.Direction == AnalyticalDirection.Bullish)
                || analysis.PriceAction.BullishRejection
                || analysis.PriceAction.MomentumCandle);
        var rawBearish = latest.Close < latest.Open
            && (latestPatterns.Any(pattern => pattern.Direction == PatternDirection.Bearish)
                || contextualSetups.Any(setup => setup.Direction == AnalyticalDirection.Bearish)
                || analysis.PriceAction.BearishRejection
                || analysis.PriceAction.MomentumCandle);
        var bullish = rawBullish && (structure.LongMatched || trend.LongMatched);
        var bearish = rawBearish && (structure.ShortMatched || trend.ShortMatched);
        var state = bullish ? "BullishContextConfirmed"
            : bearish ? "BearishContextConfirmed"
            : latestPatterns.Length > 0 ? "PatternWithoutContext"
            : "NoContextualPattern";
        return new LocalSignalCondition(
            "Candle",
            state,
            bullish,
            bearish,
            settings.CandleWeight,
            [.. latestPatterns.Select(pattern => $"{pattern.Pattern}:{pattern.Direction}"),
                .. contextualSetups.Take(8).Select(setup => $"{setup.Family}:{setup.Strategy}:{setup.Direction}:{setup.State}"),
                $"MomentumCandle:{analysis.PriceAction.MomentumCandle}"]);
    }

    private LocalSignalCondition MomentumCondition(
        TechnicalAnalysisResult analysis,
        IReadOnlyList<StoredMarketCandle> candles)
    {
        var rsi = analysis.Momentum.Rsi;
        var macd = analysis.Momentum.Macd;
        var recent = candles.TakeLast(Math.Min(4, candles.Count)).ToArray();
        var rising = recent.Length >= 3 && recent[^1].Close > recent[^2].Close && recent[^2].Close > recent[^3].Close;
        var falling = recent.Length >= 3 && recent[^1].Close < recent[^2].Close && recent[^2].Close < recent[^3].Close;
        var currentBody = Math.Abs(recent[^1].Close - recent[^1].Open);
        var previousBody = recent.Length >= 2 ? Math.Abs(recent[^2].Close - recent[^2].Open) : currentBody;
        var accelerating = currentBody > previousBody;
        var volumeConfirmation = recent.Length >= 2
            && recent[^1].TickVolume.HasValue
            && recent[^2].TickVolume.HasValue
            && recent[^1].TickVolume >= recent[^2].TickVolume;
        var flow = analysis.Strategies?.Flow;
        var bullishFlow = flow is { Direction: AnalyticalDirection.Bullish, TickVolumeRatio: >= 0.8m } && rising;
        var bearishFlow = flow is { Direction: AnalyticalDirection.Bearish, TickVolumeRatio: >= 0.8m } && falling;
        var bullish = (rsi.Value > settings.RsiBullishMinimum && rsi.Value < settings.RsiBullishMaximum
            && macd.Momentum == AnalyticalDirection.Bullish
            && (rising || accelerating)) || bullishFlow;
        var bearish = (rsi.Value > settings.RsiBearishMinimum && rsi.Value < settings.RsiBearishMaximum
            && macd.Momentum == AnalyticalDirection.Bearish
            && (falling || accelerating)) || bearishFlow;
        var state = bullish ? accelerating ? "BullishAccelerating" : "Bullish"
            : bearish ? accelerating ? "BearishAccelerating" : "Bearish"
            : rising || falling ? "DirectionalUnconfirmed"
            : "Neutral";
        return new LocalSignalCondition(
            "MomentumFlow",
            state,
            bullish,
            bearish,
            settings.MomentumWeight,
            [$"Rsi:{rsi.Value:F2}/{rsi.Momentum}", $"Macd:{macd.Momentum}",
                $"PriceRising:{rising}", $"PriceFalling:{falling}",
                $"BodyAccelerating:{accelerating}", $"TickVolumeConfirms:{volumeConfirmation}",
                $"FlowMethod:{flow?.DataMethod ?? "Unavailable"}",
                $"TickVolumeRatio:{flow?.TickVolumeRatio:F3}",
                $"FlowDirection:{flow?.Direction.ToString() ?? "Unavailable"}",
                $"FlowDivergence:{flow?.Divergence ?? "Unavailable"}",
                "OrderBookClaimed:False"]);
    }

    private LocalSignalCondition KtrCondition(
        TechnicalAnalysisResult analysis,
        StoredMarketCandle latest,
        decimal atr)
    {
        var distance = atr * settings.ImportantLevelDistanceAtr;
        var support = analysis.SupportResistance
            .Where(zone => zone.Type is PriceZoneType.Support or PriceZoneType.Pivot)
            .OrderBy(zone => Math.Abs(zone.Center - latest.Close))
            .FirstOrDefault();
        var resistance = analysis.SupportResistance
            .Where(zone => zone.Type is PriceZoneType.Resistance or PriceZoneType.Pivot)
            .OrderBy(zone => Math.Abs(zone.Center - latest.Close))
            .FirstOrDefault();
        var nearSupport = support is not null && Math.Abs(latest.Close - support.Center) <= distance;
        var nearResistance = resistance is not null && Math.Abs(latest.Close - resistance.Center) <= distance;
        var ktr = analysis.Strategies?.Ktr;
        var nearestKtr = ktr?.Levels.OrderBy(level => Math.Abs(level.Price - latest.Close)).FirstOrDefault();
        var nearKtr = nearestKtr is not null && Math.Abs(latest.Close - nearestKtr.Price) <= distance;
        var bullish = (nearSupport || nearKtr) && latest.Close > latest.Open || analysis.PriceAction.BreakoutAboveRecentRange;
        var bearish = (nearResistance || nearKtr) && latest.Close < latest.Open || analysis.PriceAction.BreakoutBelowRecentRange;
        var state = analysis.PriceAction.BreakoutAboveRecentRange ? "BreakoutAboveImportantRange"
            : analysis.PriceAction.BreakoutBelowRecentRange ? "BreakoutBelowImportantRange"
            : bullish ? "SupportReaction"
            : bearish ? "ResistanceReaction"
            : "NoImportantLevelReaction";
        return new LocalSignalCondition(
            "KtrImportantLevel",
            state,
            bullish,
            bearish,
            settings.KtrWeight,
            [$"NearestSupport:{support?.Center:F3}", $"NearestResistance:{resistance?.Center:F3}",
                $"OpeningPrice:{ktr?.OpeningPrice:F3}", $"KtrUnit:{ktr?.Unit:F3}",
                $"NearestKtr:{nearestKtr?.Label}:{nearestKtr?.Price:F3}",
                $"DistanceTolerance:{distance:F3}"]);
    }

    private LocalSignalState Decide(decimal longScore, decimal shortScore, VolatilityRegime volatility)
    {
        if (volatility == VolatilityRegime.VeryHigh && !settings.AllowVeryHighVolatility)
        {
            return LocalSignalState.Nothing;
        }

        if (longScore >= settings.EntryScoreThreshold
            && longScore - shortScore >= settings.MinimumDirectionalLead)
        {
            return LocalSignalState.Buy;
        }

        if (shortScore >= settings.EntryScoreThreshold
            && shortScore - longScore >= settings.MinimumDirectionalLead)
        {
            return LocalSignalState.Sell;
        }

        return LocalSignalState.Nothing;
    }

    private string NothingReason(decimal longScore, decimal shortScore, VolatilityRegime volatility) =>
        volatility == VolatilityRegime.VeryHigh && !settings.AllowVeryHighVolatility ? "VOLATILITY_FILTER"
        : Math.Abs(longScore - shortScore) < settings.MinimumDirectionalLead ? "DIRECTIONAL_CONFLICT"
        : "SCORE_BELOW_THRESHOLD";

    private decimal BuyInvalidation(decimal close, decimal? swingLow, decimal atr)
    {
        var atrLevel = close - (atr * settings.InvalidationAtrMultiplier);
        return swingLow.HasValue && swingLow < close ? Math.Min(atrLevel, swingLow.Value) : atrLevel;
    }

    private decimal SellInvalidation(decimal close, decimal? swingHigh, decimal atr)
    {
        var atrLevel = close + (atr * settings.InvalidationAtrMultiplier);
        return swingHigh.HasValue && swingHigh > close ? Math.Max(atrLevel, swingHigh.Value) : atrLevel;
    }

    private static string BuildCandleId(string symbol, MarketTimeframe timeframe, DateTimeOffset openTimeUtc) =>
        $"{symbol.ToUpperInvariant()}-{timeframe.Code()}-{openTimeUtc.ToUniversalTime():yyyyMMddHHmmss}";

    private static IEnumerable<string> StrategyEvidence(
        TechnicalAnalysisResult analysis,
        params string[] families) =>
        analysis.Strategies?.Setups
            .Where(setup => families.Contains(setup.Family, StringComparer.Ordinal))
            .Take(8)
            .Select(setup => $"{setup.Family}:{setup.Strategy}:{setup.Direction}:{setup.State}")
        ?? [];
}
