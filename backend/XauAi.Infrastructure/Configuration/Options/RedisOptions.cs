namespace XauAi.Infrastructure.Configuration.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = "XauAi:";

    public int DefaultExpirationSeconds { get; set; } = 300;
}
