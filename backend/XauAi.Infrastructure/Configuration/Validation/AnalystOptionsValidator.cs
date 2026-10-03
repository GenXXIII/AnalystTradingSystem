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
