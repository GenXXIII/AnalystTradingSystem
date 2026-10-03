namespace XauAi.Application.News;

public sealed class NewsSettings
{
    public bool Enabled { get; init; }

    public string Provider { get; init; } = "NewsData";

    public string ProviderKey { get; init; } = "newsdata";

    public string Symbol { get; init; } = "XAUUSD";

    public string Language { get; init; } = "en";

    public string ProviderQuery { get; init; } =
        "gold OR bullion OR XAUUSD OR \"Federal Reserve\" OR FOMC OR inflation OR CPI OR NFP OR USD";

    public int InitialLookbackHours { get; init; } = 24;

    public int CollectionOverlapMinutes { get; init; } = 10;

    public int CollectionIntervalSeconds { get; init; } = 900;

    public int PageSize { get; init; } = 10;

    public int MaximumPagesPerCollection { get; init; } = 3;

    public int MaximumPageSize { get; init; } = 100;

    public int MaximumCollectionRangeDays { get; init; } = 31;

    public int MaxRetries { get; init; } = 2;

    public int RetryBaseDelaySeconds { get; init; } = 2;

    public bool ArchiveEnabled { get; init; }

    public NewsRelevanceLevel MinimumRelevance { get; init; } = NewsRelevanceLevel.Medium;

    public IReadOnlyList<string> GoldKeywords { get; init; } =
        ["gold", "bullion", "precious metal", "xau", "xauusd"];

    public IReadOnlyList<string> UsdKeywords { get; init; } =
        ["us dollar", "usd", "dxy", "dollar index", "greenback"];

    public IReadOnlyList<string> FedKeywords { get; init; } =
        ["federal reserve", "fomc", "jerome powell", "powell", "fed chair"];

    public IReadOnlyList<string> InflationKeywords { get; init; } =
        ["inflation", "consumer price index", "cpi", "personal consumption expenditures", "pce"];

    public IReadOnlyList<string> EmploymentKeywords { get; init; } =
        ["nonfarm payroll", "non-farm payroll", "nfp", "jobs report", "unemployment", "jobless claims"];

    public IReadOnlyList<string> RatesKeywords { get; init; } =
        ["interest rate", "rate cut", "rate hike", "treasury yield", "bond yield", "real yield"];

    public IReadOnlyList<string> EconomyKeywords { get; init; } =
        ["us economy", "gdp", "economic growth", "recession", "retail sales", "ism", "consumer confidence"];

    public IReadOnlyList<string> CentralBankKeywords { get; init; } =
        ["central bank", "ecb", "bank of japan", "boj", "bank of england", "pboc"];

    public IReadOnlyList<string> GeopoliticsKeywords { get; init; } =
        ["geopolitical", "sanctions", "war", "conflict", "ceasefire", "trade tensions", "safe haven"];

    public IReadOnlyList<string> CommodityKeywords { get; init; } =
        ["commodity", "commodities", "silver", "oil price", "energy market"];
}
