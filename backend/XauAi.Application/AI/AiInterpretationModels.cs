using System.Text.Json.Serialization;

namespace XauAi.Application.AI;

public enum AiSpecialist
{
    News,
    Candle,
    Structure,
    Liquidity,
    Flow,
    Ktr,
    Risk,
    Master
}

public enum AiInterpretationType
{
    NewsEvent,
    MacroData,
    AnalystClaim,
    TechnicalEvidence,
    GeopoliticalRisk,
    EvidenceSynthesis
}

public enum AiInterpretationDirection
{
    Unknown,
    Bullish,
    Bearish,
    Neutral,
    Mixed
}

public enum AiInterpretationImpact
{
    Unknown,
    Low,
    Medium,
    High,
    Critical
}

public enum AiReactionAlignment
{
    Unknown,
    Aligned,
    Opposite,
    Mixed,
    Weak
}

public enum AiCurrentRelevance
{
    Unknown,
    Low,
    Medium,
    High
}

public enum AiInterpretationLifecycle
{
    Current,
    Stale,
    Superseded,
    Invalid
}

public enum AiInterpretationExecutionStatus
{
    Completed,
    Failed
}

public sealed record CreateAiInterpretationRequest(
    string Instrument,
    AiSpecialist Specialist,
    AiInterpretationType InterpretationType,
    string? Timeframe,
    DateTimeOffset? AnalysisTimeUtc,
    int? LookbackHours);

public sealed record AiInterpretationQuery(
    string? Instrument,
    AiSpecialist? Specialist,
    AiInterpretationType? InterpretationType,
    string? Timeframe,
    AiInterpretationLifecycle? Lifecycle,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    bool IncludeHistorical,
    int Page,
    int PageSize);

public sealed record AiEvidenceSelectionQuery(
    string Instrument,
    AiSpecialist Specialist,
    AiInterpretationType InterpretationType,
    string? Timeframe,
    DateTimeOffset FromUtc,
    DateTimeOffset AnalysisTimeUtc,
    int CandidateLimit);

public sealed record AiEvidenceRelation(
    Guid EvidenceId,
    Guid RelatedEvidenceId,
    string RelationType);

public sealed record AiEvidenceCandidate(
    Guid Id,
    string EvidenceType,
    string SourceType,
    string SourceKey,
    string? ExternalId,
    string? ContentHash,
    string Instrument,
    string? Timeframe,
    DateTimeOffset EventTimeUtc,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? Title,
    string? Summary,
    decimal? NumericValue,
    string? OriginalValue,
    string Unit,
    string Direction,
    string Importance,
    string Category,
    string Quality,
    string Completeness,
    bool IsRelevant,
    string? OriginalSourceUrl,
    string? MetadataJson,
    DateTimeOffset UpdatedAtUtc,
    Guid? ClusterId,
    IReadOnlyList<AiEvidenceRelation> Relations);

public sealed record SelectedAiEvidence(
    IReadOnlyList<AiEvidenceCandidate> Items,
    IReadOnlyList<AiEvidenceConflict> Conflicts,
    string EvidenceVersion,
    DateTimeOffset EvidenceUpdatedAtUtc);

public sealed record AiEvidenceConflict(
    Guid FirstEvidenceId,
    Guid SecondEvidenceId,
    string Reason);

public sealed record CompressedAiEvidence(
    string Json,
    IReadOnlyList<Guid> EvidenceIds,
    int OriginalCount,
    int SelectedCount,
    int CharacterCount);

public sealed record AiProviderRequest(
    AiSpecialist Specialist,
    AiInterpretationType InterpretationType,
    string Instrument,
    string? Timeframe,
    DateTimeOffset AnalysisTimeUtc,
    string PromptVersion,
    string EvidenceJson,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record AiProviderCompletion(
    string Json,
    int? InputTokens,
    int? OutputTokens,
    int LatencyMilliseconds,
    string? ProviderRequestId);

public sealed record AiInterpretationStatement(
    string Text,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record AiInterpretationConflict(
    string Description,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record AiInterpretationMeasurement(
    Guid EvidenceId,
    string Name,
    decimal Value,
    string Unit);

public sealed class StructuredAiInterpretation
{
    public required AiInterpretationType InterpretationType { get; init; }

    public required AiInterpretationDirection Direction { get; init; }

    public required AiInterpretationImpact Impact { get; init; }

    public required IReadOnlyList<string> AffectedAssets { get; init; }

    public required string Mechanism { get; init; }

    public required string ExpectedEffect { get; init; }

    public required string ObservedReaction { get; init; }

    public required AiReactionAlignment ReactionAlignment { get; init; }

    public required AiCurrentRelevance CurrentRelevance { get; init; }

    public required decimal Confidence { get; init; }

    public required string Uncertainty { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<Guid> EvidenceIds { get; init; }

    public required IReadOnlyList<AiInterpretationStatement> Facts { get; init; }

    public required IReadOnlyList<AiInterpretationStatement> Interpretations { get; init; }

    public required IReadOnlyList<string> Unknowns { get; init; }

    public required IReadOnlyList<AiInterpretationConflict> Conflicts { get; init; }

    public required IReadOnlyList<AiInterpretationMeasurement> Measurements { get; init; }
}

public sealed record AiInterpretationWriteModel(
    Guid Id,
    string Instrument,
    string? Timeframe,
    AiSpecialist Specialist,
    StructuredAiInterpretation Interpretation,
    string Provider,
    string Model,
    string PromptVersion,
    string EvidenceVersion,
    string CacheKey,
    string InputDigest,
    string OutputJson,
    DateTimeOffset AnalysisTimeUtc,
    DateTimeOffset EvidenceUpdatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int? InputTokens,
    int? OutputTokens,
    int LatencyMilliseconds,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record AiInterpretationFailureWriteModel(
    Guid Id,
    string Instrument,
    string? Timeframe,
    AiSpecialist Specialist,
    AiInterpretationType InterpretationType,
    string Provider,
    string Model,
    string PromptVersion,
    string EvidenceVersion,
    string CacheKey,
    string InputDigest,
    DateTimeOffset AnalysisTimeUtc,
    DateTimeOffset EvidenceUpdatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int LatencyMilliseconds,
    string ErrorCode,
    string ErrorMessage,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record AiInterpretationResult(
    Guid Id,
    string Instrument,
    string? Timeframe,
    AiSpecialist Specialist,
    AiInterpretationType InterpretationType,
    AiInterpretationDirection Direction,
    AiInterpretationImpact Impact,
    IReadOnlyList<string> AffectedAssets,
    string Mechanism,
    string ExpectedEffect,
    string ObservedReaction,
    AiReactionAlignment ReactionAlignment,
    AiCurrentRelevance CurrentRelevance,
    decimal? Confidence,
    string Uncertainty,
    string Summary,
    string Provider,
    string Model,
    string PromptVersion,
    string EvidenceVersion,
    DateTimeOffset AnalysisTimeUtc,
    DateTimeOffset EvidenceUpdatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    AiInterpretationExecutionStatus Status,
    AiInterpretationLifecycle Lifecycle,
    bool CacheHit,
    int? InputTokens,
    int? OutputTokens,
    int? LatencyMilliseconds,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<Guid> EvidenceIds,
    StructuredAiInterpretation? StructuredOutput);

public sealed record PagedAiInterpretations(
    IReadOnlyList<AiInterpretationResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AiSpecialistConfiguration(
    AiSpecialist Specialist,
    bool Enabled,
    string Provider,
    string Adapter,
    bool RequiresApiKey,
    string ApiKey,
    string Model,
    string BaseUrl,
    double Temperature,
    int TimeoutSeconds,
    int MaxOutputTokens,
    int MaxRetries,
    int RequestsPerMinute);

public sealed record AiSpecialistConfigurationView(
    AiSpecialist Specialist,
    bool Enabled,
    string Provider,
    string Adapter,
    string Model,
    string BaseUrl,
    double Temperature,
    int TimeoutSeconds,
    int MaxOutputTokens,
    int MaxRetries,
    int RequestsPerMinute,
    bool HasApiKey);

public sealed class AiInterpretationSettings
{
    public string PromptVersion { get; init; } = "phase11-v2";

    public int DefaultLookbackHours { get; init; } = 168;

    public int MaximumLookbackHours { get; init; } = 17_520;

    public int MaximumEvidenceItems { get; init; } = 60;

    public int MaximumCompressedCharacters { get; init; } = 24_000;

    public int CurrentContextCacheMinutes { get; init; } = 5;

    public int MaximumPageSize { get; init; } = 200;
}

public sealed class AiSpecialistCatalog(IEnumerable<AiSpecialistConfiguration> configurations)
{
    private readonly IReadOnlyDictionary<AiSpecialist, AiSpecialistConfiguration> _configurations =
        configurations.ToDictionary(configuration => configuration.Specialist);

    public AiSpecialistConfiguration Get(AiSpecialist specialist) =>
        _configurations.TryGetValue(specialist, out var configuration)
            ? configuration
            : throw new AiInterpretationException(
                AiInterpretationErrorCodes.SpecialistNotConfigured,
                $"The {specialist} AI specialist is not configured.");

    public IReadOnlyList<AiSpecialistConfigurationView> GetSafeViews() =>
        [.. _configurations.Values.OrderBy(configuration => configuration.Specialist)
            .Select(configuration => new AiSpecialistConfigurationView(
                configuration.Specialist,
                configuration.Enabled,
                configuration.Provider,
                configuration.Adapter,
                configuration.Model,
                configuration.BaseUrl,
                configuration.Temperature,
                configuration.TimeoutSeconds,
                configuration.MaxOutputTokens,
                configuration.MaxRetries,
                configuration.RequestsPerMinute,
                !string.IsNullOrWhiteSpace(configuration.ApiKey)))];
}
