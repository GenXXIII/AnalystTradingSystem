using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class AiInterpretationOptionsValidator : IValidateOptions<AiInterpretationOptions>
{
    public ValidateOptionsResult Validate(string? name, AiInterpretationOptions options)
    {
        var failures = new List<string>();
        if (!ConfigurationValidation.IsConfigured(options.PromptVersion))
        {
            failures.Add("AiInterpretation:PromptVersion is required.");
        }

        if (options.DefaultLookbackHours < 1
            || options.MaximumLookbackHours < options.DefaultLookbackHours
            || options.MaximumLookbackHours > 87_600)
        {
            failures.Add("AiInterpretation lookback limits are invalid.");
        }

        if (options.MaximumEvidenceItems is < 1 or > 500)
        {
            failures.Add("AiInterpretation:MaximumEvidenceItems must be between 1 and 500.");
        }

        if (options.MaximumCompressedCharacters is < 2_000 or > 1_000_000)
        {
            failures.Add("AiInterpretation:MaximumCompressedCharacters must be between 2000 and 1000000.");
        }

        if (options.CurrentContextCacheMinutes is < 1 or > 60)
        {
            failures.Add("AiInterpretation:CurrentContextCacheMinutes must be between 1 and 60.");
        }

        if (options.MaximumPageSize is < 1 or > 1_000)
        {
            failures.Add("AiInterpretation:MaximumPageSize must be between 1 and 1000.");
        }

        return ConfigurationValidation.Result(failures);
    }
}

internal sealed class AiSpecialistsOptionsValidator : IValidateOptions<AiSpecialistsOptions>
{
    public ValidateOptionsResult Validate(string? name, AiSpecialistsOptions options)
    {
        var failures = new List<string>();
        Validate("News", options.News, failures);
        Validate("Candle", options.Candle, failures);
        Validate("Structure", options.Structure, failures);
        Validate("Liquidity", options.Liquidity, failures);
        Validate("Flow", options.Flow, failures);
        Validate("Ktr", options.Ktr, failures);
        Validate("Risk", options.Risk, failures);
        Validate("Master", options.Master, failures);
        return ConfigurationValidation.Result(failures);
    }

    private static void Validate(string name, AiSpecialistOptions options, ICollection<string> failures)
    {
        var prefix = $"AiSpecialists:{name}";
        if (options.Temperature is < 0 or > 2)
        {
            failures.Add($"{prefix}:Temperature must be between 0 and 2.");
        }

        if (options.TimeoutSeconds is < 1 or > 600)
        {
            failures.Add($"{prefix}:TimeoutSeconds must be between 1 and 600.");
        }

        if (options.MaxOutputTokens is < 1 or > 100_000)
        {
            failures.Add($"{prefix}:MaxOutputTokens must be between 1 and 100000.");
        }

        if (options.MaxRetries is < 0 or > 10)
        {
            failures.Add($"{prefix}:MaxRetries must be between 0 and 10.");
        }

        if (options.RequestsPerMinute is < 0 or > 10_000)
        {
            failures.Add($"{prefix}:RequestsPerMinute must be between 0 and 10000.");
        }

        if (!options.Enabled)
        {
            return;
        }

        if (!ConfigurationValidation.IsConfigured(options.Provider) || options.Provider == "None")
        {
            failures.Add($"{prefix}:Provider is required when enabled.");
        }

        if (!ConfigurationValidation.IsConfigured(options.Adapter))
        {
            failures.Add($"{prefix}:Adapter is required when enabled.");
        }

        if (!ConfigurationValidation.IsConfigured(options.Model))
        {
            failures.Add($"{prefix}:Model is required when enabled.");
        }

        if (!ConfigurationValidation.IsAbsoluteHttpUrl(options.BaseUrl))
        {
            failures.Add($"{prefix}:BaseUrl must be an absolute HTTP or HTTPS URL when enabled.");
        }

        if (options.RequiresApiKey && !ConfigurationValidation.IsConfigured(options.ApiKey))
        {
            failures.Add($"{prefix}:ApiKey is required for the configured provider.");
        }
    }
}
