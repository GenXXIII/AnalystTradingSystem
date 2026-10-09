using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

public enum AnalysisReadiness
{
    InsufficientData,
    WarmingUp,
    Ready
}

public enum AnalyticalDirection
{
    Bullish,
    Bearish,
    Neutral,
    Transition,
    Conflicting
}

public enum IndicatorZone
{
    Unavailable,
    Oversold,
    Neutral,
    Overbought
}

public enum CrossoverState
{
    None,
    Bullish,
    Bearish
}

public enum VolatilityRegime
{
    Unavailable,
    VeryLow,
    Low,
    Normal,
    High,
    VeryHigh
}

public enum PatternDirection
{
    Bullish,
    Bearish,
    Neutral
}

public enum SwingKind
{
    High,
    Low
}

public enum SwingClassification
{
    SwingHigh,
    SwingLow,
    HigherHigh,
    HigherLow,
    LowerHigh,
    LowerLow,
    EqualHigh,
    EqualLow
}

public enum PriceZoneType
{
    Support,
    Resistance,
    Pivot
}

public sealed record TechnicalAnalysisRequest(
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset? AtUtc = null);

public sealed record IndicatorValueResult(
    string Name,
    int Period,
    decimal? Value,
    decimal? PreviousValue,
    AnalysisReadiness Readiness);

public sealed record RsiResult(
    int Period,
    decimal? Value,
    decimal? PreviousValue,
    IndicatorZone Zone,
    AnalyticalDirection Momentum,
    AnalysisReadiness Readiness);

public sealed record MacdResult(
    int FastPeriod,
    int SlowPeriod,
    int SignalPeriod,
    decimal? Macd,
    decimal? Signal,
    decimal? Histogram,
    decimal? PreviousHistogram,
    CrossoverState Crossover,
    AnalyticalDirection Momentum,
    AnalysisReadiness Readiness);

public sealed record AtrResult(
    int Period,
    decimal? Value,
    decimal? PreviousValue,
    AnalysisReadiness Readiness);

public sealed record AdxResult(
    int Period,
    decimal? Adx,
    decimal? PlusDi,
    decimal? MinusDi,
    string Strength,
    AnalyticalDirection DirectionalBias,
    AnalysisReadiness Readiness);

public sealed record BollingerBandsResult(
    int Period,
    decimal StandardDeviations,
    decimal? Middle,
    decimal? Upper,
    decimal? Lower,
    decimal? Width,
    decimal? PercentB,
    AnalysisReadiness Readiness);

public sealed record StochasticResult(
    int KPeriod,
    int DPeriod,
    decimal? PercentK,
    decimal? PercentD,
    CrossoverState Crossover,
    IndicatorZone Zone,
    AnalysisReadiness Readiness);

public sealed record IndicatorSet(
    IReadOnlyList<IndicatorValueResult> Sma,
    IReadOnlyList<IndicatorValueResult> Ema,
    RsiResult Rsi,
    MacdResult Macd,
    AtrResult Atr,
    AdxResult Adx,
    BollingerBandsResult BollingerBands,
    StochasticResult Stochastic);

public sealed record CandleQuality(
    DateTimeOffset CandleTimeUtc,
    decimal Body,
    decimal UpperWick,
    decimal LowerWick,
    decimal Range,
    decimal BodyToRangeRatio,
    decimal UpperWickToBodyRatio,
    decimal LowerWickToBodyRatio,
    decimal RelativeRange);

public sealed record CandlestickPatternResult(
    string Pattern,
    PatternDirection Direction,
    MarketTimeframe Timeframe,
    DateTimeOffset CandleTimeUtc,
    decimal Quality,
    IReadOnlyList<string> SupportingConditions);

public sealed record MarketSwing(
    SwingKind Kind,
    SwingClassification Classification,
    DateTimeOffset CandleTimeUtc,
    decimal Price,
    int CandleIndex);

public sealed record MarketStructureResult(
    AnalyticalDirection Direction,
    string Structure,
    IReadOnlyList<MarketSwing> Swings,
    AnalysisReadiness Readiness);

public sealed record SupportResistanceZone(
    decimal Lower,
    decimal Upper,
    decimal Center,
    PriceZoneType Type,
    string Strength,
    int Touches,
    IReadOnlyList<MarketTimeframe> Timeframes,
    decimal DistanceFromPricePercent,
    string Source = "SwingCluster",
    DateTimeOffset? CreatedAtUtc = null,
    string Status = "Active");

public sealed record PriceActionResult(
    bool RangeExpansion,
    bool RangeContraction,
    bool BreakoutAboveRecentRange,
    bool BreakoutBelowRecentRange,
    bool BullishRejection,
    bool BearishRejection,
    bool MomentumCandle,
    bool InsideRange,
    bool Consolidating,
    IReadOnlyList<string> Evidence);

public sealed record TrendAnalysisResult(
    AnalyticalDirection Direction,
    string Strength,
    string EmaAlignment,
    string PriceVsEma,
    IReadOnlyList<IndicatorValueResult> Sma,
    IReadOnlyList<IndicatorValueResult> Ema);

public sealed record MomentumAnalysisResult(
    RsiResult Rsi,
    MacdResult Macd,
    StochasticResult Stochastic);

public sealed record VolatilityAnalysisResult(
    VolatilityRegime Regime,
    decimal? AtrRelativeToBaseline,
    decimal CurrentRangeRelativeToAverage,
    AtrResult Atr,
    BollingerBandsResult BollingerBands,
    IReadOnlyList<string> Evidence);

public sealed record TechnicalConfluenceResult(
    IReadOnlyList<string> TrendEvidence,
    IReadOnlyList<string> MomentumEvidence,
    IReadOnlyList<string> StructureEvidence,
    IReadOnlyList<string> LevelEvidence,
    IReadOnlyList<string> PatternEvidence,
    IReadOnlyList<string> VolatilityEvidence,
    IReadOnlyList<string> Conflicts);

public sealed record StrategySetupResult(
    string Family,
    string Strategy,
    AnalyticalDirection Direction,
    string State,
    decimal Quality,
    IReadOnlyList<string> Evidence);

public sealed record FlowFeatureResult(
    string DataMethod,
    bool TrueBidAskDeltaAvailable,
    decimal? TickVolumeRatio,
    decimal? DirectionalPressure,
    AnalyticalDirection Direction,
    string Divergence,
    bool AbsorptionCandidate,
    bool ExhaustionCandidate,
    bool BreakoutConfirmed,
    IReadOnlyList<string> Evidence);

public sealed record KtrLevelResult(
    string Label,
    decimal Price,
    int Multiple);

public sealed record KtrLevelSet(
    decimal OpeningPrice,
    decimal Unit,
    string Method,
    IReadOnlyList<KtrLevelResult> Levels);

public sealed record StrategyAnalysisResult(
    IReadOnlyList<StrategySetupResult> Setups,
    FlowFeatureResult Flow,
    KtrLevelSet Ktr,
    IReadOnlyList<string> CoveredFamilies,
    IReadOnlyList<string> UnavailableCapabilities);

public sealed record AnalysisDiagnostics(
    long DurationMilliseconds,
    int CandlesRead,
    int CandlesUsed,
    int DuplicateCandlesIgnored,
    int InvalidCandlesIgnored,
    int MissingIntervalCount,
    int IndicatorsCalculated,
    int InsufficientIndicators,
    DateTimeOffset DataCutoffUtc);

public sealed record TechnicalAnalysisResult(
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset AnalyzedAtUtc,
    DateTimeOffset? LastCandleCloseTimeUtc,
    TrendAnalysisResult Trend,
    MomentumAnalysisResult Momentum,
    VolatilityAnalysisResult Volatility,
    MarketStructureResult MarketStructure,
    IReadOnlyList<SupportResistanceZone> SupportResistance,
    IReadOnlyList<CandlestickPatternResult> CandlestickPatterns,
    CandleQuality? LatestCandleQuality,
    PriceActionResult PriceAction,
    IndicatorSet Indicators,
    TechnicalConfluenceResult Confluence,
    IReadOnlyList<string> Conflicts,
    AnalysisDiagnostics Diagnostics)
{
    public StrategyAnalysisResult? Strategies { get; init; }
}

public sealed record TimeframeAnalysisSummary(
    MarketTimeframe Timeframe,
    AnalyticalDirection Trend,
    AnalyticalDirection Structure,
    AnalyticalDirection Momentum,
    VolatilityRegime Volatility,
    DateTimeOffset? LastCandleCloseTimeUtc,
    int CandlesUsed);

public sealed record MultiTimeframeAnalysisResult(
    string Symbol,
    DateTimeOffset AnalyzedAtUtc,
    IReadOnlyList<TimeframeAnalysisSummary> Timeframes,
    string TrendAlignment,
    IReadOnlyList<string> Agreements,
    IReadOnlyList<string> Conflicts,
    IReadOnlyDictionary<MarketTimeframe, TechnicalAnalysisResult> Analyses,
    long DurationMilliseconds);
