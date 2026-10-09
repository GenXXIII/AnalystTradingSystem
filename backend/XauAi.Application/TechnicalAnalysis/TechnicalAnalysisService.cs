using System.Diagnostics;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class TechnicalAnalysisService(
    IMarketDataQueryStore marketData,
    IIndicatorCalculator indicatorCalculator,
    ICandlestickAnalyzer candlestickAnalyzer,
    IMarketStructureAnalyzer marketStructureAnalyzer,
    ISupportResistanceAnalyzer supportResistanceAnalyzer,
    IPriceActionAnalyzer priceActionAnalyzer,
    IVolatilityAnalyzer volatilityAnalyzer,
    IStrategyFeatureAnalyzer strategyFeatureAnalyzer,
    IMarketSessionCalendar sessionCalendar,
    TechnicalAnalysisSettings settings,
    TimeProvider timeProvider,
    ILogger<TechnicalAnalysisService> logger) : ITechnicalAnalysisService
{
    public async Task<TechnicalAnalysisResult> AnalyzeAsync(
        TechnicalAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.Symbol, request.Timeframe);
        var now = timeProvider.GetUtcNow();
        var cutoff = (request.AtUtc ?? now).ToUniversalTime();
        if (cutoff > now.AddMinutes(1))
        {
            throw new TechnicalAnalysisException(
                TechnicalAnalysisErrorCodes.InvalidRequest,
                "The analysis cutoff cannot be in the future.");
        }

        var stopwatch = Stopwatch.StartNew();
        var source = await marketData.GetHistoryUpToAsync(
            settings.Symbol,
            request.Timeframe,
            cutoff,
            settings.HistoryLimit,
            cancellationToken);
        var normalized = Normalize(source, cutoff);
        if (normalized.Candles.Count == 0)
        {
            throw new TechnicalAnalysisException(
                TechnicalAnalysisErrorCodes.NoData,
                "No completed valid market candles are available at the requested cutoff.");
        }

        var indicators = indicatorCalculator.Calculate(normalized.Candles, settings);
        var trend = BuildTrend(normalized.Candles, indicators);
        var momentum = new MomentumAnalysisResult(indicators.Rsi, indicators.Macd, indicators.Stochastic);
        var structure = marketStructureAnalyzer.Analyze(normalized.Candles, settings.SwingWindow);
        var levels = supportResistanceAnalyzer.Analyze(
            normalized.Candles,
            request.Timeframe,
            structure.Swings,
            settings);
        var candleAnalysis = candlestickAnalyzer.Analyze(normalized.Candles, request.Timeframe);
        var priceAction = priceActionAnalyzer.Analyze(normalized.Candles, settings);
        var volatility = volatilityAnalyzer.Analyze(normalized.Candles, indicators, settings);
        var strategies = strategyFeatureAnalyzer.Analyze(
            normalized.Candles,
            request.Timeframe,
            indicators,
            trend,
            structure,
            priceAction,
            volatility,
            levels,
            candleAnalysis.Patterns);
        var conflicts = DetectConflicts(trend, momentum, structure, candleAnalysis.Patterns);
        var confluence = BuildConfluence(
            trend,
            momentum,
            structure,
            levels,
            candleAnalysis.Patterns,
            volatility,
            conflicts);
        stopwatch.Stop();
        var gapCount = MarketDataGapDetector.Detect(
            settings.Symbol,
            request.Timeframe,
            normalized.Candles.Select(candle => candle.OpenTimeUtc),
            sessionCalendar,
            settings.HistoryLimit).Count;
        var insufficient = CountInsufficient(indicators);
        var result = new TechnicalAnalysisResult(
            settings.Symbol,
            request.Timeframe,
            cutoff,
            normalized.Candles[^1].CloseTimeUtc,
            trend,
            momentum,
            volatility,
            structure,
            levels,
            candleAnalysis.Patterns,
            candleAnalysis.LatestQuality,
            priceAction,
            indicators,
            confluence,
            conflicts,
            new AnalysisDiagnostics(
                stopwatch.ElapsedMilliseconds,
                source.Count,
                normalized.Candles.Count,
                normalized.DuplicateCount,
                normalized.InvalidCount,
                gapCount,
                CountCalculated(indicators),
                insufficient,
                cutoff))
        {
            Strategies = strategies
        };

        logger.LogInformation(
            "Technical analysis completed for {Symbol} {Timeframe} at {CutoffUtc} in {DurationMilliseconds} ms using {CandleCount} candles; {InsufficientCount} indicators insufficient",
            result.Symbol,
            result.Timeframe,
            cutoff,
            stopwatch.ElapsedMilliseconds,
            normalized.Candles.Count,
            insufficient);
        return result;
    }

    public async Task<MultiTimeframeAnalysisResult> AnalyzeMultiTimeframeAsync(
        string symbol,
        DateTimeOffset? atUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
        {
            throw Disabled();
        }

        if (!string.Equals(symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new TechnicalAnalysisException(
                TechnicalAnalysisErrorCodes.InvalidRequest,
                "The requested market symbol is not configured for technical analysis.");
        }

        var cutoff = (atUtc ?? timeProvider.GetUtcNow()).ToUniversalTime();
        var stopwatch = Stopwatch.StartNew();
        var analyses = new Dictionary<MarketTimeframe, TechnicalAnalysisResult>();
        foreach (var timeframe in settings.Timeframes.OrderByDescending(TimeframeRank))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                analyses[timeframe] = await AnalyzeAsync(
                    new TechnicalAnalysisRequest(settings.Symbol, timeframe, cutoff),
                    cancellationToken);
            }
            catch (TechnicalAnalysisException exception) when (exception.Code == TechnicalAnalysisErrorCodes.NoData)
            {
                logger.LogWarning(
                    "No completed candles were available for multi-timeframe analysis of {Symbol} {Timeframe}",
                    settings.Symbol,
                    timeframe);
            }
        }

        if (analyses.Count == 0)
        {
            throw new TechnicalAnalysisException(
                TechnicalAnalysisErrorCodes.NoData,
                "No completed market candles are available for multi-timeframe analysis.");
        }

        var summaries = analyses.Values
            .OrderByDescending(result => TimeframeRank(result.Timeframe))
            .Select(result => new TimeframeAnalysisSummary(
                result.Timeframe,
                result.Trend.Direction,
                result.MarketStructure.Direction,
                MomentumDirection(result.Momentum),
                result.Volatility.Regime,
                result.LastCandleCloseTimeUtc,
                result.Diagnostics.CandlesUsed))
            .ToArray();
        var bullish = summaries.Count(summary => summary.Trend == AnalyticalDirection.Bullish);
        var bearish = summaries.Count(summary => summary.Trend == AnalyticalDirection.Bearish);
        var alignment = bullish == summaries.Length ? "BullishAligned"
            : bearish == summaries.Length ? "BearishAligned"
            : bullish > 0 && bearish > 0 ? "Conflicting"
            : "MixedOrUnconfirmed";
        var agreements = new List<string>();
        var conflicts = new List<string>();
        if (bullish >= 2)
        {
            agreements.Add($"{bullish}TimeframesBullishTrend");
        }

        if (bearish >= 2)
        {
            agreements.Add($"{bearish}TimeframesBearishTrend");
        }

        if (bullish > 0 && bearish > 0)
        {
            conflicts.Add("TimeframeTrendDisagreement");
        }

        foreach (var summary in summaries.Where(value =>
                     value.Trend is AnalyticalDirection.Bullish or AnalyticalDirection.Bearish
                     && value.Momentum is AnalyticalDirection.Bullish or AnalyticalDirection.Bearish
                     && value.Trend != value.Momentum))
        {
            conflicts.Add($"{summary.Timeframe}:TrendMomentumDisagreement");
        }

        stopwatch.Stop();
        return new MultiTimeframeAnalysisResult(
            settings.Symbol,
            cutoff,
            summaries,
            alignment,
            agreements,
            conflicts,
            analyses,
            stopwatch.ElapsedMilliseconds);
    }

    private void Validate(string symbol, MarketTimeframe timeframe)
    {
        if (!settings.Enabled)
        {
            throw Disabled();
        }

        if (!string.Equals(symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new TechnicalAnalysisException(
                TechnicalAnalysisErrorCodes.InvalidRequest,
                "The requested market symbol is not configured for technical analysis.");
        }

        if (!settings.Timeframes.Contains(timeframe))
        {
            throw new TechnicalAnalysisException(
                TechnicalAnalysisErrorCodes.InvalidRequest,
                "The requested timeframe is not enabled for technical analysis.");
        }
    }

    private static (IReadOnlyList<StoredMarketCandle> Candles, int DuplicateCount, int InvalidCount) Normalize(
        IReadOnlyList<StoredMarketCandle> source,
        DateTimeOffset cutoff)
    {
        var invalidCount = source.Count(candle => !IsValid(candle, cutoff));
        var valid = source.Where(candle => IsValid(candle, cutoff)).ToArray();
        var grouped = valid.GroupBy(candle => candle.OpenTimeUtc.ToUniversalTime()).ToArray();
        var candles = grouped
            .Select(group => group.OrderByDescending(candle => candle.FetchedAtUtc).First())
            .OrderBy(candle => candle.OpenTimeUtc)
            .ToArray();
        return (candles, valid.Length - grouped.Length, invalidCount);
    }

    private static bool IsValid(StoredMarketCandle candle, DateTimeOffset cutoff) =>
        candle.IsComplete
        && candle.CloseTimeUtc.ToUniversalTime() <= cutoff
        && candle.OpenTimeUtc < candle.CloseTimeUtc
        && candle.Open > 0m
        && candle.High > 0m
        && candle.Low > 0m
        && candle.Close > 0m
        && candle.High >= candle.Low
        && candle.High >= Math.Max(candle.Open, candle.Close)
        && candle.Low <= Math.Min(candle.Open, candle.Close);

    private static TrendAnalysisResult BuildTrend(
        IReadOnlyList<StoredMarketCandle> candles,
        IndicatorSet indicators)
    {
        var available = indicators.Ema.Where(value => value.Value.HasValue).OrderBy(value => value.Period).ToArray();
        var bullishAlignment = available.Length >= 3
            && available.Zip(available.Skip(1)).All(pair => pair.First.Value > pair.Second.Value);
        var bearishAlignment = available.Length >= 3
            && available.Zip(available.Skip(1)).All(pair => pair.First.Value < pair.Second.Value);
        var alignment = bullishAlignment ? "BullishStack"
            : bearishAlignment ? "BearishStack"
            : available.Length < 3 ? "InsufficientData"
            : "Mixed";
        var price = candles[^1].Close;
        var priceVs = available.Length == 0 ? "InsufficientData"
            : available.All(value => price > value.Value) ? "AboveAllAvailableEma"
            : available.All(value => price < value.Value) ? "BelowAllAvailableEma"
            : "Mixed";
        var reference = available.FirstOrDefault(value => value.Period == 20) ?? available.FirstOrDefault();
        var slope = reference?.Value - reference?.PreviousValue;
        var bullishEvidence = bullishAlignment && priceVs == "AboveAllAvailableEma" && slope > 0m;
        var bearishEvidence = bearishAlignment && priceVs == "BelowAllAvailableEma" && slope < 0m;
        var contradictory = (bullishAlignment && (priceVs == "BelowAllAvailableEma" || slope < 0m))
            || (bearishAlignment && (priceVs == "AboveAllAvailableEma" || slope > 0m));
        var direction = bullishEvidence ? AnalyticalDirection.Bullish
            : bearishEvidence ? AnalyticalDirection.Bearish
            : contradictory ? AnalyticalDirection.Conflicting
            : available.Length < 3 ? AnalyticalDirection.Neutral
            : AnalyticalDirection.Transition;
        return new TrendAnalysisResult(
            direction,
            indicators.Adx.Strength,
            alignment,
            priceVs,
            indicators.Sma,
            indicators.Ema);
    }

    private static AnalyticalDirection MomentumDirection(MomentumAnalysisResult momentum)
    {
        var values = new[] { momentum.Rsi.Momentum, momentum.Macd.Momentum }
            .Where(value => value is AnalyticalDirection.Bullish or AnalyticalDirection.Bearish)
            .Distinct()
            .ToArray();
        return values.Length > 1 ? AnalyticalDirection.Conflicting
            : values.Length == 1 ? values[0]
            : AnalyticalDirection.Neutral;
    }

    private static IReadOnlyList<string> DetectConflicts(
        TrendAnalysisResult trend,
        MomentumAnalysisResult momentum,
        MarketStructureResult structure,
        IReadOnlyList<CandlestickPatternResult> patterns)
    {
        var conflicts = new List<string>();
        var momentumDirection = MomentumDirection(momentum);
        if (Opposed(trend.Direction, momentumDirection))
        {
            conflicts.Add("TrendMomentumDisagreement");
        }

        if (Opposed(trend.Direction, structure.Direction))
        {
            conflicts.Add("TrendStructureDisagreement");
        }

        if (patterns.Any(pattern =>
                trend.Direction == AnalyticalDirection.Bullish && pattern.Direction == PatternDirection.Bearish
                || trend.Direction == AnalyticalDirection.Bearish && pattern.Direction == PatternDirection.Bullish))
        {
            conflicts.Add("CandlestickPatternAgainstTrend");
        }

        if (momentum.Rsi.Momentum != momentum.Macd.Momentum
            && momentum.Rsi.Momentum is AnalyticalDirection.Bullish or AnalyticalDirection.Bearish
            && momentum.Macd.Momentum is AnalyticalDirection.Bullish or AnalyticalDirection.Bearish)
        {
            conflicts.Add("RsiMacdDisagreement");
        }

        return conflicts;
    }

    private static TechnicalConfluenceResult BuildConfluence(
        TrendAnalysisResult trend,
        MomentumAnalysisResult momentum,
        MarketStructureResult structure,
        IReadOnlyList<SupportResistanceZone> levels,
        IReadOnlyList<CandlestickPatternResult> patterns,
        VolatilityAnalysisResult volatility,
        IReadOnlyList<string> conflicts) =>
        new(
            [$"Trend:{trend.Direction}", $"EmaAlignment:{trend.EmaAlignment}", $"PriceVsEma:{trend.PriceVsEma}"],
            [$"Rsi:{momentum.Rsi.Zone}/{momentum.Rsi.Momentum}", $"Macd:{momentum.Macd.Momentum}", $"Stochastic:{momentum.Stochastic.Zone}"],
            [$"Structure:{structure.Structure}"],
            [.. levels.Select(level => $"{level.Type}:{level.Center:F3}/{level.Strength}")],
            [.. patterns.Select(pattern => $"{pattern.Pattern}:{pattern.Direction}")],
            volatility.Evidence,
            conflicts);

    private static bool Opposed(AnalyticalDirection left, AnalyticalDirection right) =>
        left == AnalyticalDirection.Bullish && right == AnalyticalDirection.Bearish
        || left == AnalyticalDirection.Bearish && right == AnalyticalDirection.Bullish;

    private static int CountCalculated(IndicatorSet indicators) =>
        indicators.Sma.Count(value => value.Value.HasValue)
        + indicators.Ema.Count(value => value.Value.HasValue)
        + (indicators.Rsi.Value.HasValue ? 1 : 0)
        + (indicators.Macd.Histogram.HasValue ? 1 : 0)
        + (indicators.Atr.Value.HasValue ? 1 : 0)
        + (indicators.Adx.Adx.HasValue ? 1 : 0)
        + (indicators.BollingerBands.Middle.HasValue ? 1 : 0)
        + (indicators.Stochastic.PercentD.HasValue ? 1 : 0);

    private static int CountInsufficient(IndicatorSet indicators) =>
        indicators.Sma.Count(value => value.Readiness == AnalysisReadiness.InsufficientData)
        + indicators.Ema.Count(value => value.Readiness == AnalysisReadiness.InsufficientData)
        + new[]
        {
            indicators.Rsi.Readiness,
            indicators.Macd.Readiness,
            indicators.Atr.Readiness,
            indicators.Adx.Readiness,
            indicators.BollingerBands.Readiness,
            indicators.Stochastic.Readiness
        }.Count(value => value == AnalysisReadiness.InsufficientData);

    private static int TimeframeRank(MarketTimeframe timeframe) => timeframe switch
    {
        MarketTimeframe.D1 => 7,
        MarketTimeframe.H4 => 6,
        MarketTimeframe.H1 => 5,
        MarketTimeframe.M30 => 4,
        MarketTimeframe.M15 => 3,
        MarketTimeframe.M5 => 2,
        MarketTimeframe.M1 => 1,
        _ => 0
    };

    private static TechnicalAnalysisException Disabled() =>
        new(
            TechnicalAnalysisErrorCodes.Disabled,
            "Technical analysis is disabled by configuration.");
}
