namespace XauAi.Infrastructure.Configuration.Options;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public bool EnableSwagger { get; set; }

    public CorsOptions Cors { get; set; } = new();
}

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = [];
}
