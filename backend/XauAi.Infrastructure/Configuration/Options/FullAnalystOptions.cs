namespace XauAi.Infrastructure.Configuration.Options;

public sealed class FullAnalystOptions
{
    public const string SectionName = "FullAnalyst";

    public bool Enabled { get; set; }
    public string Symbol { get; set; } = "XAUUSD";
    public string Timeframes { get; set; } = "M1,M5,M15,M30,H1,H4,D1";
    public int EvidenceLookbackHours { get; set; } = 720;
    public int MaximumEvidenceItemsPerWorkspace { get; set; } = 8;
    public int MaximumCompressedCharacters { get; set; } = 2_500;
    public int MaximumTotalTokens { get; set; } = 15_000;
    public int MinimumMarketTimeframes { get; set; } = 3;
    public int StaleAfterIntervals { get; set; } = 3;
    public decimal MinimumConfidence { get; set; } = 0.65m;
    public int DefaultValidityMinutes { get; set; } = 120;
    public int MaximumValidityMinutes { get; set; } = 1_440;
    public int CacheMinutes { get; set; } = 5;
    public int MonitorIntervalSeconds { get; set; } = 30;
    public int MaximumPageSize { get; set; } = 200;
    public string ConfigurationVersion { get; set; } = "phase14-v1";
}

public sealed class FullAiWorkspacesOptions
{
    public const string SectionName = "FullAiWorkspaces";

    public FullAiWorkspaceOptions Structure { get; set; } = new() { PromptVersion = "phase14-structure-v1" };
    public FullAiWorkspaceOptions Liquidity { get; set; } = new() { PromptVersion = "phase14-liquidity-v1" };
    public FullAiWorkspaceOptions Candle { get; set; } = new() { PromptVersion = "phase14-candle-v1" };
    public FullAiWorkspaceOptions Flow { get; set; } = new() { PromptVersion = "phase14-flow-v1" };
    public FullAiWorkspaceOptions Ktr { get; set; } = new() { PromptVersion = "phase14-ktr-v1" };
    public FullAiWorkspaceOptions News { get; set; } = new() { PromptVersion = "phase14-news-v1" };
    public FullAiWorkspaceOptions Risk { get; set; } = new() { PromptVersion = "phase14-risk-v1" };
    public FullAiWorkspaceOptions Master { get; set; } = new() { PromptVersion = "phase14-master-v1" };
}

public sealed class FullAiWorkspaceOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "None";
    public string Adapter { get; set; } = "OpenAiCompatible";
    public bool RequiresApiKey { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string FallbackModels { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.1;
    public int TimeoutSeconds { get; set; } = 90;
    public int MaxOutputTokens { get; set; } = 450;
    public bool DisableReasoning { get; set; } = true;
    public int MaxRetries { get; set; } = 2;
    public int RequestsPerMinute { get; set; } = 10;
    public string PromptVersion { get; set; } = "phase14-v1";
    public string ConfigurationVersion { get; set; } = "phase14-v1";
}
