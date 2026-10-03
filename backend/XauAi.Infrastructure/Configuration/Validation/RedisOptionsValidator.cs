using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class RedisOptionsValidator : IValidateOptions<RedisOptions>
{
    public ValidateOptionsResult Validate(string? name, RedisOptions options)
    {
        var failures = new List<string>();

        if (options.DefaultExpirationSeconds is < 1 or > 86_400)
        {
            failures.Add("Redis:DefaultExpirationSeconds must be between 1 and 86400.");
        }

        if (string.IsNullOrWhiteSpace(options.InstanceName))
        {
            failures.Add("Redis:InstanceName is required.");
        }

        if (options.Enabled && !ConfigurationValidation.IsConfigured(options.ConnectionString))
        {
            failures.Add("Redis:ConnectionString is required when Redis:Enabled is true.");
        }

        return ConfigurationValidation.Result(failures);
    }
}
