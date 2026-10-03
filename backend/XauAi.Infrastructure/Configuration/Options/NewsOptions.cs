namespace XauAi.Infrastructure.Configuration.Options;

public sealed class NewsOptions
{
    public const string SectionName = "News";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = "None";

    public bool RequiresApiKey { get; set; } = true;

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 2;

    public int RateLimitPerMinute { get; set; }

    public bool UseTimeframeParameter { get; set; }

    public string ProviderKey { get; set; } = "newsdata";

    public string Symbol { get; set; } = "XAUUSD";

    public string Language { get; set; } = "en";

    public string ProviderQuery { get; set; } =
        "gold OR bullion OR XAUUSD OR \"Federal Reserve\" OR FOMC OR inflation OR CPI OR NFP OR USD";

    public int InitialLookbackHours { get; set; } = 24;

    public int CollectionOverlapMinutes { get; set; } = 10;

    public int CollectionIntervalSeconds { get; set; } = 900;

    public int PageSize { get; set; } = 10;

    public int MaximumPagesPerCollection { get; set; } = 3;

    public int MaximumPageSize { get; set; } = 100;

    public int MaximumCollectionRangeDays { get; set; } = 31;

    public int RetryBaseDelaySeconds { get; set; } = 2;

    public bool ArchiveEnabled { get; set; }

    public string MinimumRelevance { get; set; } = "Medium";

    public string GoldKeywords { get; set; } = "gold,bullion,precious metal,xau,xauusd";

    public string UsdKeywords { get; set; } = "us dollar,usd,dxy,dollar index,greenback";

    public string FedKeywords { get; set; } = "federal reserve,fomc,jerome powell,powell,fed chair";

    public string InflationKeywords { get; set; } = "inflation,consumer price index,cpi,personal consumption expenditures,pce";

    public string EmploymentKeywords { get; set; } = "nonfarm payroll,non-farm payroll,nfp,jobs report,unemployment,jobless claims";

    public string RatesKeywords { get; set; } = "interest rate,rate cut,rate hike,treasury yield,bond yield,real yield";

    public string EconomyKeywords { get; set; } = "us economy,gdp,economic growth,recession,retail sales,ism,consumer confidence";

    public string CentralBankKeywords { get; set; } = "central bank,ecb,bank of japan,boj,bank of england,pboc";

    public string GeopoliticsKeywords { get; set; } = "geopolitical,sanctions,war,conflict,ceasefire,trade tensions,safe haven";

    public string CommodityKeywords { get; set; } = "commodity,commodities,silver,oil price,energy market";
}
