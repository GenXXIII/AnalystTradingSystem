namespace XauAi.Application.Evidence;

public enum EvidenceType
{
    Market,
    Technical,
    Candle,
    CandleFlow,
    Liquidity,
    OrderFlow,
    Session,
    News,
    Economic,
    Analyst
}

public enum EvidenceSourceType
{
    InternalMarketData,
    InternalTechnicalEngine,
    NewsProvider,
    EconomicProvider,
    AnalystProvider,
    Manual,
    Other
}

public enum EvidenceDirection
{
    Unknown,
    Bullish,
    Bearish,
    Neutral
}

public enum EvidenceImportance
{
    Unknown,
    Low,
    Medium,
    High,
    Critical
}

public enum EvidenceCategory
{
    Other,
    Fed,
    InterestRates,
    Inflation,
    Employment,
    Gdp,
    Usd,
    Treasury,
    Geopolitical,
    CentralBank,
    Commodity,
    MarketStructure,
    Liquidity,
    Technical,
    Session
}

public enum EvidenceUnit
{
    Unknown,
    Price,
    Percent,
    Index,
    Usd,
    UsdBillions,
    UsdTrillions,
    Thousands,
    Millions,
    Count,
    BasisPoints
}

public enum EvidenceQuality
{
    Unknown,
    Low,
    Medium,
    High
}

public enum EvidenceCompleteness
{
    Unknown,
    Partial,
    Complete
}

public enum EvidenceTimestampQuality
{
    Unknown,
    Approximate,
    DateOnly,
    Exact
}

public enum EvidenceSourceReliability
{
    Unknown,
    Known
}

public enum EvidenceRelationType
{
    Duplicate,
    Republished,
    SameEvent,
    Contradicts,
    Supports,
    Updates,
    References
}

public enum EvidenceClusterType
{
    NewsEvent,
    EconomicEvent,
    MarketEvent,
    Other
}

public enum EvidenceCoverageState
{
    Unavailable,
    Partial,
    Available
}

public enum EvidenceTimeframeRole
{
    None,
    Confirmation,
    Primary,
    Context
}

public sealed class EvidenceInput
{
    public EvidenceType EvidenceType { get; init; }

    public EvidenceSourceType SourceType { get; init; }

    public required string SourceKey { get; init; }

    public string? ExternalId { get; init; }

    public required string Instrument { get; init; }

    public string? OriginalInstrument { get; init; }

    public string? Timeframe { get; init; }

    public required DateTimeOffset EventTime { get; init; }

    public required DateTimeOffset AvailableAt { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public DateTimeOffset? CollectedAt { get; init; }

    public DateTimeOffset? ValidFrom { get; init; }

    public DateTimeOffset? ValidTo { get; init; }

    public string? Title { get; init; }

    public string? Summary { get; init; }

    public decimal? Value { get; init; }

    public string? OriginalValue { get; init; }

    public string? Unit { get; init; }

    public string? Direction { get; init; }

    public string? Importance { get; init; }

    public string? Category { get; init; }

    public string? CurrencyCode { get; init; }

    public string? OriginalSourceUrl { get; init; }

    public EvidenceTimestampQuality TimestampQuality { get; init; } = EvidenceTimestampQuality.Exact;

    public EvidenceSourceReliability SourceReliability { get; init; } = EvidenceSourceReliability.Unknown;

    public string? MetadataJson { get; init; }

    public EvidenceClusterType? ClusterType { get; init; }

    public string? ClusterKey { get; init; }
}

public sealed record NormalizedEvidenceItem(
    Guid Id,
    EvidenceType EvidenceType,
    EvidenceSourceType SourceType,
    string SourceKey,
    string? ExternalId,
    string IdentityHash,
    string ContentHash,
    string CanonicalSymbol,
    string OriginalSymbol,
    string? Timeframe,
    DateTimeOffset EventTimeUtc,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? CollectedAtUtc,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? Title,
    string? Summary,
    decimal? Value,
    string? OriginalValue,
    EvidenceUnit Unit,
    string? OriginalUnit,
    EvidenceDirection Direction,
    string? OriginalDirection,
    EvidenceImportance Importance,
    string? OriginalImportance,
    EvidenceCategory Category,
    string? OriginalCategory,
    string? CurrencyCode,
    string? OriginalSourceUrl,
    EvidenceQuality Quality,
    EvidenceCompleteness Completeness,
    EvidenceTimestampQuality TimestampQuality,
    EvidenceSourceReliability SourceReliability,
    bool IsRelevant,
    string RelevanceReason,
    string? MetadataJson,
    EvidenceClusterType? ClusterType,
    string? ClusterKey,
    DateTimeOffset CreatedAtUtc);

public sealed record EvidenceNormalizationResult(
    NormalizedEvidenceItem? Item,
    string? ErrorCode,
    string? ErrorMessage,
    string PayloadHash)
{
    public bool IsValid => Item is not null;
}

public sealed record EvidenceQuery(
    string? Instrument,
    EvidenceType? EvidenceType,
    EvidenceSourceType? SourceType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? Timeframe,
    EvidenceDirection? Direction,
    EvidenceImportance? Importance,
    bool? RelevantOnly,
    DateTimeOffset? AsOfUtc,
    int Page,
    int PageSize);

public sealed record EvidenceStoreQuery(
    string? Instrument,
    EvidenceType? EvidenceType,
    EvidenceSourceType? SourceType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? Timeframe,
    EvidenceDirection? Direction,
    EvidenceImportance? Importance,
    bool? RelevantOnly,
    DateTimeOffset AsOfUtc,
    int Page,
    int PageSize);

public sealed record EvidencePackRequest(
    string Instrument,
    string PrimaryTimeframe,
    string? ConfirmationTimeframe,
    DateTimeOffset? AnalysisTimeUtc,
    int? LookbackDays);

public sealed record EvidenceConflictQuery(
    string Instrument,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? Timeframe,
    DateTimeOffset? AsOfUtc);

public sealed record EvidenceSourceQuery(
    EvidenceSourceType? SourceType,
    DateTimeOffset? AsOfUtc,
    int Page,
    int PageSize);

public sealed record EvidenceResult(
    Guid Id,
    EvidenceType EvidenceType,
    EvidenceSourceType SourceType,
    string SourceKey,
    string? ExternalId,
    string Instrument,
    string? OriginalInstrument,
    string? Timeframe,
    DateTimeOffset EventTimeUtc,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? CollectedAtUtc,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? Title,
    string? Summary,
    decimal? Value,
    string? OriginalValue,
    EvidenceUnit Unit,
    string? OriginalUnit,
    EvidenceDirection Direction,
    string? OriginalDirection,
    EvidenceImportance Importance,
    string? OriginalImportance,
    EvidenceCategory Category,
    string? OriginalCategory,
    string? CurrencyCode,
    string? OriginalSourceUrl,
    EvidenceQuality Quality,
    EvidenceCompleteness Completeness,
    EvidenceTimestampQuality TimestampQuality,
    EvidenceSourceReliability SourceReliability,
    bool IsRelevant,
    string? RelevanceReason,
    string? MetadataJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PagedEvidence(
    IReadOnlyList<EvidenceResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    DateTimeOffset AsOfUtc);

public sealed record EvidenceRelationResult(
    Guid EvidenceId,
    Guid RelatedEvidenceId,
    EvidenceRelationType RelationType,
    string Reason,
    DateTimeOffset CreatedAtUtc);

public sealed record EvidenceClusterResult(
    Guid Id,
    EvidenceClusterType ClusterType,
    string Title,
    DateTimeOffset EventTimeUtc,
    string Role);

public sealed record EvidenceDetailResult(
    EvidenceResult Evidence,
    IReadOnlyList<EvidenceRelationResult> Relations,
    IReadOnlyList<EvidenceClusterResult> Clusters);

public sealed record EvidenceConflictResult(
    Guid FirstEvidenceId,
    Guid SecondEvidenceId,
    string Instrument,
    string? Timeframe,
    EvidenceCategory Category,
    EvidenceDirection FirstDirection,
    EvidenceDirection SecondDirection,
    string Reason);

public sealed record EvidenceSourceSummary(
    string SourceKey,
    EvidenceSourceType SourceType,
    int EvidenceCount,
    DateTimeOffset LatestAvailableAtUtc);

public sealed record PagedEvidenceSources(
    IReadOnlyList<EvidenceSourceSummary> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    DateTimeOffset AsOfUtc);

public sealed record EvidenceTimeframeGroup(
    string Timeframe,
    EvidenceTimeframeRole Role,
    IReadOnlyList<EvidenceResult> Items);

public sealed record EvidenceTypeGroup(
    EvidenceType EvidenceType,
    IReadOnlyList<EvidenceResult> Items);

public sealed record EvidenceCoverage(
    EvidenceType EvidenceType,
    EvidenceCoverageState State,
    int RecordCount,
    int CompleteCount,
    int PartialCount);

public sealed record EvidencePack(
    string Instrument,
    DateTimeOffset AnalysisTimeUtc,
    DateTimeOffset FromUtc,
    string PrimaryTimeframe,
    string? ConfirmationTimeframe,
    IReadOnlyList<EvidenceTypeGroup> Evidence,
    IReadOnlyList<EvidenceTimeframeGroup> MarketTimeframes,
    IReadOnlyList<EvidenceConflictResult> Conflicts,
    IReadOnlyList<EvidenceCoverage> Coverage);

public sealed record EvidenceIngestionResult(
    int Received,
    int Normalized,
    int Inserted,
    int Deduplicated,
    int Rejected,
    int RelationsCreated,
    int ClustersCreated);

public sealed record EvidencePersistenceResult(
    int Inserted,
    int Deduplicated,
    int RelationsCreated,
    int ClustersCreated);

public sealed record QuarantinedEvidenceItem(
    Guid Id,
    EvidenceType EvidenceType,
    EvidenceSourceType SourceType,
    string SourceKey,
    string? ExternalId,
    string PayloadHash,
    string ErrorCode,
    string ErrorMessage,
    DateTimeOffset? OriginalTimestampUtc,
    string? MetadataJson,
    DateTimeOffset QuarantinedAtUtc);
