namespace XauAi.Infrastructure.Configuration.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool ApplyMigrationsOnStartup { get; set; }
}
