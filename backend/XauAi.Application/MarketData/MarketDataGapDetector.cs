namespace XauAi.Application.MarketData;

public interface IMarketSessionCalendar
{
    bool IsPotentiallyOpen(string symbol, DateTimeOffset candleOpenTimeUtc);
}

public sealed class DefaultMarketSessionCalendar : IMarketSessionCalendar
{
    private const int WeeklyCloseHourUtc = 22;
    private static readonly TimeSpan XauUsdDailyCloseUtc = new(20, 58, 0);
    private static readonly TimeSpan XauUsdDailyOpenUtc = new(22, 1, 0);

    public bool IsPotentiallyOpen(string symbol, DateTimeOffset candleOpenTimeUtc)
    {
        var utc = candleOpenTimeUtc.ToUniversalTime();
        if (string.Equals(symbol, "XAUUSD", StringComparison.OrdinalIgnoreCase))
        {
            return utc.DayOfWeek switch
            {
                DayOfWeek.Saturday => false,
                DayOfWeek.Sunday => utc.TimeOfDay >= XauUsdDailyOpenUtc,
                DayOfWeek.Friday => utc.TimeOfDay < XauUsdDailyCloseUtc,
                _ => utc.TimeOfDay < XauUsdDailyCloseUtc || utc.TimeOfDay >= XauUsdDailyOpenUtc
            };
        }

        return utc.DayOfWeek switch
        {
            DayOfWeek.Saturday => false,
            DayOfWeek.Sunday => utc.Hour >= WeeklyCloseHourUtc,
            DayOfWeek.Friday => utc.Hour < WeeklyCloseHourUtc,
            _ => true
        };
    }
}

public static class MarketDataGapDetector
{
    public static IReadOnlyList<MarketDataGap> Detect(
        string symbol,
        MarketTimeframe timeframe,
        IEnumerable<DateTimeOffset> openTimes,
        IMarketSessionCalendar sessionCalendar,
        int maxResults)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxResults, 1);

        var ordered = openTimes
            .Select(value => value.ToUniversalTime())
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        if (ordered.Length < 2)
        {
            return [];
        }

        var duration = timeframe.Duration();
        var gaps = new List<MarketDataGap>();
        for (var index = 1; index < ordered.Length && gaps.Count < maxResults; index++)
        {
            for (var expected = ordered[index - 1].Add(duration);
                 expected < ordered[index] && gaps.Count < maxResults;
                 expected = expected.Add(duration))
            {
                if (sessionCalendar.IsPotentiallyOpen(symbol, expected))
                {
                    gaps.Add(new MarketDataGap(expected, "UnverifiedMissing"));
                }
            }
        }

        return gaps;
    }
}
