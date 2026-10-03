using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class ApplicationOptionsValidator : IValidateOptions<ApplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            failures.Add("Application:Name is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Version))
        {
            failures.Add("Application:Version is required.");
        }

        return ConfigurationValidation.Result(failures);
    }
}
