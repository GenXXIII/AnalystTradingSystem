using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class IndicatorCalculator : IIndicatorCalculator
{
    public IndicatorSet Calculate(
        IReadOnlyList<StoredMarketCandle> candles,
        TechnicalAnalysisSettings settings)
    {
        var closes = candles.Select(candle => candle.Close).ToArray();
        var sma = settings.SmaPeriods
            .Select(period => MovingAverage("SMA", closes, period, exponential: false))
            .ToArray();
        var ema = settings.EmaPeriods
            .Select(period => MovingAverage("EMA", closes, period, exponential: true))
            .ToArray();

        return new IndicatorSet(
            sma,
            ema,
            Rsi(closes, settings.RsiPeriod),
            Macd(closes, settings.MacdFastPeriod, settings.MacdSlowPeriod, settings.MacdSignalPeriod),
            Atr(candles, settings.AtrPeriod),
            Adx(candles, settings.AdxPeriod),
            Bollinger(closes, settings.BollingerPeriod, settings.BollingerStandardDeviations),
            Stochastic(candles, settings.StochasticKPeriod, settings.StochasticDPeriod));
    }

    private static IndicatorValueResult MovingAverage(
        string name,
        IReadOnlyList<decimal> values,
        int period,
        bool exponential)
    {
        var series = exponential ? EmaValues(values, period) : SmaValues(values, period);
        return new IndicatorValueResult(
            name,
            period,
            Last(series),
            Previous(series),
            Readiness(values.Count, period, exponential ? period * 3 : period));
    }

    private static RsiResult Rsi(IReadOnlyList<decimal> closes, int period)
    {
        var values = RsiValues(closes, period);
        var current = Last(values);
        var previous = Previous(values);
        var zone = current switch
        {
            null => IndicatorZone.Unavailable,
            <= 30m => IndicatorZone.Oversold,
            >= 70m => IndicatorZone.Overbought,
            _ => IndicatorZone.Neutral
        };
        var momentum = current is null || previous is null
            ? AnalyticalDirection.Neutral
            : current > previous ? AnalyticalDirection.Bullish
            : current < previous ? AnalyticalDirection.Bearish
            : AnalyticalDirection.Neutral;
        return new RsiResult(
            period,
            current,
            previous,
            zone,
            momentum,
            Readiness(closes.Count, period + 1, period * 3));
    }

    private static MacdResult Macd(
        IReadOnlyList<decimal> closes,
        int fastPeriod,
        int slowPeriod,
        int signalPeriod)
    {
        var fast = EmaValues(closes, fastPeriod);
        var slow = EmaValues(closes, slowPeriod);
        var macd = new List<decimal>();
        for (var index = 0; index < closes.Count; index++)
        {
            if (fast[index].HasValue && slow[index].HasValue)
            {
                macd.Add(fast[index]!.Value - slow[index]!.Value);
            }
        }

        var signal = EmaValues(macd, signalPeriod);
        decimal? currentMacd = macd.Count == 0 ? null : macd[^1];
        decimal? previousMacd = macd.Count < 2 ? null : macd[^2];
        var currentSignal = Last(signal);
        var previousSignal = Previous(signal);
        decimal? histogram = currentMacd.HasValue && currentSignal.HasValue
            ? currentMacd.Value - currentSignal.Value
            : null;
        decimal? previousHistogram = previousMacd.HasValue && previousSignal.HasValue
            ? previousMacd.Value - previousSignal.Value
            : null;
        var crossover = Crossover(previousHistogram, histogram);
        var momentum = histogram switch
        {
            > 0m => AnalyticalDirection.Bullish,
            < 0m => AnalyticalDirection.Bearish,
            _ => AnalyticalDirection.Neutral
        };
        var minimum = slowPeriod + signalPeriod - 1;
        return new MacdResult(
            fastPeriod,
            slowPeriod,
            signalPeriod,
            currentMacd,
            currentSignal,
            histogram,
            previousHistogram,
            crossover,
            momentum,
            Readiness(closes.Count, minimum, slowPeriod * 3));
    }

    private static AtrResult Atr(IReadOnlyList<StoredMarketCandle> candles, int period)
    {
        var values = AtrValues(candles, period);
        return new AtrResult(
            period,
            Last(values),
            Previous(values),
            Readiness(candles.Count, period, period * 3));
    }

    private static AdxResult Adx(IReadOnlyList<StoredMarketCandle> candles, int period)
    {
        if (candles.Count < period + 1)
        {
            return new AdxResult(
                period,
                null,
                null,
                null,
                "Unavailable",
                AnalyticalDirection.Neutral,
                AnalysisReadiness.InsufficientData);
        }

        decimal smoothedTr = 0;
        decimal smoothedPlusDm = 0;
        decimal smoothedMinusDm = 0;
        for (var index = 1; index <= period; index++)
        {
            var movements = DirectionalMovement(candles[index - 1], candles[index]);
            smoothedTr += movements.TrueRange;
            smoothedPlusDm += movements.PlusDm;
            smoothedMinusDm += movements.MinusDm;
        }

        var dxValues = new List<decimal>();
        var plusDi = DirectionalIndex(smoothedPlusDm, smoothedTr);
        var minusDi = DirectionalIndex(smoothedMinusDm, smoothedTr);
        dxValues.Add(DirectionalDifference(plusDi, minusDi));

        for (var index = period + 1; index < candles.Count; index++)
        {
            var movements = DirectionalMovement(candles[index - 1], candles[index]);
            smoothedTr = smoothedTr - (smoothedTr / period) + movements.TrueRange;
            smoothedPlusDm = smoothedPlusDm - (smoothedPlusDm / period) + movements.PlusDm;
            smoothedMinusDm = smoothedMinusDm - (smoothedMinusDm / period) + movements.MinusDm;
            plusDi = DirectionalIndex(smoothedPlusDm, smoothedTr);
            minusDi = DirectionalIndex(smoothedMinusDm, smoothedTr);
            dxValues.Add(DirectionalDifference(plusDi, minusDi));
        }

        decimal? adx = null;
        if (dxValues.Count >= period)
        {
            adx = dxValues.Take(period).Average();
            for (var index = period; index < dxValues.Count; index++)
            {
                adx = ((adx.Value * (period - 1)) + dxValues[index]) / period;
            }
        }

        var strength = adx switch
        {
            null => "Unavailable",
            < 20m => "Weak",
            < 25m => "Developing",
            < 40m => "Strong",
            _ => "VeryStrong"
        };
        var direction = plusDi > minusDi ? AnalyticalDirection.Bullish
            : minusDi > plusDi ? AnalyticalDirection.Bearish
            : AnalyticalDirection.Neutral;
        return new AdxResult(
            period,
            adx,
            plusDi,
            minusDi,
            strength,
            direction,
            Readiness(candles.Count, period * 2, period * 3));
    }

    private static BollingerBandsResult Bollinger(
        IReadOnlyList<decimal> closes,
        int period,
        decimal deviations)
    {
        if (closes.Count < period)
        {
            return new BollingerBandsResult(
                period,
                deviations,
                null,
                null,
                null,
                null,
                null,
                AnalysisReadiness.InsufficientData);
        }

        var window = closes.Skip(closes.Count - period).ToArray();
        var middle = window.Average();
        var variance = window.Sum(value => (value - middle) * (value - middle)) / period;
        var standardDeviation = (decimal)Math.Sqrt((double)variance);
        var upper = middle + (deviations * standardDeviation);
        var lower = middle - (deviations * standardDeviation);
        decimal? width = middle == 0m ? null : (upper - lower) / middle;
        var percentB = upper == lower ? 0.5m : (closes[^1] - lower) / (upper - lower);
        return new BollingerBandsResult(
            period,
            deviations,
            middle,
            upper,
            lower,
            width,
            percentB,
            Readiness(closes.Count, period, period * 3));
    }

    private static StochasticResult Stochastic(
        IReadOnlyList<StoredMarketCandle> candles,
        int kPeriod,
        int dPeriod)
    {
        var kValues = new List<decimal>();
        for (var index = kPeriod - 1; index < candles.Count; index++)
        {
            var window = candles.Skip(index - kPeriod + 1).Take(kPeriod).ToArray();
            var highest = window.Max(candle => candle.High);
            var lowest = window.Min(candle => candle.Low);
            kValues.Add(highest == lowest ? 50m : ((candles[index].Close - lowest) / (highest - lowest)) * 100m);
        }

        decimal? currentD = kValues.Count >= dPeriod ? kValues.TakeLast(dPeriod).Average() : null;
        decimal? previousD = kValues.Count > dPeriod
            ? kValues.Skip(kValues.Count - dPeriod - 1).Take(dPeriod).Average()
            : null;
        decimal? currentK = kValues.Count == 0 ? null : kValues[^1];
        decimal? previousK = kValues.Count < 2 ? null : kValues[^2];
        var currentDifference = currentK.HasValue && currentD.HasValue ? currentK - currentD : null;
        var previousDifference = previousK.HasValue && previousD.HasValue ? previousK - previousD : null;
        var zone = currentK switch
        {
            null => IndicatorZone.Unavailable,
            <= 20m => IndicatorZone.Oversold,
            >= 80m => IndicatorZone.Overbought,
            _ => IndicatorZone.Neutral
        };
        var minimum = kPeriod + dPeriod - 1;
        return new StochasticResult(
            kPeriod,
            dPeriod,
            currentK,
            currentD,
            Crossover(previousDifference, currentDifference),
            zone,
            Readiness(candles.Count, minimum, kPeriod * 3));
    }

    internal static IReadOnlyList<decimal?> SmaValues(IReadOnlyList<decimal> values, int period)
    {
        var result = Enumerable.Repeat<decimal?>(null, values.Count).ToArray();
        if (period <= 0 || values.Count < period)
        {
            return result;
        }

        decimal sum = 0;
        for (var index = 0; index < values.Count; index++)
        {
            sum += values[index];
            if (index >= period)
            {
                sum -= values[index - period];
            }

            if (index >= period - 1)
            {
                result[index] = sum / period;
            }
        }

        return result;
    }

    internal static IReadOnlyList<decimal?> EmaValues(IReadOnlyList<decimal> values, int period)
    {
        var result = Enumerable.Repeat<decimal?>(null, values.Count).ToArray();
        if (period <= 0 || values.Count < period)
        {
            return result;
        }

        var current = values.Take(period).Average();
        result[period - 1] = current;
        var multiplier = 2m / (period + 1m);
        for (var index = period; index < values.Count; index++)
        {
            current = ((values[index] - current) * multiplier) + current;
            result[index] = current;
        }

        return result;
    }

    internal static IReadOnlyList<decimal?> RsiValues(IReadOnlyList<decimal> closes, int period)
    {
        var result = Enumerable.Repeat<decimal?>(null, closes.Count).ToArray();
        if (period <= 0 || closes.Count < period + 1)
        {
            return result;
        }

        decimal averageGain = 0;
        decimal averageLoss = 0;
        for (var index = 1; index <= period; index++)
        {
            var change = closes[index] - closes[index - 1];
            averageGain += Math.Max(change, 0m);
            averageLoss += Math.Max(-change, 0m);
        }

        averageGain /= period;
        averageLoss /= period;
        result[period] = RsiValue(averageGain, averageLoss);
        for (var index = period + 1; index < closes.Count; index++)
        {
            var change = closes[index] - closes[index - 1];
            var gain = Math.Max(change, 0m);
            var loss = Math.Max(-change, 0m);
            averageGain = ((averageGain * (period - 1)) + gain) / period;
            averageLoss = ((averageLoss * (period - 1)) + loss) / period;
            result[index] = RsiValue(averageGain, averageLoss);
        }

        return result;
    }

    internal static IReadOnlyList<decimal?> AtrValues(
        IReadOnlyList<StoredMarketCandle> candles,
        int period)
    {
        var result = Enumerable.Repeat<decimal?>(null, candles.Count).ToArray();
        if (period <= 0 || candles.Count < period)
        {
            return result;
        }

        var trueRanges = new decimal[candles.Count];
        trueRanges[0] = candles[0].High - candles[0].Low;
        for (var index = 1; index < candles.Count; index++)
        {
            trueRanges[index] = TrueRange(candles[index - 1].Close, candles[index]);
        }

        var current = trueRanges.Take(period).Average();
        result[period - 1] = current;
        for (var index = period; index < candles.Count; index++)
        {
            current = ((current * (period - 1)) + trueRanges[index]) / period;
            result[index] = current;
        }

        return result;
    }

    internal static decimal TrueRange(decimal previousClose, StoredMarketCandle candle) =>
        Math.Max(
            candle.High - candle.Low,
            Math.Max(Math.Abs(candle.High - previousClose), Math.Abs(candle.Low - previousClose)));

    private static (decimal TrueRange, decimal PlusDm, decimal MinusDm) DirectionalMovement(
        StoredMarketCandle previous,
        StoredMarketCandle current)
    {
        var upMove = current.High - previous.High;
        var downMove = previous.Low - current.Low;
        return (
            TrueRange(previous.Close, current),
            upMove > downMove && upMove > 0m ? upMove : 0m,
            downMove > upMove && downMove > 0m ? downMove : 0m);
    }

    private static decimal DirectionalIndex(decimal movement, decimal trueRange) =>
        trueRange == 0m ? 0m : 100m * movement / trueRange;

    private static decimal DirectionalDifference(decimal plusDi, decimal minusDi) =>
        plusDi + minusDi == 0m ? 0m : 100m * Math.Abs(plusDi - minusDi) / (plusDi + minusDi);

    private static decimal RsiValue(decimal averageGain, decimal averageLoss)
    {
        if (averageGain == 0m && averageLoss == 0m)
        {
            return 50m;
        }

        if (averageLoss == 0m)
        {
            return 100m;
        }

        var relativeStrength = averageGain / averageLoss;
        return 100m - (100m / (1m + relativeStrength));
    }

    private static AnalysisReadiness Readiness(int count, int minimum, int readyAt) =>
        count < minimum ? AnalysisReadiness.InsufficientData
        : count < readyAt ? AnalysisReadiness.WarmingUp
        : AnalysisReadiness.Ready;

    private static decimal? Last(IReadOnlyList<decimal?> values) =>
        values.Count == 0 ? null : values[^1];

    private static decimal? Previous(IReadOnlyList<decimal?> values) =>
        values.Count < 2 ? null : values[^2];

    private static CrossoverState Crossover(decimal? previousDifference, decimal? currentDifference)
    {
        if (!previousDifference.HasValue || !currentDifference.HasValue)
        {
            return CrossoverState.None;
        }

        if (previousDifference <= 0m && currentDifference > 0m)
        {
            return CrossoverState.Bullish;
        }

        return previousDifference >= 0m && currentDifference < 0m
            ? CrossoverState.Bearish
            : CrossoverState.None;
    }
}
