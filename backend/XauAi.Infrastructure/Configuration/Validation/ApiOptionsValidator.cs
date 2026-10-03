using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class ApiOptionsValidator : IValidateOptions<ApiOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiOptions options)
    {
        var failures = new List<string>();

        if (options.Cors.AllowedOrigins.Length == 0)
        {
            failures.Add("Api:Cors:AllowedOrigins must contain at least one trusted origin.");
        }

        foreach (var origin in options.Cors.AllowedOrigins)
        {
            if (!ConfigurationValidation.IsAbsoluteHttpUrl(origin)
                || origin.EndsWith('/'))
            {
                failures.Add("Each Api:Cors:AllowedOrigins value must be an absolute HTTP or HTTPS origin without a trailing slash.");
                break;
            }
        }

        return ConfigurationValidation.Result(failures);
    }
}
