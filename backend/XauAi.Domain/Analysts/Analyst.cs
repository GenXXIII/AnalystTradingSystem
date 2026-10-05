namespace XauAi.Domain.Analysts;

public sealed class Analyst
{
    public Guid Id { get; set; }

    public Guid AnalystSourceId { get; set; }

    public string? ExternalId { get; set; }

    public string IdentityHash { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Role { get; set; }

    public string? ProfileUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
