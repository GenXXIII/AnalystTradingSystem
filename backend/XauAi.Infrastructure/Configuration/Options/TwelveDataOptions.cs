namespace XauAi.Infrastructure.Configuration.Options;

public sealed class TwelveDataOptions
{
    public const string SectionName = "TwelveData";

    public bool Enabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string ApplicationSymbol { get; set; } = "XAUUSD";

    public string Symbol { get; set; } = "XAU/USD";

    public string ProviderKey { get; set; } = "twelvedata";

    public string BaseUrl { get; set; } = "https://api.twelvedata.com/";

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int MinimumRequestIntervalSeconds { get; set; } = 8;

    public int ReferenceSyncIntervalSeconds { get; set; } = 900;

    public int InitialHistoryDays { get; set; } = 7;

    public int IncrementalLookbackMinutes { get; set; } = 180;

    public int MaximumPointsPerRequest { get; set; } = 5000;

    public decimal MaximumCloseDeviationBps { get; set; } = 30m;
}
