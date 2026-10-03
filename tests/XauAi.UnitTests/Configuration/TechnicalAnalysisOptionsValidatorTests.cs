using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Configuration.Validation;

namespace XauAi.UnitTests.Configuration;

public sealed class TechnicalAnalysisOptionsValidatorTests
{
    [Fact]
    public void Default_options_are_valid()
    {
        var result = new TechnicalAnalysisOptionsValidator().Validate(null, new TechnicalAnalysisOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Invalid_periods_threshold_order_and_timeframes_are_rejected()
    {
        var options = new TechnicalAnalysisOptions
        {
            Timeframes = "H2",
            EmaPeriods = "9,nope,200",
            MacdFastPeriod = 30,
            MacdSlowPeriod = 20,
            VeryLowVolatilityRatio = 2m,
            LowVolatilityRatio = 1m,
            HighVolatilityRatio = 0.8m,
            VeryHighVolatilityRatio = 0.5m
        };

        var result = new TechnicalAnalysisOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Timeframes", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("EmaPeriods", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("MacdFastPeriod", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("volatility ratios", StringComparison.Ordinal));
    }
}
