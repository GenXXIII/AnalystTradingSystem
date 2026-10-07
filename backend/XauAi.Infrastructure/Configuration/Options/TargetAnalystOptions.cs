namespace XauAi.Infrastructure.Configuration.Options;

public sealed class TargetAnalystOptions
{
    public const string SectionName = "TargetAnalyst";

    public bool Enabled { get; set; }

    public string Symbol { get; set; } = "XAUUSD";

    public string Timeframes { get; set; } = "M1,M5,M15,M30,H1,H4,D1";

    public int EvidenceLookbackHours { get; set; } = 720;

    public int MaximumEvidenceItemsPerWorkspace { get; set; } = 50;

    public int MaximumCompressedCharacters { get; set; } = 32_000;

    public int MinimumMarketTimeframes { get; set; } = 3;

    public int StaleAfterIntervals { get; set; } = 3;

    public decimal MinimumConfidence { get; set; } = 0.60m;

    public decimal MinimumTargetDistanceAtr { get; set; } = 0.25m;

    public int DefaultValidityMinutes { get; set; } = 240;

    public int MaximumValidityMinutes { get; set; } = 1_440;

    public int MonitorIntervalSeconds { get; set; } = 30;

    public int MaximumPageSize { get; set; } = 200;

    public string ConfigurationVersion { get; set; } = "phase13-v1";
}

public sealed class TargetAiWorkspacesOptions
{
    public const string SectionName = "TargetAiWorkspaces";

    public TargetAiWorkspaceOptions Structure { get; set; } = new() { PromptVersion = "phase13-structure-v1" };

    public TargetAiWorkspaceOptions Liquidity { get; set; } = new() { PromptVersion = "phase13-liquidity-v1" };

    public TargetAiWorkspaceOptions Candle { get; set; } = new() { PromptVersion = "phase13-candle-v1" };

    public TargetAiWorkspaceOptions Flow { get; set; } = new() { PromptVersion = "phase13-flow-v1" };

    public TargetAiWorkspaceOptions Ktr { get; set; } = new() { PromptVersion = "phase13-ktr-v1" };

    public TargetAiWorkspaceOptions News { get; set; } = new() { PromptVersion = "phase13-news-v1" };

    public TargetAiWorkspaceOptions Risk { get; set; } = new() { PromptVersion = "phase13-risk-v1" };

    public TargetAiWorkspaceOptions Master { get; set; } = new() { PromptVersion = "phase13-master-v1" };
}

public sealed class TargetAiWorkspaceOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "None";

    public string Adapter { get; set; } = "OpenAiCompatible";

    public bool RequiresApiKey { get; set; } = true;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public double Temperature { get; set; } = 0.1;

    public int TimeoutSeconds { get; set; } = 90;

    public int MaxOutputTokens { get; set; } = 2_500;

    public int MaxRetries { get; set; } = 2;

    public int RequestsPerMinute { get; set; } = 10;

    public string PromptVersion { get; set; } = "phase13-v1";

    public string ConfigurationVersion { get; set; } = "phase13-v1";
}
