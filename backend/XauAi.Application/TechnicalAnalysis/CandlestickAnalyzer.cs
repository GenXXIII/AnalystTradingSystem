using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class CandlestickAnalyzer : ICandlestickAnalyzer
{
    public (CandleQuality? LatestQuality, IReadOnlyList<CandlestickPatternResult> Patterns) Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe)
    {
        if (candles.Count == 0)
        {
            return (null, []);
        }

        var latest = candles[^1];
        var averageRange = candles.TakeLast(Math.Min(20, candles.Count))
            .Average(candle => Math.Max(candle.High - candle.Low, 0m));
        var quality = Measure(latest, averageRange);
        var patterns = new List<CandlestickPatternResult>();
        DetectSingleCandle(candles, timeframe, quality, patterns);
        DetectTwoCandle(candles, timeframe, averageRange, patterns);
        DetectThreeCandle(candles, timeframe, averageRange, patterns);
        return (quality, patterns);
    }

    private static void DetectSingleCandle(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe,
        CandleQuality quality,
        List<CandlestickPatternResult> patterns)
    {
        var candle = candles[^1];
        var effectiveBody = Math.Max(quality.Body, quality.Range * 0.02m);
        if (quality.Range > 0m && quality.BodyToRangeRatio <= 0.10m)
        {
            Add(patterns, "Doji", PatternDirection.Neutral, timeframe, candle,
                Clamp(1m - quality.BodyToRangeRatio),
                "Body is at most 10% of candle range.");
        }

        var priorTrend = PriorTrend(candles);
        var smallUpper = quality.UpperWick <= effectiveBody;
        var smallLower = quality.LowerWick <= effectiveBody;
        if (quality.LowerWick >= effectiveBody * 2m && smallUpper)
        {
            var name = priorTrend == AnalyticalDirection.Bullish ? "HangingMan" : "Hammer";
            var direction = name == "Hammer" ? PatternDirection.Bullish : PatternDirection.Bearish;
            Add(patterns, name, direction, timeframe, candle,
                Clamp(quality.LowerWick / Math.Max(quality.Range, 0.0000001m)),
                "Lower wick is at least twice the body.",
                "Upper wick is no larger than the body.");
        }

        if (quality.UpperWick >= effectiveBody * 2m && smallLower)
        {
            var name = priorTrend == AnalyticalDirection.Bullish ? "ShootingStar" : "InvertedHammer";
            var direction = name == "InvertedHammer" ? PatternDirection.Bullish : PatternDirection.Bearish;
            Add(patterns, name, direction, timeframe, candle,
                Clamp(quality.UpperWick / Math.Max(quality.Range, 0.0000001m)),
                "Upper wick is at least twice the body.",
                "Lower wick is no larger than the body.");
        }
    }

    private static void DetectTwoCandle(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe,
        decimal averageRange,
        List<CandlestickPatternResult> patterns)
    {
        if (candles.Count < 2)
        {
            return;
        }

        var previous = candles[^2];
        var current = candles[^1];
        var previousBodyLow = Math.Min(previous.Open, previous.Close);
        var previousBodyHigh = Math.Max(previous.Open, previous.Close);
        var currentBodyLow = Math.Min(current.Open, current.Close);
        var currentBodyHigh = Math.Max(current.Open, current.Close);
        if (previous.Close < previous.Open
            && current.Close > current.Open
            && currentBodyLow <= previousBodyLow
            && currentBodyHigh >= previousBodyHigh)
        {
            Add(patterns, "BullishEngulfing", PatternDirection.Bullish, timeframe, current,
                RelativeBodyQuality(current, previous),
                "Bullish body contains the previous bearish body.");
        }

        if (previous.Close > previous.Open
            && current.Close < current.Open
            && currentBodyLow <= previousBodyLow
            && currentBodyHigh >= previousBodyHigh)
        {
            Add(patterns, "BearishEngulfing", PatternDirection.Bearish, timeframe, current,
                RelativeBodyQuality(current, previous),
                "Bearish body contains the previous bullish body.");
        }

        if (current.High < previous.High && current.Low > previous.Low)
        {
            Add(patterns, "InsideBar", PatternDirection.Neutral, timeframe, current,
                RangeQuality(current, averageRange),
                "Current range is fully inside the previous range.");
        }

        if (current.High > previous.High && current.Low < previous.Low)
        {
            Add(patterns, "OutsideBar", Direction(current), timeframe, current,
                RangeQuality(current, averageRange),
                "Current range fully contains the previous range.");
        }
    }

    private static void DetectThreeCandle(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe,
        decimal averageRange,
        List<CandlestickPatternResult> patterns)
    {
        if (candles.Count < 3)
        {
            return;
        }

        var first = candles[^3];
        var middle = candles[^2];
        var last = candles[^1];
        var firstBody = Math.Abs(first.Close - first.Open);
        var middleBody = Math.Abs(middle.Close - middle.Open);
        var lastBody = Math.Abs(last.Close - last.Open);
        var firstMidpoint = (first.Open + first.Close) / 2m;
        var hasSmallMiddle = middleBody <= Math.Max(firstBody, lastBody) * 0.5m;
        if (first.Close < first.Open
            && last.Close > last.Open
            && hasSmallMiddle
            && last.Close > firstMidpoint)
        {
            Add(patterns, "MorningStar", PatternDirection.Bullish, timeframe, last,
                Clamp((firstBody + lastBody) / Math.Max(averageRange * 2m, 0.0000001m)),
                "Bearish first candle, small middle body, bullish recovery above first midpoint.");
        }

        if (first.Close > first.Open
            && last.Close < last.Open
            && hasSmallMiddle
            && last.Close < firstMidpoint)
        {
            Add(patterns, "EveningStar", PatternDirection.Bearish, timeframe, last,
                Clamp((firstBody + lastBody) / Math.Max(averageRange * 2m, 0.0000001m)),
                "Bullish first candle, small middle body, bearish decline below first midpoint.");
        }
    }

    private static CandleQuality Measure(StoredMarketCandle candle, decimal averageRange)
    {
        var body = Math.Abs(candle.Close - candle.Open);
        var range = Math.Max(candle.High - candle.Low, 0m);
        var upper = Math.Max(candle.High - Math.Max(candle.Open, candle.Close), 0m);
        var lower = Math.Max(Math.Min(candle.Open, candle.Close) - candle.Low, 0m);
        var bodyDenominator = Math.Max(body, range * 0.02m);
        return new CandleQuality(
            candle.OpenTimeUtc,
            body,
            upper,
            lower,
            range,
            range == 0m ? 0m : body / range,
            range == 0m ? 0m : upper / bodyDenominator,
            range == 0m ? 0m : lower / bodyDenominator,
            averageRange == 0m ? 0m : range / averageRange);
    }

    private static AnalyticalDirection PriorTrend(IReadOnlyList<StoredMarketCandle> candles)
    {
        if (candles.Count < 4)
        {
            return AnalyticalDirection.Neutral;
        }

        var start = candles[Math.Max(0, candles.Count - 6)].Close;
        var end = candles[^2].Close;
        return end > start ? AnalyticalDirection.Bullish
            : end < start ? AnalyticalDirection.Bearish
            : AnalyticalDirection.Neutral;
    }

    private static PatternDirection Direction(StoredMarketCandle candle) =>
        candle.Close > candle.Open ? PatternDirection.Bullish
        : candle.Close < candle.Open ? PatternDirection.Bearish
        : PatternDirection.Neutral;

    private static decimal RelativeBodyQuality(StoredMarketCandle current, StoredMarketCandle previous)
    {
        var previousBody = Math.Abs(previous.Close - previous.Open);
        var currentBody = Math.Abs(current.Close - current.Open);
        return Clamp(currentBody / Math.Max(previousBody, 0.0000001m) / 2m);
    }

    private static decimal RangeQuality(StoredMarketCandle candle, decimal averageRange) =>
        Clamp((candle.High - candle.Low) / Math.Max(averageRange, 0.0000001m));

    private static void Add(
        List<CandlestickPatternResult> patterns,
        string name,
        PatternDirection direction,
        MarketTimeframe timeframe,
        StoredMarketCandle candle,
        decimal quality,
        params string[] conditions) =>
        patterns.Add(new CandlestickPatternResult(
            name,
            direction,
            timeframe,
            candle.OpenTimeUtc,
            quality,
            conditions));

    private static decimal Clamp(decimal value) => Math.Clamp(value, 0m, 1m);
}
