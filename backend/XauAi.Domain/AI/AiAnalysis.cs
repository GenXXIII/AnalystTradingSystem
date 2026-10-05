namespace XauAi.Domain.AI;

public sealed class AiAnalysis
{
    public Guid Id { get; set; }

    public Guid? InstrumentId { get; set; }

    public string AnalysisType { get; set; } = string.Empty;

    public string Specialist { get; set; } = string.Empty;

    public string? Timeframe { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public string PromptIdentifier { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = string.Empty;

    public string AnalysisVersion { get; set; } = string.Empty;

    public string ApplicationVersion { get; set; } = string.Empty;

    public string? InputDigest { get; set; }

    public string CacheKey { get; set; } = string.Empty;

    public string EvidenceVersion { get; set; } = string.Empty;

    public DateTimeOffset AnalysisTimeUtc { get; set; }

    public DateTimeOffset EvidenceUpdatedAtUtc { get; set; }

    public string Direction { get; set; } = string.Empty;

    public string Impact { get; set; } = string.Empty;

    public string AffectedAssetsJson { get; set; } = "[]";

    public string Mechanism { get; set; } = string.Empty;

    public string ExpectedEffect { get; set; } = string.Empty;

    public string ObservedReaction { get; set; } = string.Empty;

    public string ReactionAlignment { get; set; } = string.Empty;

    public string CurrentRelevance { get; set; } = string.Empty;

    public decimal? Confidence { get; set; }

    public string Uncertainty { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? Output { get; set; }

    public string Status { get; set; } = string.Empty;

    public string LifecycleStatus { get; set; } = string.Empty;

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    public decimal? CostUsd { get; set; }

    public int? LatencyMilliseconds { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class AiAnalysisEvidence
{
    public Guid AiAnalysisId { get; set; }

    public Guid EvidenceRecordId { get; set; }

    public string Role { get; set; } = string.Empty;
}
