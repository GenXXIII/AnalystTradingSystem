using XauAi.Application.MarketData;

namespace XauAi.Application.TechnicalAnalysis;

internal sealed class SupportResistanceAnalyzer : ISupportResistanceAnalyzer
{
    public IReadOnlyList<SupportResistanceZone> Analyze(
        IReadOnlyList<StoredMarketCandle> candles,
        MarketTimeframe timeframe,
        IReadOnlyList<MarketSwing> swings,
        TechnicalAnalysisSettings settings)
    {
        if (candles.Count == 0 || swings.Count == 0)
        {
            return [];
        }

        var currentPrice = candles[^1].Close;
        var tolerance = Math.Max(currentPrice * settings.LevelTolerancePercent / 100m, 0.00001m);
        var groups = new List<List<MarketSwing>>();
        foreach (var swing in swings.OrderBy(value => value.Price))
        {
            var group = groups.FirstOrDefault(candidate =>
                Math.Abs(candidate.Average(value => value.Price) - swing.Price) <= tolerance);
            if (group is null)
            {
                groups.Add([swing]);
            }
            else
            {
                group.Add(swing);
            }
        }

        return [.. groups
            .Where(group => group.Count >= settings.MinimumLevelTouches)
            .Select(group => CreateZone(group, timeframe, currentPrice, tolerance))
            .OrderByDescending(zone => zone.Touches)
            .ThenBy(zone => Math.Abs(zone.DistanceFromPricePercent))
            .Take(settings.MaximumLevelZones)];
    }

    private static SupportResistanceZone CreateZone(
        IReadOnlyList<MarketSwing> swings,
        MarketTimeframe timeframe,
        decimal currentPrice,
        decimal tolerance)
    {
        var center = swings.Average(swing => swing.Price);
        var type = center < currentPrice - tolerance ? PriceZoneType.Support
            : center > currentPrice + tolerance ? PriceZoneType.Resistance
            : PriceZoneType.Pivot;
        var strength = swings.Count switch
        {
            >= 5 => "Strong",
            >= 3 => "Moderate",
            _ => "Developing"
        };
        return new SupportResistanceZone(
            center - tolerance,
            center + tolerance,
            center,
            type,
            strength,
            swings.Count,
            [timeframe],
            currentPrice == 0m ? 0m : ((center - currentPrice) / currentPrice) * 100m);
    }
}
