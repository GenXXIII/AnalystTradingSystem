using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class PriceActionAnalyzer : IPriceActionAnalyzer
{
    public PriceActionResult Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        TechnicalAnalysisSettings settings)
    {
        if (candles.Count < 2)
        {
            return new PriceActionResult(false, false, false, false, false, false, false, false, false, []);
        }

        var latest = candles[^1];
        var previous = candles[^2];
        var lookback = candles.Skip(Math.Max(0, candles.Count - settings.PriceActionLookback - 1))
            .Take(Math.Min(settings.PriceActionLookback, candles.Count - 1))
            .ToArray();
        var latestRange = latest.High - latest.Low;
        var averageRange = lookback.Length == 0 ? latestRange : lookback.Average(candle => candle.High - candle.Low);
        var body = Math.Abs(latest.Close - latest.Open);
        var upperWick = latest.High - Math.Max(latest.Open, latest.Close);
        var lowerWick = Math.Min(latest.Open, latest.Close) - latest.Low;
        var rangeExpansion = averageRange > 0m && latestRange >= averageRange * 1.5m;
        var rangeContraction = averageRange > 0m && latestRange <= averageRange * 0.65m;
        var breakoutAbove = lookback.Length > 0 && latest.Close > lookback.Max(candle => candle.High);
        var breakoutBelow = lookback.Length > 0 && latest.Close < lookback.Min(candle => candle.Low);
        var bullishRejection = lowerWick >= Math.Max(body, latestRange * 0.05m) * 2m;
        var bearishRejection = upperWick >= Math.Max(body, latestRange * 0.05m) * 2m;
        var momentum = latestRange > 0m && body / latestRange >= 0.70m && rangeExpansion;
        var insideRange = latest.High < previous.High && latest.Low > previous.Low;
        var recent = candles.TakeLast(Math.Min(10, candles.Count)).ToArray();
        var recentSpan = recent.Max(candle => candle.High) - recent.Min(candle => candle.Low);
        var consolidating = averageRange > 0m && recentSpan <= averageRange * 4m;
        var evidence = new List<string>();
        Add(evidence, rangeExpansion, "RangeExpansion");
        Add(evidence, rangeContraction, "RangeContraction");
        Add(evidence, breakoutAbove, "CloseAboveRecentRange");
        Add(evidence, breakoutBelow, "CloseBelowRecentRange");
        Add(evidence, bullishRejection, "LowerWickRejection");
        Add(evidence, bearishRejection, "UpperWickRejection");
        Add(evidence, momentum, "MomentumCandle");
        Add(evidence, insideRange, "InsideRange");
        Add(evidence, consolidating, "Consolidation");
        return new PriceActionResult(
            rangeExpansion,
            rangeContraction,
            breakoutAbove,
            breakoutBelow,
            bullishRejection,
            bearishRejection,
            momentum,
            insideRange,
            consolidating,
            evidence);
    }

    private static void Add(List<string> evidence, bool condition, string value)
    {
        if (condition)
        {
            evidence.Add(value);
        }
    }
}
