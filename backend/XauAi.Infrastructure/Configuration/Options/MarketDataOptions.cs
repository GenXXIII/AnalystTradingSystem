namespace XauAi.Infrastructure.Configuration.Options;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    public string Provider { get; set; } = "AllTick";

    public string ProviderKey { get; set; } = "alltick";

    public bool SyncEnabled { get; set; }

    public string Symbol { get; set; } = "XAUUSD";

    public string Timeframes { get; set; } = "M1,M5,M15,M30,H1,H4,D1";

    public int InitialHistoryDays { get; set; } = 7;

    public int SyncIntervalSeconds { get; set; } = 60;

    public int BatchSize { get; set; } = 1000;

    public int MaxApiLimit { get; set; } = 5000;

    public int MaxQueryRangeDays { get; set; } = 366;

    public int MaxRetries { get; set; } = 2;

    public int RetryBaseDelaySeconds { get; set; } = 2;

    public int MaxGapResults { get; set; } = 1000;

    public bool IncludeFormingCandle { get; set; }
}
