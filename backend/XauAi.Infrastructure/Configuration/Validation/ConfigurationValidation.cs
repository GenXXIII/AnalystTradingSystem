using Microsoft.Extensions.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal static class ConfigurationValidation
{
    public static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("USER_PROVIDED", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("DEFINED_LATER", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase);

    public static bool IsAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static ValidateOptionsResult Result(List<string> failures) =>
        failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
}
