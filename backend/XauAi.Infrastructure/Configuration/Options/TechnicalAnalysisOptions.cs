namespace XauAi.Infrastructure.Configuration.Options;

public sealed class TechnicalAnalysisOptions
{
    public const string SectionName = "TechnicalAnalysis";

    public bool Enabled { get; set; }

    public string Symbol { get; set; } = "XAUUSD";

    public string Timeframes { get; set; } = "M1,M5,M15,M30,H1,H4,D1";

    public int HistoryLimit { get; set; } = 1000;

    public string SmaPeriods { get; set; } = "20,50,200";

    public string EmaPeriods { get; set; } = "9,20,50,100,200";

    public int RsiPeriod { get; set; } = 14;

    public int MacdFastPeriod { get; set; } = 12;

    public int MacdSlowPeriod { get; set; } = 26;

    public int MacdSignalPeriod { get; set; } = 9;

    public int AtrPeriod { get; set; } = 14;

    public int AdxPeriod { get; set; } = 14;

    public int BollingerPeriod { get; set; } = 20;

    public decimal BollingerStandardDeviations { get; set; } = 2m;

    public int StochasticKPeriod { get; set; } = 14;

    public int StochasticDPeriod { get; set; } = 3;

    public int SwingWindow { get; set; } = 2;

    public decimal LevelTolerancePercent { get; set; } = 0.15m;

    public int MinimumLevelTouches { get; set; } = 2;

    public int MaximumLevelZones { get; set; } = 8;

    public int PriceActionLookback { get; set; } = 20;

    public int VolatilityLookback { get; set; } = 50;

    public decimal VeryLowVolatilityRatio { get; set; } = 0.60m;

    public decimal LowVolatilityRatio { get; set; } = 0.80m;

    public decimal HighVolatilityRatio { get; set; } = 1.20m;

    public decimal VeryHighVolatilityRatio { get; set; } = 1.50m;
}
