namespace XauAi.Infrastructure.Configuration.Options;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string Name { get; set; } = "XAUUSD-AI";

    public string Version { get; set; } = "1.0.0";
}
