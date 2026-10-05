namespace XauAi.Domain.Evidence;

public sealed class EvidenceRecord
{
    public Guid Id { get; set; }

    public Guid? DataProviderId { get; set; }

    public Guid? InstrumentId { get; set; }

    public Guid? TimeframeId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public string SourceKey { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public string IdentityHash { get; set; } = string.Empty;

    public string? ContentHash { get; set; }

    public string CanonicalSymbol { get; set; } = string.Empty;

    public string? OriginalSymbol { get; set; }

    public string? TimeframeCode { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }

    public DateTimeOffset AvailableAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public DateTimeOffset? CollectedAtUtc { get; set; }

    public DateTimeOffset? ValidFromUtc { get; set; }

    public DateTimeOffset? ValidToUtc { get; set; }

    public string? Title { get; set; }

    public string? Summary { get; set; }

    public decimal? NumericValue { get; set; }

    public string? OriginalValue { get; set; }

    public string? Unit { get; set; }

    public string? OriginalUnit { get; set; }

    public string Direction { get; set; } = string.Empty;

    public string? OriginalDirection { get; set; }

    public string Importance { get; set; } = string.Empty;

    public string? OriginalImportance { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? OriginalCategory { get; set; }

    public string? CurrencyCode { get; set; }

    public string? OriginalSourceUrl { get; set; }

    public string Quality { get; set; } = string.Empty;

    public string Completeness { get; set; } = string.Empty;

    public string TimestampQuality { get; set; } = string.Empty;

    public string SourceReliability { get; set; } = string.Empty;

    public bool IsRelevant { get; set; }

    public string? RelevanceReason { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class EvidenceRelation
{
    public Guid EvidenceId { get; set; }

    public Guid RelatedEvidenceId { get; set; }

    public string RelationType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class EvidenceCluster
{
    public Guid Id { get; set; }

    public string ClusterType { get; set; } = string.Empty;

    public string DeterministicKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset EventTimeUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class EvidenceClusterMember
{
    public Guid EvidenceClusterId { get; set; }

    public Guid EvidenceId { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class EvidenceQuarantineRecord
{
    public Guid Id { get; set; }

    public string EvidenceType { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public string SourceKey { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public string PayloadHash { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTimeOffset? OriginalTimestampUtc { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset QuarantinedAtUtc { get; set; }
}
