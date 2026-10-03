using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class TwelveDataOptionsValidator : IValidateOptions<TwelveDataOptions>
{
    public ValidateOptionsResult Validate(string? name, TwelveDataOptions options)
    {
        var failures = new List<string>();
        if (options.Enabled && !ConfigurationValidation.IsConfigured(options.ApiKey))
        {
            failures.Add("TwelveData:ApiKey is required when Twelve Data is enabled.");
        }

        Required(options.ApplicationSymbol, "TwelveData:ApplicationSymbol", failures);
        Required(options.Symbol, "TwelveData:Symbol", failures);
        if (!string.Equals(options.ProviderKey, "twelvedata", StringComparison.Ordinal))
        {
            failures.Add("TwelveData:ProviderKey must be twelvedata.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("TwelveData:BaseUrl must be an absolute HTTPS URL.");
        }

        Range(options.RequestTimeoutSeconds, 1, 300, "TwelveData:RequestTimeoutSeconds", failures);
        Range(options.MinimumRequestIntervalSeconds, 8, 3600, "TwelveData:MinimumRequestIntervalSeconds", failures);
        Range(options.ReferenceSyncIntervalSeconds, 60, 86400, "TwelveData:ReferenceSyncIntervalSeconds", failures);
        Range(options.InitialHistoryDays, 1, 365, "TwelveData:InitialHistoryDays", failures);
        Range(options.IncrementalLookbackMinutes, 5, 10080, "TwelveData:IncrementalLookbackMinutes", failures);
        Range(options.MaximumPointsPerRequest, 1, 5000, "TwelveData:MaximumPointsPerRequest", failures);
        if (options.MaximumCloseDeviationBps is < 0 or > 10000)
        {
            failures.Add("TwelveData:MaximumCloseDeviationBps must be between 0 and 10000.");
        }

        return ConfigurationValidation.Result(failures);
    }

    private static void Required(string value, string key, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required.");
        }
    }

    private static void Range(int value, int minimum, int maximum, string key, List<string> failures)
    {
        if (value < minimum || value > maximum)
        {
            failures.Add($"{key} must be between {minimum} and {maximum}.");
        }
    }
}
