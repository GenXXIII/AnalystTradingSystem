namespace XauAi.Infrastructure.Configuration.Options;

public sealed class AiInterpretationOptions
{
    public const string SectionName = "AiInterpretation";

    public string PromptVersion { get; set; } = "phase11-v2";

    public int DefaultLookbackHours { get; set; } = 168;

    public int MaximumLookbackHours { get; set; } = 17_520;

    public int MaximumEvidenceItems { get; set; } = 60;

    public int MaximumCompressedCharacters { get; set; } = 24_000;

    public int CurrentContextCacheMinutes { get; set; } = 5;

    public int MaximumPageSize { get; set; } = 200;
}

public sealed class AiSpecialistsOptions
{
    public const string SectionName = "AiSpecialists";

    public AiSpecialistOptions News { get; set; } = new();

    public AiSpecialistOptions Candle { get; set; } = new();

    public AiSpecialistOptions Structure { get; set; } = new();

    public AiSpecialistOptions Liquidity { get; set; } = new();

    public AiSpecialistOptions Flow { get; set; } = new();

    public AiSpecialistOptions Ktr { get; set; } = new();

    public AiSpecialistOptions Risk { get; set; } = new();

    public AiSpecialistOptions Master { get; set; } = new();
}

public sealed class AiSpecialistOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "None";

    public string Adapter { get; set; } = "OpenAiCompatible";

    public bool RequiresApiKey { get; set; } = true;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public double Temperature { get; set; } = 0.2;

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxOutputTokens { get; set; } = 2_000;

    public int MaxRetries { get; set; } = 2;

    public int RequestsPerMinute { get; set; } = 10;
}
