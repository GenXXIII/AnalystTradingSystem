using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class FullAnalystOptionsValidator : IValidateOptions<FullAnalystOptions>
{
    public ValidateOptionsResult Validate(string? name, FullAnalystOptions options)
    {
        var failures = new List<string>();
        if (!string.Equals(options.Symbol, "XAUUSD", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("FullAnalyst:Symbol must be XAUUSD.");
        }

        var timeframes = options.Timeframes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (timeframes.Length == 0 || timeframes.Any(value => !MarketTimeframes.TryParse(value, out _)))
        {
            failures.Add("FullAnalyst:Timeframes must contain valid market timeframes.");
        }

        if (options.EvidenceLookbackHours is < 1 or > 87_600
            || options.MaximumEvidenceItemsPerWorkspace is < 1 or > 500
            || options.MaximumCompressedCharacters is < 2_000 or > 1_000_000
            || options.MinimumMarketTimeframes is < 1 or > 7
            || options.StaleAfterIntervals is < 1 or > 100)
        {
            failures.Add("FullAnalyst evidence and market-data limits are invalid.");
        }

        if (options.MinimumConfidence is < 0m or > 1m)
        {
            failures.Add("FullAnalyst:MinimumConfidence must be between 0 and 1.");
        }

        if (options.DefaultValidityMinutes < 1
            || options.MaximumValidityMinutes < options.DefaultValidityMinutes
            || options.MaximumValidityMinutes > 10_080)
        {
            failures.Add("FullAnalyst validity-minute limits are invalid.");
        }

        if (options.CacheMinutes is < 1 or > 1_440
            || options.MonitorIntervalSeconds is < 5 or > 3_600
            || options.MaximumPageSize is < 1 or > 1_000)
        {
            failures.Add("FullAnalyst cache, monitoring, or paging limits are invalid.");
        }

        if (!ConfigurationValidation.IsConfigured(options.ConfigurationVersion))
        {
            failures.Add("FullAnalyst:ConfigurationVersion is required.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

internal sealed class FullAiWorkspacesOptionsValidator : IValidateOptions<FullAiWorkspacesOptions>
{
    public ValidateOptionsResult Validate(string? name, FullAiWorkspacesOptions options)
    {
        var failures = new List<string>();
        foreach (var pair in Workspaces(options))
        {
            var workspace = pair.Value;
            if (workspace.Temperature is < 0 or > 2
                || workspace.TimeoutSeconds is < 1 or > 600
                || workspace.MaxOutputTokens is < 64 or > 100_000
                || workspace.MaxRetries is < 0 or > 10
                || workspace.RequestsPerMinute is < 0 or > 10_000)
            {
                failures.Add($"FullAiWorkspaces:{pair.Key} execution limits are invalid.");
            }

            if (!ConfigurationValidation.IsConfigured(workspace.Adapter)
                || !ConfigurationValidation.IsConfigured(workspace.PromptVersion)
                || !ConfigurationValidation.IsConfigured(workspace.ConfigurationVersion))
            {
                failures.Add($"FullAiWorkspaces:{pair.Key} adapter and version values are required.");
            }

            if (workspace.Enabled
                && (!ConfigurationValidation.IsConfigured(workspace.Provider)
                    || string.Equals(workspace.Provider, "None", StringComparison.OrdinalIgnoreCase)
                    || !ConfigurationValidation.IsConfigured(workspace.Model)
                    || !Uri.TryCreate(workspace.BaseUrl, UriKind.Absolute, out _)
                    || workspace.RequiresApiKey && !ConfigurationValidation.IsConfigured(workspace.ApiKey)))
            {
                failures.Add($"FullAiWorkspaces:{pair.Key} enabled provider configuration is incomplete.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static IReadOnlyDictionary<string, FullAiWorkspaceOptions> Workspaces(FullAiWorkspacesOptions options) =>
        new Dictionary<string, FullAiWorkspaceOptions>(StringComparer.Ordinal)
        {
            ["Structure"] = options.Structure,
            ["Liquidity"] = options.Liquidity,
            ["Candle"] = options.Candle,
            ["Flow"] = options.Flow,
            ["Ktr"] = options.Ktr,
            ["News"] = options.News,
            ["Risk"] = options.Risk,
            ["Master"] = options.Master
        };
}
