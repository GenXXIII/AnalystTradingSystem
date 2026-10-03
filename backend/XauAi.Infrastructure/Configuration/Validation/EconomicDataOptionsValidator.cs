using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class EconomicDataOptionsValidator : IValidateOptions<EconomicDataOptions>
{
    public ValidateOptionsResult Validate(string? name, EconomicDataOptions options)
    {
        var failures = new List<string>();

        if (options.TimeoutSeconds is < 1 or > 300)
        {
            failures.Add("EconomicData:TimeoutSeconds must be between 1 and 300.");
        }

        if (options.MaxRetries is < 0 or > 10)
        {
            failures.Add("EconomicData:MaxRetries must be between 0 and 10.");
        }

        if (options.RateLimitPerMinute < 0)
        {
            failures.Add("EconomicData:RateLimitPerMinute cannot be negative.");
        }

        if (options.InitialHistoryYears is < 1 or > 100)
        {
            failures.Add("EconomicData:InitialHistoryYears must be between 1 and 100.");
        }

        if (options.RevisionLookbackDays is < 0 or > 3650)
        {
            failures.Add("EconomicData:RevisionLookbackDays must be between 0 and 3650.");
        }

        if (options.SyncIntervalMinutes is < 15 or > 10080)
        {
            failures.Add("EconomicData:SyncIntervalMinutes must be between 15 and 10080.");
        }

        if (options.ProviderPageSize is < 1 or > 100_000)
        {
            failures.Add("EconomicData:ProviderPageSize must be between 1 and 100000.");
        }

        if (options.MaximumPagesPerSeries is < 1 or > 1000)
        {
            failures.Add("EconomicData:MaximumPagesPerSeries must be between 1 and 1000.");
        }

        if (options.MaximumPageSize is < 1 or > 1000)
        {
            failures.Add("EconomicData:MaximumPageSize must be between 1 and 1000.");
        }

        if (options.MaximumQueryRangeYears is < 1 or > 200)
        {
            failures.Add("EconomicData:MaximumQueryRangeYears must be between 1 and 200.");
        }

        if (options.RetryBaseDelaySeconds is < 1 or > 60)
        {
            failures.Add("EconomicData:RetryBaseDelaySeconds must be between 1 and 60.");
        }

        if (options.Enabled)
        {
            if (!ConfigurationValidation.IsConfigured(options.Provider) || options.Provider == "None")
            {
                failures.Add("EconomicData:Provider is required when EconomicData:Enabled is true.");
            }
            else if (!string.Equals(options.Provider, "FRED", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("EconomicData:Provider must be FRED for the Phase 8 adapter.");
            }

            if (!ConfigurationValidation.IsConfigured(options.ProviderKey))
            {
                failures.Add("EconomicData:ProviderKey is required when EconomicData:Enabled is true.");
            }

            if (!ConfigurationValidation.IsAbsoluteHttpUrl(options.BaseUrl))
            {
                failures.Add("EconomicData:BaseUrl must be an absolute HTTP or HTTPS URL when enabled.");
            }

            if (!options.RequiresApiKey)
            {
                failures.Add("EconomicData:RequiresApiKey must be true for FRED.");
            }

            if (!ConfigurationValidation.IsConfigured(options.ApiKey))
            {
                failures.Add("EconomicData:ApiKey is required when FRED is enabled.");
            }

            var definitions = options.TrackedSeries.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (definitions.Length == 0
                || definitions.Any(definition =>
                {
                    var parts = definition.Split(':', 2, StringSplitOptions.TrimEntries);
                    return parts.Length != 2
                        || string.IsNullOrWhiteSpace(parts[0])
                        || string.IsNullOrWhiteSpace(parts[1])
                        || parts[0].Length > 120
                        || parts[1].Length > 100
                        || parts[0].Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_');
                })
                || definitions.Select(definition => definition.Split(':', 2)[0])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() != definitions.Length)
            {
                failures.Add("EconomicData:TrackedSeries must contain unique SERIES_ID:Category entries.");
            }
        }

        return ConfigurationValidation.Result(failures);
    }
}
