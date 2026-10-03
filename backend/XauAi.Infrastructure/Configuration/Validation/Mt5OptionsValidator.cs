using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class Mt5OptionsValidator : IValidateOptions<Mt5Options>
{
    public ValidateOptionsResult Validate(string? name, Mt5Options options)
    {
        var failures = new List<string>();

        if (options.ConnectionTimeoutSeconds is < 1 or > 300)
        {
            failures.Add("MT5:ConnectionTimeoutSeconds must be between 1 and 300.");
        }

        if (options.ReconnectDelaySeconds is < 1 or > 300)
        {
            failures.Add("MT5:ReconnectDelaySeconds must be between 1 and 300.");
        }

        if (options.RequestTimeoutSeconds is < 1 or > 600)
        {
            failures.Add("MT5:RequestTimeoutSeconds must be between 1 and 600.");
        }

        if (options.MaxBarsPerRequest is < 1 or > 100000)
        {
            failures.Add("MT5:MaxBarsPerRequest must be between 1 and 100000.");
        }

        if (string.IsNullOrWhiteSpace(options.ApplicationSymbol))
        {
            failures.Add("MT5:ApplicationSymbol is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Symbol))
        {
            failures.Add("MT5:Symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(options.TimeZone))
        {
            failures.Add("MT5:TimeZone is required.");
        }

        if (!string.Equals(options.TimeZone, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("MT5:TimeZone must be UTC because MT5 bar timestamps are normalized to UTC.");
        }

        if (options.Enabled)
        {
            RequireConfigured(options.Login, "MT5:Login", failures);
            RequireConfigured(options.Password, "MT5:Password", failures);
            RequireConfigured(options.Server, "MT5:Server", failures);
            RequireConfigured(options.TerminalPath, "MT5:TerminalPath", failures);
            RequireConfigured(options.PythonExecutable, "MT5:PythonExecutable", failures);

            if (ConfigurationValidation.IsConfigured(options.Login)
                && !long.TryParse(options.Login, out _))
            {
                failures.Add("MT5:Login must be a numeric account identifier.");
            }

            if (ConfigurationValidation.IsConfigured(options.TerminalPath)
                && (!Path.IsPathFullyQualified(options.TerminalPath)
                    || !string.Equals(Path.GetExtension(options.TerminalPath), ".exe", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add("MT5:TerminalPath must be an absolute path to terminal64.exe.");
            }
        }

        return ConfigurationValidation.Result(failures);
    }

    private static void RequireConfigured(string value, string key, List<string> failures)
    {
        if (!ConfigurationValidation.IsConfigured(value))
        {
            failures.Add($"{key} is required when MT5:Enabled is true.");
        }
    }
}
