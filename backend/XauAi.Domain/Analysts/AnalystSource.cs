namespace XauAi.Domain.Analysts;

public sealed class AnalystSource
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public string? ExternalId { get; set; }

    public string IdentityHash { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "Other";

    public string? Website { get; set; }

    public string? CountryCode { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
