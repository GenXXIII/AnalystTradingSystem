namespace XauAi.Infrastructure.Configuration.Options;

public sealed class EconomicDataOptions
{
    public const string SectionName = "EconomicData";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = "None";

    public string ProviderKey { get; set; } = "fred";

    public bool RequiresApiKey { get; set; } = true;

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.stlouisfed.org/fred/";

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 2;

    public int RateLimitPerMinute { get; set; }

    public int InitialHistoryYears { get; set; } = 20;

    public int RevisionLookbackDays { get; set; } = 370;

    public int SyncIntervalMinutes { get; set; } = 360;

    public int ProviderPageSize { get; set; } = 1000;

    public int MaximumPagesPerSeries { get; set; } = 100;

    public int MaximumPageSize { get; set; } = 500;

    public int MaximumQueryRangeYears { get; set; } = 100;

    public int RetryBaseDelaySeconds { get; set; } = 2;

    public string TrackedSeries { get; set; } =
        "CPIAUCSL:Inflation,CPILFESL:Inflation,PCEPI:Inflation,PCEPILFE:Inflation," +
        "UNRATE:Employment,PAYEMS:Employment,FEDFUNDS:InterestRates," +
        "DGS2:Treasury,DGS5:Treasury,DGS10:Treasury,DGS30:Treasury," +
        "GDP:EconomicGrowth,GDPC1:EconomicGrowth,RSAFS:Consumer,INDPRO:Production,UMCSENT:Consumer";
}
