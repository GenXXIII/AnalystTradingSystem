using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Configuration.Validation;

namespace XauAi.UnitTests.Configuration;

public sealed class LocalAnalystOptionsValidatorTests
{
    private readonly LocalAnalystOptionsValidator _validator = new();

    [Fact]
    public void Valid_phase12_configuration_is_accepted()
    {
        var result = _validator.Validate(null, new LocalAnalystOptions { Enabled = true });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validator_rejects_non_xauusd_invalid_weights_and_missing_validity()
    {
        var result = _validator.Validate(null, new LocalAnalystOptions
        {
            Symbol = "EURUSD",
            EntryScoreThreshold = 7m,
            ValidityCandles = "M1:0,H2:5",
            StructureWeight = 0m
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, value => value.Contains("LocalAnalyst:Symbol", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, value => value.Contains("weights", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures!, value => value.Contains("EntryScoreThreshold", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, value => value.Contains("ValidityCandles", StringComparison.Ordinal));
    }
}

