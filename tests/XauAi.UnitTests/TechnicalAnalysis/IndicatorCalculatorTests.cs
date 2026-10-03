using XauAi.Application.TechnicalAnalysis;

namespace XauAi.UnitTests.TechnicalAnalysis;

public sealed class IndicatorCalculatorTests
{
    private readonly IndicatorCalculator calculator = new();

    [Fact]
    public void Sma_and_ema_match_known_values()
    {
        var settings = Settings(sma: [3], ema: [3]);
        var candles = TechnicalAnalysisTestData.FromCloses([1m, 2m, 3m, 4m]);

        var result = calculator.Calculate(candles, settings);

        Assert.Equal(3m, result.Sma.Single().Value);
        Assert.Equal(2m, result.Sma.Single().PreviousValue);
        Assert.Equal(3m, result.Ema.Single().Value);
        Assert.Equal(2m, result.Ema.Single().PreviousValue);
        Assert.Equal(AnalysisReadiness.WarmingUp, result.Ema.Single().Readiness);
    }

    [Fact]
    public void Rsi_uses_wilder_smoothing_and_handles_constant_prices()
    {
        var rising = calculator.Calculate(
            TechnicalAnalysisTestData.FromCloses(Enumerable.Range(1, 20).Select(value => (decimal)value)),
            Settings());
        var constant = calculator.Calculate(
            TechnicalAnalysisTestData.FromCloses(Enumerable.Repeat(100m, 20)),
            Settings());

        Assert.Equal(100m, rising.Rsi.Value);
        Assert.Equal(IndicatorZone.Overbought, rising.Rsi.Zone);
        Assert.Equal(50m, constant.Rsi.Value);
        Assert.Equal(IndicatorZone.Neutral, constant.Rsi.Zone);
    }

    [Fact]
    public void Macd_atr_and_adx_return_direction_and_strength_without_a_trade_signal()
    {
        var candles = TechnicalAnalysisTestData.FromCloses(
            Enumerable.Range(1, 120).Select(value => 2000m + (value * value * 0.01m)));

        var result = calculator.Calculate(candles, Settings());

        Assert.True(result.Macd.Macd > 0m);
        Assert.True(result.Macd.Histogram >= 0m);
        Assert.Equal(AnalyticalDirection.Bullish, result.Macd.Momentum);
        Assert.True(result.Atr.Value > 0m);
        Assert.True(result.Adx.Adx > 25m);
        Assert.Equal(AnalyticalDirection.Bullish, result.Adx.DirectionalBias);
    }

    [Fact]
    public void Bollinger_bands_and_stochastic_match_known_ranges()
    {
        var settings = Settings(sma: [3], ema: [3], bollinger: 5, stochasticK: 3, stochasticD: 2);
        var candles = TechnicalAnalysisTestData.FromCloses([1m, 2m, 3m, 4m, 5m]);

        var result = calculator.Calculate(candles, settings);

        Assert.Equal(3m, result.BollingerBands.Middle);
        Assert.Equal(5.828427m, result.BollingerBands.Upper!.Value, 6);
        Assert.Equal(0.171573m, result.BollingerBands.Lower!.Value, 6);
        Assert.InRange(result.BollingerBands.PercentB!.Value, 0.85m, 0.86m);
        Assert.InRange(result.Stochastic.PercentK!.Value, 80m, 100m);
        Assert.NotNull(result.Stochastic.PercentD);
    }

    [Fact]
    public void Every_indicator_reports_insufficient_data_instead_of_fabricating_values()
    {
        var result = calculator.Calculate(
            TechnicalAnalysisTestData.FromCloses([100m]),
            Settings());

        Assert.All(result.Sma, value => Assert.Equal(AnalysisReadiness.InsufficientData, value.Readiness));
        Assert.All(result.Ema, value => Assert.Equal(AnalysisReadiness.InsufficientData, value.Readiness));
        Assert.Equal(AnalysisReadiness.InsufficientData, result.Rsi.Readiness);
        Assert.Equal(AnalysisReadiness.InsufficientData, result.Macd.Readiness);
        Assert.Equal(AnalysisReadiness.InsufficientData, result.Atr.Readiness);
        Assert.Equal(AnalysisReadiness.InsufficientData, result.Adx.Readiness);
        Assert.Equal(AnalysisReadiness.InsufficientData, result.BollingerBands.Readiness);
        Assert.Equal(AnalysisReadiness.InsufficientData, result.Stochastic.Readiness);
    }

    [Fact]
    public void Ten_thousand_candles_calculate_with_bounded_time_and_memory()
    {
        var candles = TechnicalAnalysisTestData.FromCloses(
            Enumerable.Range(0, 10000).Select(index => 2000m + (index % 200) * 0.1m));
        var before = GC.GetTotalMemory(forceFullCollection: true);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = calculator.Calculate(candles, Settings());

        stopwatch.Stop();
        var delta = GC.GetTotalMemory(forceFullCollection: true) - before;
        Assert.NotNull(result.Ema.Single(value => value.Period == 200).Value);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Calculation took {stopwatch.Elapsed}.");
        Assert.True(delta < 64 * 1024 * 1024, $"Managed-memory delta was {delta} bytes.");
    }

    private static TechnicalAnalysisSettings Settings(
        IReadOnlyList<int>? sma = null,
        IReadOnlyList<int>? ema = null,
        int bollinger = 20,
        int stochasticK = 14,
        int stochasticD = 3) => new()
        {
            SmaPeriods = sma ?? [20, 50, 200],
            EmaPeriods = ema ?? [9, 20, 50, 100, 200],
            BollingerPeriod = bollinger,
            StochasticKPeriod = stochasticK,
            StochasticDPeriod = stochasticD
        };
}
