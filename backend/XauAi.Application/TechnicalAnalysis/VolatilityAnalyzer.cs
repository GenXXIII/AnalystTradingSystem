using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class VolatilityAnalyzer : IVolatilityAnalyzer
{
    public VolatilityAnalysisResult Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        IndicatorSet indicators,
        TechnicalAnalysisSettings settings)
    {
        if (candles.Count == 0)
        {
            return new VolatilityAnalysisResult(
                VolatilityRegime.Unavailable,
                null,
                0m,
                indicators.Atr,
                indicators.BollingerBands,
                []);
        }

        var ranges = candles.TakeLast(Math.Min(settings.VolatilityLookback, candles.Count))
            .Select(candle => candle.High - candle.Low)
            .ToArray();
        var averageRange = ranges.Length == 0 ? 0m : ranges.Average();
        var currentRangeRatio = averageRange == 0m ? 0m : ranges[^1] / averageRange;
        decimal? atrRatio = null;
        var atrSeries = IndicatorCalculator.AtrValues(candles, settings.AtrPeriod)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .TakeLast(settings.VolatilityLookback)
            .ToArray();
        if (indicators.Atr.Value.HasValue && atrSeries.Length > 0 && atrSeries.Average() > 0m)
        {
            atrRatio = indicators.Atr.Value.Value / atrSeries.Average();
        }

        var ratio = atrRatio ?? currentRangeRatio;
        var regime = indicators.Atr.Value is null
            ? VolatilityRegime.Unavailable
            : ratio <= settings.VeryLowVolatilityRatio ? VolatilityRegime.VeryLow
            : ratio <= settings.LowVolatilityRatio ? VolatilityRegime.Low
            : ratio < settings.HighVolatilityRatio ? VolatilityRegime.Normal
            : ratio < settings.VeryHighVolatilityRatio ? VolatilityRegime.High
            : VolatilityRegime.VeryHigh;
        var evidence = new List<string> { $"Regime:{regime}" };
        if (atrRatio.HasValue)
        {
            evidence.Add($"AtrVsBaseline:{atrRatio.Value:F3}");
        }

        if (indicators.BollingerBands.Width.HasValue)
        {
            evidence.Add($"BollingerWidth:{indicators.BollingerBands.Width.Value:F6}");
        }

        return new VolatilityAnalysisResult(
            regime,
            atrRatio,
            currentRangeRatio,
            indicators.Atr,
            indicators.BollingerBands,
            evidence);
    }
}
