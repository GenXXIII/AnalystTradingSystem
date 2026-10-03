using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Configuration.Validation;

internal sealed class TechnicalAnalysisOptionsValidator : IValidateOptions<TechnicalAnalysisOptions>
{
    public ValidateOptionsResult Validate(string? name, TechnicalAnalysisOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Symbol))
        {
            failures.Add("TechnicalAnalysis:Symbol is required.");
        }

        ValidateTimeframes(options.Timeframes, failures);
        ValidatePeriods(options.SmaPeriods, "TechnicalAnalysis:SmaPeriods", failures);
        ValidatePeriods(options.EmaPeriods, "TechnicalAnalysis:EmaPeriods", failures);
        Range(options.HistoryLimit, 200, 10000, "TechnicalAnalysis:HistoryLimit", failures);
        Range(options.RsiPeriod, 2, 200, "TechnicalAnalysis:RsiPeriod", failures);
        Range(options.MacdFastPeriod, 2, 200, "TechnicalAnalysis:MacdFastPeriod", failures);
        Range(options.MacdSlowPeriod, 3, 400, "TechnicalAnalysis:MacdSlowPeriod", failures);
        Range(options.MacdSignalPeriod, 2, 200, "TechnicalAnalysis:MacdSignalPeriod", failures);
        if (options.MacdFastPeriod >= options.MacdSlowPeriod)
        {
            failures.Add("TechnicalAnalysis:MacdFastPeriod must be less than MacdSlowPeriod.");
        }

        Range(options.AtrPeriod, 2, 200, "TechnicalAnalysis:AtrPeriod", failures);
        Range(options.AdxPeriod, 2, 200, "TechnicalAnalysis:AdxPeriod", failures);
        Range(options.BollingerPeriod, 2, 400, "TechnicalAnalysis:BollingerPeriod", failures);
        DecimalRange(options.BollingerStandardDeviations, 0.1m, 10m, "TechnicalAnalysis:BollingerStandardDeviations", failures);
        Range(options.StochasticKPeriod, 2, 400, "TechnicalAnalysis:StochasticKPeriod", failures);
        Range(options.StochasticDPeriod, 2, 100, "TechnicalAnalysis:StochasticDPeriod", failures);
        Range(options.SwingWindow, 1, 20, "TechnicalAnalysis:SwingWindow", failures);
        DecimalRange(options.LevelTolerancePercent, 0.001m, 5m, "TechnicalAnalysis:LevelTolerancePercent", failures);
        Range(options.MinimumLevelTouches, 2, 20, "TechnicalAnalysis:MinimumLevelTouches", failures);
        Range(options.MaximumLevelZones, 1, 50, "TechnicalAnalysis:MaximumLevelZones", failures);
        Range(options.PriceActionLookback, 2, 500, "TechnicalAnalysis:PriceActionLookback", failures);
        Range(options.VolatilityLookback, 5, 500, "TechnicalAnalysis:VolatilityLookback", failures);
        if (!(options.VeryLowVolatilityRatio < options.LowVolatilityRatio
            && options.LowVolatilityRatio < options.HighVolatilityRatio
            && options.HighVolatilityRatio < options.VeryHighVolatilityRatio))
        {
            failures.Add("TechnicalAnalysis volatility ratios must increase from VeryLow through VeryHigh.");
        }

        return ConfigurationValidation.Result(failures);
    }

    private static void ValidateTimeframes(string values, List<string> failures)
    {
        var parsed = values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parsed.Length == 0 || parsed.Any(value => !MarketTimeframes.TryParse(value, out _)))
        {
            failures.Add("TechnicalAnalysis:Timeframes must contain only M1, M5, M15, M30, H1, H4, or D1.");
        }
    }

    private static void ValidatePeriods(string values, string key, List<string> failures)
    {
        var parsed = values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parsed.Length == 0 || parsed.Any(value => !int.TryParse(value, out var period) || period is < 2 or > 1000))
        {
            failures.Add($"{key} must contain comma-separated periods between 2 and 1000.");
        }
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
