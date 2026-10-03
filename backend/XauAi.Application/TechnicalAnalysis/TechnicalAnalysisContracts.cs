using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

public interface ITechnicalAnalysisService
{
    Task<TechnicalAnalysisResult> AnalyzeAsync(
        TechnicalAnalysisRequest request,
        CancellationToken cancellationToken = default);

    Task<MultiTimeframeAnalysisResult> AnalyzeMultiTimeframeAsync(
        string symbol,
        DateTimeOffset? atUtc = null,
        CancellationToken cancellationToken = default);
}

public interface IIndicatorCalculator
{
    IndicatorSet Calculate(
        IReadOnlyList<StoredMarketCandle> candles,
        TechnicalAnalysisSettings settings);
}

public interface ICandlestickAnalyzer
{
    (CandleQuality? LatestQuality, IReadOnlyList<CandlestickPatternResult> Patterns) Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe);
}

public interface IMarketStructureAnalyzer
{
    MarketStructureResult Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        int swingWindow);
}

public interface ISupportResistanceAnalyzer
{
    IReadOnlyList<SupportResistanceZone> Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe,
        IReadOnlyList<MarketSwing> swings,
        TechnicalAnalysisSettings settings);
}

public interface IPriceActionAnalyzer
{
    PriceActionResult Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        TechnicalAnalysisSettings settings);
}

public interface IVolatilityAnalyzer
{
    VolatilityAnalysisResult Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        IndicatorSet indicators,
        TechnicalAnalysisSettings settings);
}
