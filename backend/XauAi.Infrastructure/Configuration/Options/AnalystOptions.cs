namespace XauAi.Infrastructure.Configuration.Options;

public sealed class AnalystOptions
{
    public const string SectionName = "Analysts";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = "RssAtom";

    public string ProviderKey { get; set; } = "analyst-rss";

    public AnalystSourceType SourceType { get; set; } = AnalystSourceType.RssFeed;

    public bool RequiresApiKey { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 2;

    public int RateLimitPerMinute { get; set; }

    public string Symbol { get; set; } = "XAUUSD";

    public int InitialLookbackDays { get; set; } = 7;

    public int CollectionOverlapMinutes { get; set; } = 30;

    public int SyncIntervalMinutes { get; set; } = 60;

    public int ProviderPageSize { get; set; } = 100;

    public int MaximumPagesPerSync { get; set; } = 10;

    public int MaximumPageSize { get; set; } = 100;

    public int MaximumCollectionRangeDays { get; set; } = 3650;

    public int RetryBaseDelaySeconds { get; set; } = 2;

    public string RelevanceKeywords { get; set; } =
        "xauusd,gold,xau,bullion,precious metals,us dollar,usd,dxy,federal reserve,fed,fomc,cpi,pce,nfp,treasury yield,interest rate,inflation,geopolitical";
}

public enum AnalystSourceType
{
    OfficialApi,
    RssFeed,
    PublicSource,
    PermittedWeb,
    Manual
}
