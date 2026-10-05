using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class AnalystOptionsValidator : IValidateOptions<AnalystOptions>
{
    public ValidateOptionsResult Validate(string? name, AnalystOptions options)
    {
        var failures = new List<string>();

        if (options.TimeoutSeconds is < 1 or > 300)
        {
            failures.Add("Analysts:TimeoutSeconds must be between 1 and 300.");
        }

        if (options.MaxRetries is < 0 or > 10)
        {
            failures.Add("Analysts:MaxRetries must be between 0 and 10.");
        }

        if (options.RateLimitPerMinute < 0)
        {
            failures.Add("Analysts:RateLimitPerMinute cannot be negative.");
        }

        if (options.InitialLookbackDays is < 1 or > 3650)
        {
            failures.Add("Analysts:InitialLookbackDays must be between 1 and 3650.");
        }

        if (options.CollectionOverlapMinutes is < 0 or > 1440)
        {
            failures.Add("Analysts:CollectionOverlapMinutes must be between 0 and 1440.");
        }

        if (options.SyncIntervalMinutes is < 1 or > 10080)
        {
            failures.Add("Analysts:SyncIntervalMinutes must be between 1 and 10080.");
        }

        if (options.ProviderPageSize is < 1 or > 500)
        {
            failures.Add("Analysts:ProviderPageSize must be between 1 and 500.");
        }

        if (options.MaximumPagesPerSync is < 1 or > 100)
        {
            failures.Add("Analysts:MaximumPagesPerSync must be between 1 and 100.");
        }

        if (options.MaximumPageSize is < 1 or > 500)
        {
            failures.Add("Analysts:MaximumPageSize must be between 1 and 500.");
        }

        if (options.MaximumCollectionRangeDays is < 1 or > 36500)
        {
            failures.Add("Analysts:MaximumCollectionRangeDays must be between 1 and 36500.");
        }

        if (options.RetryBaseDelaySeconds is < 1 or > 300)
        {
            failures.Add("Analysts:RetryBaseDelaySeconds must be between 1 and 300.");
        }

        if (!ConfigurationValidation.IsConfigured(options.ProviderKey))
        {
            failures.Add("Analysts:ProviderKey is required.");
        }

        if (!ConfigurationValidation.IsConfigured(options.Symbol))
        {
            failures.Add("Analysts:Symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(options.RelevanceKeywords))
        {
            failures.Add("Analysts:RelevanceKeywords must contain at least one term.");
        }

        if (!Enum.IsDefined(options.SourceType))
        {
            failures.Add("Analysts:SourceType is invalid.");
        }

        if (options.Enabled)
        {
            if (!ConfigurationValidation.IsConfigured(options.Provider) || options.Provider == "None")
            {
                failures.Add("Analysts:Provider is required when Analysts:Enabled is true.");
            }

            else if (!options.Provider.Equals("RssAtom", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Analysts:Provider must be RssAtom for the configured Phase 9 adapter.");
            }

            if (options.SourceType != AnalystSourceType.RssFeed)
            {
                failures.Add("Analysts:SourceType must be RssFeed for the RssAtom adapter.");
            }

            if (options.SourceType != AnalystSourceType.Manual
                && !ConfigurationValidation.IsAbsoluteHttpUrl(options.BaseUrl))
            {
                failures.Add("Analysts:BaseUrl must be an absolute HTTP or HTTPS URL for non-manual sources.");
            }

            if (options.RequiresApiKey && !ConfigurationValidation.IsConfigured(options.ApiKey))
            {
                failures.Add("Analysts:ApiKey is required for an enabled provider that requires a key.");
            }
        }

        return ConfigurationValidation.Result(failures);
    }
}
