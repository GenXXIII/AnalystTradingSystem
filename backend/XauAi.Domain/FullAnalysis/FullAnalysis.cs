namespace XauAi.Domain.FullAnalysis;

public sealed class FullAnalysis
{
    public Guid Id { get; set; }
    public Guid InstrumentId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DateTimeOffset AnalysisTimeUtc { get; set; }
    public decimal? CurrentPrice { get; set; }
    public string Decision { get; set; } = string.Empty;
    public decimal? Confidence { get; set; }
    public decimal? Agreement { get; set; }
    public string ConflictsJson { get; set; } = "[]";
    public string KeyEvidenceIdsJson { get; set; } = "[]";
    public string Reasoning { get; set; } = string.Empty;
    public string InvalidationJson { get; set; } = "{}";
    public string Uncertainty { get; set; } = string.Empty;
    public DateTimeOffset? ValidUntilUtc { get; set; }
    public bool FutureAvailable { get; set; }
    public Guid? MasterResultId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string ConfigurationVersion { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = "{}";
    public string WorkspaceResultsJson { get; set; } = "[]";
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
}

public sealed class FullAnalysisLifecycleEvent
{
    public Guid Id { get; set; }
    public Guid FullAnalysisId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal? Price { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
