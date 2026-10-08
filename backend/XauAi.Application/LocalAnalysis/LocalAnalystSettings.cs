using XauAi.Application.MarketData;

namespace XauAi.Application.LocalAnalysis;

public sealed class LocalAnalystSettings
{
    public bool Enabled { get; init; }

    public string Symbol { get; init; } = "XAUUSD";

    public IReadOnlyList<MarketTimeframe> Timeframes { get; init; } = Enum.GetValues<MarketTimeframe>();

    public int EvaluationIntervalSeconds { get; init; } = 30;

    public int HistoryLimit { get; init; } = 250;

    public int MinimumCandles { get; init; } = 205;

    public int StaleAfterIntervals { get; init; } = 3;

    public int MaximumAllowedGaps { get; init; } = 30;

    public decimal EntryScoreThreshold { get; init; } = 4m;

    public decimal MinimumDirectionalLead { get; init; } = 2m;

    public decimal StopOpposingScoreThreshold { get; init; } = 4m;

    public decimal RsiBullishMinimum { get; init; } = 50m;

    public decimal RsiBullishMaximum { get; init; } = 75m;

    public decimal RsiBearishMinimum { get; init; } = 25m;

    public decimal RsiBearishMaximum { get; init; } = 50m;

    public bool AllowVeryHighVolatility { get; init; }

    public decimal StructureWeight { get; init; } = 1m;

    public decimal TrendWeight { get; init; } = 1m;

    public decimal LiquidityWeight { get; init; } = 1m;

    public decimal CandleWeight { get; init; } = 1m;

    public decimal MomentumWeight { get; init; } = 1m;

    public decimal KtrWeight { get; init; } = 1m;

    public decimal InvalidationAtrMultiplier { get; init; } = 1.5m;

    public decimal TargetAtrMultiplier { get; init; } = 2m;

    public decimal EqualLevelToleranceAtr { get; init; } = 0.15m;

    public decimal ImportantLevelDistanceAtr { get; init; } = 0.5m;

    public string ConfigurationVersion { get; init; } = "phase12-v3";

    public IReadOnlyDictionary<MarketTimeframe, int> ValidityCandles { get; init; } =
        new Dictionary<MarketTimeframe, int>
        {
            [MarketTimeframe.M1] = 15,
            [MarketTimeframe.M5] = 12,
            [MarketTimeframe.M15] = 8,
            [MarketTimeframe.M30] = 6,
            [MarketTimeframe.H1] = 5,
            [MarketTimeframe.H4] = 4,
            [MarketTimeframe.D1] = 3
        };

    public decimal MaximumScore => StructureWeight + TrendWeight + LiquidityWeight
        + CandleWeight + MomentumWeight + KtrWeight;

    public int ValidityFor(MarketTimeframe timeframe) =>
        ValidityCandles.TryGetValue(timeframe, out var candles) ? candles : 5;
}
