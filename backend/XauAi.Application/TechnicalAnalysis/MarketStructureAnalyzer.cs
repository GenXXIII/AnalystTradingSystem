using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class MarketStructureAnalyzer : IMarketStructureAnalyzer
{
    public MarketStructureResult Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        int swingWindow)
    {
        if (candles.Count < (swingWindow * 2) + 1)
        {
            return new MarketStructureResult(
                AnalyticalDirection.Neutral,
                "InsufficientData",
                [],
                AnalysisReadiness.InsufficientData);
        }

        var swings = new List<MarketSwing>();
        decimal? previousHigh = null;
        decimal? previousLow = null;
        for (var index = swingWindow; index < candles.Count - swingWindow; index++)
        {
            var current = candles[index];
            var neighbors = candles.Skip(index - swingWindow).Take((swingWindow * 2) + 1).ToArray();
            var isHigh = neighbors.Where((_, neighborIndex) => neighborIndex != swingWindow)
                .All(candle => current.High > candle.High);
            var isLow = neighbors.Where((_, neighborIndex) => neighborIndex != swingWindow)
                .All(candle => current.Low < candle.Low);
            if (isHigh)
            {
                var classification = previousHigh switch
                {
                    null => SwingClassification.SwingHigh,
                    var value when current.High > value => SwingClassification.HigherHigh,
                    var value when current.High < value => SwingClassification.LowerHigh,
                    _ => SwingClassification.EqualHigh
                };
                swings.Add(new MarketSwing(SwingKind.High, classification, current.OpenTimeUtc, current.High, index));
                previousHigh = current.High;
            }

            if (isLow)
            {
                var classification = previousLow switch
                {
                    null => SwingClassification.SwingLow,
                    var value when current.Low > value => SwingClassification.HigherLow,
                    var value when current.Low < value => SwingClassification.LowerLow,
                    _ => SwingClassification.EqualLow
                };
                swings.Add(new MarketSwing(SwingKind.Low, classification, current.OpenTimeUtc, current.Low, index));
                previousLow = current.Low;
            }
        }

        var recent = swings.TakeLast(6).ToArray();
        var hasHigherHigh = recent.Any(swing => swing.Classification == SwingClassification.HigherHigh);
        var hasHigherLow = recent.Any(swing => swing.Classification == SwingClassification.HigherLow);
        var hasLowerHigh = recent.Any(swing => swing.Classification == SwingClassification.LowerHigh);
        var hasLowerLow = recent.Any(swing => swing.Classification == SwingClassification.LowerLow);
        var direction = hasHigherHigh && hasHigherLow && !(hasLowerHigh && hasLowerLow)
            ? AnalyticalDirection.Bullish
            : hasLowerHigh && hasLowerLow && !(hasHigherHigh && hasHigherLow)
                ? AnalyticalDirection.Bearish
                : recent.Length < 2 ? AnalyticalDirection.Neutral
                : AnalyticalDirection.Conflicting;
        var structure = direction switch
        {
            AnalyticalDirection.Bullish => "HigherHighsAndHigherLows",
            AnalyticalDirection.Bearish => "LowerHighsAndLowerLows",
            AnalyticalDirection.Conflicting => "MixedStructure",
            _ => "Unconfirmed"
        };
        return new MarketStructureResult(
            direction,
            structure,
            recent,
            swings.Count < 2 ? AnalysisReadiness.WarmingUp : AnalysisReadiness.Ready);
    }
}
