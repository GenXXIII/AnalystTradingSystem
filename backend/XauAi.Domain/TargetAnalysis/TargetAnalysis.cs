namespace XauAi.Domain.TargetAnalysis;

public sealed class TargetAnalysis
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public DateTimeOffset AnalysisTimeUtc { get; set; }

    public decimal? CurrentPrice { get; set; }

    public decimal? TargetPrice { get; set; }

    public decimal? InvalidationPrice { get; set; }

    public string DirectionContext { get; set; } = string.Empty;

    public decimal? Confidence { get; set; }

    public DateTimeOffset? ValidUntilUtc { get; set; }

    public Guid? MasterResultId { get; set; }

    public string ReasoningSummary { get; set; } = string.Empty;

    public string Uncertainty { get; set; } = string.Empty;

    public string? NoTargetReason { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = string.Empty;

    public string ConfigurationVersion { get; set; } = string.Empty;

    public string SnapshotJson { get; set; } = "{}";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? EndedAtUtc { get; set; }
}

public sealed class TargetSpecialistResult
{
    public Guid Id { get; set; }

    public Guid TargetAnalysisId { get; set; }

    public string Workspace { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool HasCandidate { get; set; }

    public decimal? CandidateTargetPrice { get; set; }

    public decimal? CandidateInvalidationPrice { get; set; }

    public string DirectionContext { get; set; } = string.Empty;

    public decimal? Confidence { get; set; }

    public bool? RiskAcceptable { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Uncertainty { get; set; } = string.Empty;

    public string OutputJson { get; set; } = "{}";

    public string EvidenceIdsJson { get; set; } = "[]";

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = string.Empty;

    public string ConfigurationVersion { get; set; } = string.Empty;

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    public int? LatencyMilliseconds { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class TargetAnalysisEvidence
{
    public Guid TargetAnalysisId { get; set; }

    public Guid EvidenceRecordId { get; set; }

    public string Role { get; set; } = string.Empty;
}

public sealed class TargetAnalysisLifecycleEvent
{
    public Guid Id { get; set; }

    public Guid TargetAnalysisId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? PreviousStatus { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public decimal? Price { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
