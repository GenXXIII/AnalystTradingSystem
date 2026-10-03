using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        if (options.CommandTimeoutSeconds is < 1 or > 600)
        {
            failures.Add("Database:CommandTimeoutSeconds must be between 1 and 600.");
        }

        if (options.Enabled && !ConfigurationValidation.IsConfigured(options.ConnectionString))
        {
            failures.Add("Database:ConnectionString is required when Database:Enabled is true.");
        }

        if (!options.Enabled && options.ApplyMigrationsOnStartup)
        {
            failures.Add("Database:ApplyMigrationsOnStartup requires Database:Enabled to be true.");
        }

        return ConfigurationValidation.Result(failures);
    }
}
