using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Application.News;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class NewsOptionsValidator : IValidateOptions<NewsOptions>
{
    public ValidateOptionsResult Validate(string? name, NewsOptions options)
    {
        var failures = ValidateLimits(options.TimeoutSeconds, options.MaxRetries, options.RateLimitPerMinute);

        if (options.Enabled)
        {
            if (!ConfigurationValidation.IsConfigured(options.Provider) || options.Provider == "None")
            {
                failures.Add("News:Provider is required when News:Enabled is true.");
            }
            else if (!options.Provider.Equals("NewsData", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("News:Provider must be NewsData for the configured Phase 7 adapter.");
            }

            if (!ConfigurationValidation.IsAbsoluteHttpUrl(options.BaseUrl))
            {
                failures.Add("News:BaseUrl must be an absolute HTTP or HTTPS URL when News:Enabled is true.");
            }

            if (options.RequiresApiKey && !ConfigurationValidation.IsConfigured(options.ApiKey))
            {
                failures.Add("News:ApiKey is required for an enabled provider that requires a key.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.ProviderKey) || options.ProviderKey.Length > 100)
        {
            failures.Add("News:ProviderKey is required and cannot exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(options.Symbol) || options.Symbol.Length > 40)
        {
            failures.Add("News:Symbol is required and cannot exceed 40 characters.");
        }

        if (string.IsNullOrWhiteSpace(options.Language) || options.Language.Length > 16)
        {
            failures.Add("News:Language is required and cannot exceed 16 characters.");
        }

        if (string.IsNullOrWhiteSpace(options.ProviderQuery) || options.ProviderQuery.Length > 100)
        {
            failures.Add("News:ProviderQuery is required and cannot exceed the provider's 100-character request limit.");
        }

        Range(options.InitialLookbackHours, 1, 48, "News:InitialLookbackHours", failures);
        Range(options.CollectionOverlapMinutes, 0, 120, "News:CollectionOverlapMinutes", failures);
        Range(options.CollectionIntervalSeconds, 60, 86400, "News:CollectionIntervalSeconds", failures);
        Range(options.PageSize, 1, 50, "News:PageSize", failures);
        Range(options.MaximumPagesPerCollection, 1, 20, "News:MaximumPagesPerCollection", failures);
        Range(options.MaximumPageSize, 1, 200, "News:MaximumPageSize", failures);
        Range(options.MaximumCollectionRangeDays, 1, 366, "News:MaximumCollectionRangeDays", failures);
        Range(options.RetryBaseDelaySeconds, 1, 60, "News:RetryBaseDelaySeconds", failures);
        if (!Enum.TryParse<NewsRelevanceLevel>(options.MinimumRelevance, true, out _))
        {
            failures.Add("News:MinimumRelevance must be Irrelevant, Low, Medium, High, or VeryHigh.");
        }

        ValidateKeywords(options, failures);

        return ConfigurationValidation.Result(failures);
    }

    private static void ValidateKeywords(NewsOptions options, List<string> failures)
    {
        var keywordGroups = new Dictionary<string, string>
        {
            ["GoldKeywords"] = options.GoldKeywords,
            ["UsdKeywords"] = options.UsdKeywords,
            ["FedKeywords"] = options.FedKeywords,
            ["InflationKeywords"] = options.InflationKeywords,
            ["EmploymentKeywords"] = options.EmploymentKeywords,
            ["RatesKeywords"] = options.RatesKeywords,
            ["EconomyKeywords"] = options.EconomyKeywords,
            ["CentralBankKeywords"] = options.CentralBankKeywords,
            ["GeopoliticsKeywords"] = options.GeopoliticsKeywords,
            ["CommodityKeywords"] = options.CommodityKeywords
        };
        foreach (var group in keywordGroups.Where(group =>
                     group.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length == 0))
        {
            failures.Add($"News:{group.Key} must contain at least one keyword.");
        }
    }

    private static void Range(int value, int minimum, int maximum, string key, List<string> failures)
    {
        if (value < minimum || value > maximum)
        {
            failures.Add($"{key} must be between {minimum} and {maximum}.");
        }
    }

    private static List<string> ValidateLimits(int timeout, int retries, int rateLimit)
    {
        var failures = new List<string>();

        if (timeout is < 1 or > 300)
        {
            failures.Add("News:TimeoutSeconds must be between 1 and 300.");
        }

        if (retries is < 0 or > 10)
        {
            failures.Add("News:MaxRetries must be between 0 and 10.");
        }

        if (rateLimit < 0)
        {
            failures.Add("News:RateLimitPerMinute cannot be negative.");
        }

        return failures;
    }
}
