using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        var failures = new List<string>();

        if (options.Temperature is < 0 or > 2)
        {
            failures.Add("AI:Temperature must be between 0 and 2.");
        }

        if (options.TimeoutSeconds is < 1 or > 600)
        {
            failures.Add("AI:TimeoutSeconds must be between 1 and 600.");
        }

        if (options.MaxRetries is < 0 or > 10)
        {
            failures.Add("AI:MaxRetries must be between 0 and 10.");
        }

        if (options.MaxOutputTokens is < 1 or > 100_000)
        {
            failures.Add("AI:MaxOutputTokens must be between 1 and 100000.");
        }

        if (ConfigurationValidation.IsConfigured(options.BaseUrl)
            && !ConfigurationValidation.IsAbsoluteHttpUrl(options.BaseUrl))
        {
            failures.Add("AI:BaseUrl must be an absolute HTTP or HTTPS URL when provided.");
        }

        if (options.Enabled)
        {
            if (!ConfigurationValidation.IsConfigured(options.Provider) || options.Provider == "None")
            {
                failures.Add("AI:Provider is required when AI:Enabled is true.");
            }

            if (!ConfigurationValidation.IsConfigured(options.Model))
            {
                failures.Add("AI:Model is required when AI:Enabled is true.");
            }

            if (options.RequiresApiKey && !ConfigurationValidation.IsConfigured(options.ApiKey))
            {
                failures.Add("AI:ApiKey is required when AI is enabled for a provider that requires a key.");
            }

            if (options.DailyBudgetUsd <= 0)
            {
                failures.Add("AI:DailyBudgetUsd must be greater than zero when AI:Enabled is true.");
            }

            if (options.DailyRequestLimit <= 0)
            {
                failures.Add("AI:DailyRequestLimit must be greater than zero when AI:Enabled is true.");
            }
        }

        return ConfigurationValidation.Result(failures);
    }
}
