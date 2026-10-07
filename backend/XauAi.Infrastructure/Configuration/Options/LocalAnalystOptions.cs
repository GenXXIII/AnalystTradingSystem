namespace XauAi.Infrastructure.Configuration.Options;

public sealed class LocalAnalystOptions
{
    public const string SectionName = "LocalAnalyst";

    public bool Enabled { get; set; }

    public string Symbol { get; set; } = "XAUUSD";

    public string Timeframes { get; set; } = "M1,M5,M15,M30,H1,H4,D1";

    public int EvaluationIntervalSeconds { get; set; } = 30;

    public int HistoryLimit { get; set; } = 250;

    public int MinimumCandles { get; set; } = 205;

    public int StaleAfterIntervals { get; set; } = 3;

    public int MaximumAllowedGaps { get; set; }

    public decimal EntryScoreThreshold { get; set; } = 4m;

    public decimal MinimumDirectionalLead { get; set; } = 1m;

    public decimal StopOpposingScoreThreshold { get; set; } = 4m;

    public decimal RsiBullishMinimum { get; set; } = 50m;

    public decimal RsiBullishMaximum { get; set; } = 75m;

    public decimal RsiBearishMinimum { get; set; } = 25m;

    public decimal RsiBearishMaximum { get; set; } = 50m;

    public bool AllowVeryHighVolatility { get; set; }

    public decimal StructureWeight { get; set; } = 1m;

    public decimal TrendWeight { get; set; } = 1m;

    public decimal LiquidityWeight { get; set; } = 1m;

    public decimal CandleWeight { get; set; } = 1m;

    public decimal MomentumWeight { get; set; } = 1m;

    public decimal KtrWeight { get; set; } = 1m;

    public decimal InvalidationAtrMultiplier { get; set; } = 1.5m;

    public decimal TargetAtrMultiplier { get; set; } = 2m;

    public decimal EqualLevelToleranceAtr { get; set; } = 0.15m;

    public decimal ImportantLevelDistanceAtr { get; set; } = 0.5m;

    public string ValidityCandles { get; set; } = "M1:15,M5:12,M15:8,M30:6,H1:5,H4:4,D1:3";

    public string ConfigurationVersion { get; set; } = "phase12-v1";
}
