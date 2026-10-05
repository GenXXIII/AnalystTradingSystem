namespace XauAi.Domain.Analysts;

public sealed class AnalystPublication
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public Guid AnalystSourceId { get; set; }

    public Guid? OriginalPublicationId { get; set; }

    public Guid? RelatedPublicationId { get; set; }

    public string? ExternalId { get; set; }

    public string IdentityHash { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string? SourceUrl { get; set; }

    public string? SourceUrlHash { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public string Language { get; set; } = "und";

    public string Category { get; set; } = "Other";

    public string RelationshipType { get; set; } = "Original";

    public int Version { get; set; } = 1;

    public DateTimeOffset PublishedAtUtc { get; set; }

    public DateTimeOffset CollectedAtUtc { get; set; }

    public DateTimeOffset? ProviderUpdatedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
