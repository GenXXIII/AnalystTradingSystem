namespace XauAi.Infrastructure.Configuration.Options;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = "None";

    public bool RequiresApiKey { get; set; } = true;

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public double Temperature { get; set; } = 0.2;

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxRetries { get; set; } = 2;

    public int MaxOutputTokens { get; set; } = 2_000;

    public decimal DailyBudgetUsd { get; set; }

    public int DailyRequestLimit { get; set; }
}
