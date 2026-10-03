using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class AllTickOptionsValidator : IValidateOptions<AllTickOptions>
{
    public ValidateOptionsResult Validate(string? name, AllTickOptions options)
    {
        var failures = new List<string>();
        if (options.Enabled && !ConfigurationValidation.IsConfigured(options.Token))
        {
            failures.Add("AllTick:Token is required when AllTick is enabled.");
        }

        Required(options.ApplicationSymbol, "AllTick:ApplicationSymbol", failures);
        Required(options.Symbol, "AllTick:Symbol", failures);
        AbsoluteUri(options.HttpBaseUrl, Uri.UriSchemeHttps, "AllTick:HttpBaseUrl", failures);
        AbsoluteUri(options.WebSocketUrl, "wss", "AllTick:WebSocketUrl", failures);
        Range(options.RequestTimeoutSeconds, 1, 300, "AllTick:RequestTimeoutSeconds", failures);
        Range(options.ReconnectDelaySeconds, 10, 3600, "AllTick:ReconnectDelaySeconds", failures);
        Range(options.HeartbeatIntervalSeconds, 5, 10, "AllTick:HeartbeatIntervalSeconds", failures);
        Range(options.QuoteMaxAgeSeconds, 1, 300, "AllTick:QuoteMaxAgeSeconds", failures);
        Range(options.RealtimePersistIntervalSeconds, 1, 300, "AllTick:RealtimePersistIntervalSeconds", failures);
        Range(options.MaxBarsPerRequest, 1, 500, "AllTick:MaxBarsPerRequest", failures);
        Range(options.MinimumHttpRequestIntervalSeconds, 10, 3600, "AllTick:MinimumHttpRequestIntervalSeconds", failures);
        return ConfigurationValidation.Result(failures);
    }

    private static void Required(string value, string key, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required.");
        }
    }

    private static void AbsoluteUri(string value, string scheme, string key, List<string> failures)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{key} must be an absolute {scheme} URL.");
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
