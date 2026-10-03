using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

public sealed class TechnicalAnalysisSettings
{
    public bool Enabled { get; init; } = true;

    public string Symbol { get; init; } = "XAUUSD";

    public IReadOnlyList<MarketTimeframe> Timeframes { get; init; } = Enum.GetValues<MarketTimeframe>();

    public int HistoryLimit { get; init; } = 1000;

    public IReadOnlyList<int> SmaPeriods { get; init; } = [20, 50, 200];

    public IReadOnlyList<int> EmaPeriods { get; init; } = [9, 20, 50, 100, 200];

    public int RsiPeriod { get; init; } = 14;

    public int MacdFastPeriod { get; init; } = 12;

    public int MacdSlowPeriod { get; init; } = 26;

    public int MacdSignalPeriod { get; init; } = 9;

    public int AtrPeriod { get; init; } = 14;

    public int AdxPeriod { get; init; } = 14;

    public int BollingerPeriod { get; init; } = 20;

    public decimal BollingerStandardDeviations { get; init; } = 2m;

    public int StochasticKPeriod { get; init; } = 14;

    public int StochasticDPeriod { get; init; } = 3;

    public int SwingWindow { get; init; } = 2;

    public decimal LevelTolerancePercent { get; init; } = 0.15m;

    public int MinimumLevelTouches { get; init; } = 2;

    public int MaximumLevelZones { get; init; } = 8;

    public int PriceActionLookback { get; init; } = 20;

    public int VolatilityLookback { get; init; } = 50;

    public decimal VeryLowVolatilityRatio { get; init; } = 0.60m;

    public decimal LowVolatilityRatio { get; init; } = 0.80m;

    public decimal HighVolatilityRatio { get; init; } = 1.20m;

    public decimal VeryHighVolatilityRatio { get; init; } = 1.50m;
}
