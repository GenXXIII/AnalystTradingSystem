using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class EvidenceOptionsValidator : IValidateOptions<EvidenceOptions>
{
    public ValidateOptionsResult Validate(string? name, EvidenceOptions options)
    {
        var failures = new List<string>();
        InRange(options.MaximumPageSize, 1, 1000, "Evidence:MaximumPageSize", failures);
        InRange(options.MaximumQueryRangeDays, 1, 3650, "Evidence:MaximumQueryRangeDays", failures);
        InRange(options.DefaultPackLookbackDays, 1, 3650, "Evidence:DefaultPackLookbackDays", failures);
        InRange(options.MaximumPackLookbackDays, 1, 3650, "Evidence:MaximumPackLookbackDays", failures);
        InRange(options.MaximumPackItemsPerType, 1, 1000, "Evidence:MaximumPackItemsPerType", failures);
        InRange(options.IngestionBatchSize, 1, 5000, "Evidence:IngestionBatchSize", failures);
        InRange(options.ConflictWindowHours, 1, 720, "Evidence:ConflictWindowHours", failures);
        InRange(options.MaximumConflicts, 1, 5000, "Evidence:MaximumConflicts", failures);
        if (options.DefaultPackLookbackDays > options.MaximumPackLookbackDays)
        {
            failures.Add("Evidence:DefaultPackLookbackDays cannot exceed Evidence:MaximumPackLookbackDays.");
        }

        return ConfigurationValidation.Result(failures);
    }

    private static void InRange(int value, int minimum, int maximum, string name, List<string> failures)
    {
        if (value < minimum || value > maximum)
        {
            failures.Add($"{name} must be between {minimum} and {maximum}.");
        }
    }
}
