using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Configuration.Validation;

namespace XauAi.UnitTests.Configuration;

public sealed class EvidenceOptionsValidatorTests
{
    [Fact]
    public void Default_options_are_valid()
    {
        var result = new EvidenceOptionsValidator().Validate(null, new EvidenceOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Invalid_bounds_and_inverted_lookback_are_rejected()
    {
        var options = new EvidenceOptions
        {
            MaximumPageSize = 0,
            MaximumQueryRangeDays = 3651,
            DefaultPackLookbackDays = 31,
            MaximumPackLookbackDays = 30,
            MaximumPackItemsPerType = 0,
            IngestionBatchSize = 5001,
            ConflictWindowHours = 0,
            MaximumConflicts = 5001
        };

        var result = new EvidenceOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("MaximumPageSize", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("MaximumQueryRangeDays", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("DefaultPackLookbackDays cannot exceed", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("MaximumPackItemsPerType", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("IngestionBatchSize", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("ConflictWindowHours", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("MaximumConflicts", StringComparison.Ordinal));
    }
}
