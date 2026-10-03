namespace XauAi.Infrastructure.Configuration.Options;

public sealed class AnalystOptions
{
    public const string SectionName = "Analysts";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = "None";

    public AnalystSourceType SourceType { get; set; } = AnalystSourceType.Manual;

    public bool RequiresApiKey { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 2;

    public int RateLimitPerMinute { get; set; }
}

public enum AnalystSourceType
{
    OfficialApi,
    RssFeed,
    PublicSource,
    PermittedWeb,
    Manual
}
