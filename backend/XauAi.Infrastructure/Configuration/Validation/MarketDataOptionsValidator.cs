using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class MarketDataOptionsValidator : IValidateOptions<MarketDataOptions>
{
    public ValidateOptionsResult Validate(string? name, MarketDataOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Symbol))
        {
            failures.Add("MarketData:Symbol is required.");
        }

        var timeframeValues = options.Timeframes.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (timeframeValues.Length == 0
            || timeframeValues.Any(value => !MarketTimeframes.TryParse(value, out _)))
        {
            failures.Add("MarketData:Timeframes must contain only M1, M5, M15, M30, H1, H4, or D1.");
        }

        Range(options.InitialHistoryDays, 1, 3660, "MarketData:InitialHistoryDays", failures);
        Range(options.SyncIntervalSeconds, 10, 86400, "MarketData:SyncIntervalSeconds", failures);
        Range(options.BatchSize, 2, 10000, "MarketData:BatchSize", failures);
        Range(options.MaxApiLimit, 1, 10000, "MarketData:MaxApiLimit", failures);
        Range(options.MaxQueryRangeDays, 1, 3660, "MarketData:MaxQueryRangeDays", failures);
        Range(options.MaxRetries, 0, 10, "MarketData:MaxRetries", failures);
        Range(options.RetryBaseDelaySeconds, 1, 300, "MarketData:RetryBaseDelaySeconds", failures);
        Range(options.MaxGapResults, 1, 10000, "MarketData:MaxGapResults", failures);
        return ConfigurationValidation.Result(failures);
    }

    private static void Range(int value, int minimum, int maximum, string key, List<string> failures)
    {
        if (value < minimum || value > maximum)
        {
            failures.Add($"{key} must be between {minimum} and {maximum}.");
        }
    }
}
