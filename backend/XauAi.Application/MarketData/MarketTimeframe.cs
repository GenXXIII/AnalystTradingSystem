namespace XauAi.Application.MarketData;

public enum MarketTimeframe
{
    M1,
    M5,
    M15,
    M30,
    H1,
    H4,
    D1
}

public static class MarketTimeframes
{
    public static bool TryParse(string? value, out MarketTimeframe timeframe)
    {
        timeframe = default;
        return value is not null
            && Enum.TryParse(value, ignoreCase: true, out timeframe)
            && Enum.IsDefined(timeframe)
            && !int.TryParse(value, out _);
    }

    public static TimeSpan Duration(this MarketTimeframe timeframe) => timeframe switch
    {
        MarketTimeframe.M1 => TimeSpan.FromMinutes(1),
        MarketTimeframe.M5 => TimeSpan.FromMinutes(5),
        MarketTimeframe.M15 => TimeSpan.FromMinutes(15),
        MarketTimeframe.M30 => TimeSpan.FromMinutes(30),
        MarketTimeframe.H1 => TimeSpan.FromHours(1),
        MarketTimeframe.H4 => TimeSpan.FromHours(4),
        MarketTimeframe.D1 => TimeSpan.FromDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported market timeframe.")
    };

    public static string Code(this MarketTimeframe timeframe) => timeframe.ToString();

    public static DateTimeOffset AlignDown(this MarketTimeframe timeframe, DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var durationSeconds = checked((long)timeframe.Duration().TotalSeconds);
        var unixSeconds = utc.ToUnixTimeSeconds();
        var alignedSeconds = unixSeconds - (unixSeconds % durationSeconds);
        return DateTimeOffset.FromUnixTimeSeconds(alignedSeconds);
    }
}
