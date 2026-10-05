namespace XauAi.Application.Analysts;

public sealed class AnalystSettings
{
    public bool Enabled { get; init; }

    public string Provider { get; init; } = "RssAtom";

    public string ProviderKey { get; init; } = "analyst-rss";

    public string Symbol { get; init; } = "XAUUSD";

    public int InitialLookbackDays { get; init; } = 7;

    public int CollectionOverlapMinutes { get; init; } = 30;

    public int SyncIntervalMinutes { get; init; } = 60;

    public int ProviderPageSize { get; init; } = 100;

    public int MaximumPagesPerSync { get; init; } = 10;

    public int MaximumPageSize { get; init; } = 100;

    public int MaximumCollectionRangeDays { get; init; } = 3650;

    public int MaxRetries { get; init; } = 2;

    public int RetryBaseDelaySeconds { get; init; } = 2;

    public IReadOnlyList<string> RelevanceKeywords { get; init; } =
    [
        "xauusd", "gold", "xau", "bullion", "precious metals", "us dollar", "usd", "dxy",
        "federal reserve", "fed", "fomc", "cpi", "pce", "nfp", "treasury yield",
        "interest rate", "inflation", "geopolitical"
    ];
}
