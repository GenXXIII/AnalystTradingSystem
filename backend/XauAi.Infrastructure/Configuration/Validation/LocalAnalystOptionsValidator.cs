using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class LocalAnalystOptionsValidator : IValidateOptions<LocalAnalystOptions>
{
    public ValidateOptionsResult Validate(string? name, LocalAnalystOptions options)
    {
        var failures = new List<string>();
        if (!string.Equals(options.Symbol, "XAUUSD", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("LocalAnalyst:Symbol must be XAUUSD in Phase 12.");
        }

        var timeframes = options.Timeframes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (timeframes.Length == 0 || timeframes.Any(value => !MarketTimeframes.TryParse(value, out _)))
        {
            failures.Add("LocalAnalyst:Timeframes must contain only M1, M5, M15, M30, H1, H4, or D1.");
        }

        Range(options.EvaluationIntervalSeconds, 5, 3600, "LocalAnalyst:EvaluationIntervalSeconds", failures);
        Range(options.HistoryLimit, 205, 5000, "LocalAnalyst:HistoryLimit", failures);
        Range(options.MinimumCandles, 50, options.HistoryLimit, "LocalAnalyst:MinimumCandles", failures);
        Range(options.StaleAfterIntervals, 1, 100, "LocalAnalyst:StaleAfterIntervals", failures);
        Range(options.MaximumAllowedGaps, 0, 1000, "LocalAnalyst:MaximumAllowedGaps", failures);

        var weights = new[]
        {
            options.StructureWeight, options.TrendWeight, options.LiquidityWeight,
            options.CandleWeight, options.MomentumWeight, options.KtrWeight
        };
        if (weights.Any(value => value <= 0m || value > 100m))
        {
            failures.Add("LocalAnalyst component weights must be greater than 0 and at most 100.");
        }

        var maximumScore = weights.Sum();
        DecimalRange(options.EntryScoreThreshold, 0.01m, maximumScore, "LocalAnalyst:EntryScoreThreshold", failures);
        DecimalRange(options.MinimumDirectionalLead, 0m, maximumScore, "LocalAnalyst:MinimumDirectionalLead", failures);
        DecimalRange(options.StopOpposingScoreThreshold, 0.01m, maximumScore, "LocalAnalyst:StopOpposingScoreThreshold", failures);
        if (!(options.RsiBearishMinimum < options.RsiBearishMaximum
            && options.RsiBearishMaximum <= options.RsiBullishMinimum
            && options.RsiBullishMinimum < options.RsiBullishMaximum
            && options.RsiBullishMaximum <= 100m))
        {
            failures.Add("LocalAnalyst RSI thresholds must progress from bearish through bullish inside 0-100.");
        }
        DecimalRange(options.InvalidationAtrMultiplier, 0.1m, 20m, "LocalAnalyst:InvalidationAtrMultiplier", failures);
        DecimalRange(options.TargetAtrMultiplier, 0.1m, 50m, "LocalAnalyst:TargetAtrMultiplier", failures);
        DecimalRange(options.EqualLevelToleranceAtr, 0.001m, 5m, "LocalAnalyst:EqualLevelToleranceAtr", failures);
        DecimalRange(options.ImportantLevelDistanceAtr, 0.001m, 10m, "LocalAnalyst:ImportantLevelDistanceAtr", failures);

        if (!TryParseValidity(options.ValidityCandles, out var validity)
            || timeframes.Any(value => MarketTimeframes.TryParse(value, out var timeframe) && !validity.ContainsKey(timeframe)))
        {
            failures.Add("LocalAnalyst:ValidityCandles must define positive candle counts as TIMEFRAME:COUNT for every enabled timeframe.");
        }

        if (string.IsNullOrWhiteSpace(options.ConfigurationVersion) || options.ConfigurationVersion.Length > 64)
        {
            failures.Add("LocalAnalyst:ConfigurationVersion is required and must be at most 64 characters.");
        }

        return ConfigurationValidation.Result(failures);
    }

    internal static bool TryParseValidity(
        string value,
        out IReadOnlyDictionary<MarketTimeframe, int> validity)
    {
        var parsed = new Dictionary<MarketTimeframe, int>();
        foreach (var entry in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !MarketTimeframes.TryParse(parts[0], out var timeframe)
                || !int.TryParse(parts[1], out var count)
                || count is < 1 or > 10000
                || !parsed.TryAdd(timeframe, count))
            {
                validity = new Dictionary<MarketTimeframe, int>();
                return false;
            }
        }

        validity = parsed;
        return parsed.Count > 0;
    }

    private static void Range(int value, int minimum, int maximum, string key, List<string> failures)
    {
        if (value < minimum || value > maximum)
        {
            failures.Add($"{key} must be between {minimum} and {maximum}.");
        }
    }

    private static void DecimalRange(decimal value, decimal minimum, decimal maximum, string key, List<string> failures)
    {
        if (value < minimum || value > maximum)
        {
            failures.Add($"{key} must be between {minimum} and {maximum}.");
        }
    }
}
