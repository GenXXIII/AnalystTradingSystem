using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class TargetAnalystOptionsValidator : IValidateOptions<TargetAnalystOptions>
{
    public ValidateOptionsResult Validate(string? name, TargetAnalystOptions options)
    {
        var failures = new List<string>();
        if (!string.Equals(options.Symbol, "XAUUSD", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("TargetAnalyst:Symbol must be XAUUSD.");
        }

        var timeframes = options.Timeframes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (timeframes.Length == 0 || timeframes.Any(value => !MarketTimeframes.TryParse(value, out _)))
        {
            failures.Add("TargetAnalyst:Timeframes must contain valid market timeframes.");
        }

        if (options.EvidenceLookbackHours is < 1 or > 87_600)
        {
            failures.Add("TargetAnalyst:EvidenceLookbackHours must be between 1 and 87600.");
        }

        if (options.MaximumEvidenceItemsPerWorkspace is < 1 or > 500)
        {
            failures.Add("TargetAnalyst:MaximumEvidenceItemsPerWorkspace must be between 1 and 500.");
        }

        if (options.MaximumCompressedCharacters is < 2_000 or > 1_000_000)
        {
            failures.Add("TargetAnalyst:MaximumCompressedCharacters must be between 2000 and 1000000.");
        }

        if (options.MaximumTotalTokens is < 8_000 or > 1_000_000)
        {
            failures.Add("TargetAnalyst:MaximumTotalTokens must be between 8000 and 1000000.");
        }

        if (options.MinimumMarketTimeframes is < 1 or > 7)
        {
            failures.Add("TargetAnalyst:MinimumMarketTimeframes must be between 1 and 7.");
        }

        if (options.StaleAfterIntervals is < 1 or > 100)
        {
            failures.Add("TargetAnalyst:StaleAfterIntervals must be between 1 and 100.");
        }

        if (options.MinimumConfidence is < 0m or > 1m)
        {
            failures.Add("TargetAnalyst:MinimumConfidence must be between 0 and 1.");
        }

        if (options.MinimumTargetDistanceAtr is <= 0m or > 10m)
        {
            failures.Add("TargetAnalyst:MinimumTargetDistanceAtr must be greater than 0 and at most 10.");
        }

        if (options.DefaultValidityMinutes < 1
            || options.MaximumValidityMinutes < options.DefaultValidityMinutes
            || options.MaximumValidityMinutes > 10_080)
        {
            failures.Add("TargetAnalyst validity-minute limits are invalid.");
        }

        if (options.CacheMinutes is < 1 or > 1_440)
        {
            failures.Add("TargetAnalyst:CacheMinutes must be between 1 and 1440.");
        }

        if (options.MonitorIntervalSeconds is < 5 or > 3_600)
        {
            failures.Add("TargetAnalyst:MonitorIntervalSeconds must be between 5 and 3600.");
        }

        if (options.MaximumPageSize is < 1 or > 1_000)
        {
            failures.Add("TargetAnalyst:MaximumPageSize must be between 1 and 1000.");
        }

        if (!ConfigurationValidation.IsConfigured(options.ConfigurationVersion))
        {
            failures.Add("TargetAnalyst:ConfigurationVersion is required.");
        }

        return ConfigurationValidation.Result(failures);
    }
}

internal sealed class TargetAiWorkspacesOptionsValidator : IValidateOptions<TargetAiWorkspacesOptions>
{
    public ValidateOptionsResult Validate(string? name, TargetAiWorkspacesOptions options)
    {
        var failures = new List<string>();
        Validate("Structure", options.Structure, failures);
        Validate("Liquidity", options.Liquidity, failures);
        Validate("Candle", options.Candle, failures);
        Validate("Flow", options.Flow, failures);
        Validate("Ktr", options.Ktr, failures);
        Validate("News", options.News, failures);
        Validate("Risk", options.Risk, failures);
        Validate("Master", options.Master, failures);
        return ConfigurationValidation.Result(failures);
    }

    private static void Validate(
        string name,
        TargetAiWorkspaceOptions options,
        ICollection<string> failures)
    {
        var prefix = $"TargetAiWorkspaces:{name}";
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

        if (!ConfigurationValidation.IsConfigured(options.PromptVersion))
        {
            failures.Add($"{prefix}:PromptVersion is required.");
        }

        if (!ConfigurationValidation.IsConfigured(options.ConfigurationVersion))
        {
            failures.Add($"{prefix}:ConfigurationVersion is required.");
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
        var models = options.FallbackModels.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (models.Any(model => string.Equals(model, options.Model, StringComparison.OrdinalIgnoreCase))
            || models.Distinct(StringComparer.OrdinalIgnoreCase).Count() != models.Length)
        {
            failures.Add($"{prefix}:FallbackModels must be unique and must not repeat Model.");
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
