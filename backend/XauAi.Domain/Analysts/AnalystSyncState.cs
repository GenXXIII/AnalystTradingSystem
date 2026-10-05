namespace XauAi.Domain.Analysts;

public sealed class AnalystSyncState
{
    public Guid DataProviderId { get; set; }

    public string Status { get; set; } = "NeverRun";

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? LastSuccessfulSyncAtUtc { get; set; }

    public DateTimeOffset? LastPublishedAtUtc { get; set; }

    public string? LastExternalId { get; set; }

    public int ConsecutiveFailures { get; set; }

    public int RequestsMade { get; set; }

    public int RateLimitResponses { get; set; }

    public int ItemsReceived { get; set; }

    public int PublicationsInserted { get; set; }

    public int PredictionsInserted { get; set; }

    public int ItemsSkipped { get; set; }

    public int ItemsRejected { get; set; }

    public long DurationMilliseconds { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class AnalystSyncRun
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public string Status { get; set; } = "Running";

    public DateTimeOffset RequestedFromUtc { get; set; }

    public DateTimeOffset RequestedToUtc { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public int RequestsMade { get; set; }

    public int RateLimitResponses { get; set; }

    public int ItemsReceived { get; set; }

    public int PublicationsInserted { get; set; }

    public int PredictionsInserted { get; set; }

    public int ItemsSkipped { get; set; }

    public int ItemsRejected { get; set; }

    public long DurationMilliseconds { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}
