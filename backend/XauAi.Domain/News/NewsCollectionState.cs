namespace XauAi.Domain.News;

public sealed class NewsCollectionState
{
    public Guid DataProviderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? LastSuccessfulCollectionAtUtc { get; set; }

    public DateTimeOffset? LastRequestedFromUtc { get; set; }

    public DateTimeOffset? LastRequestedToUtc { get; set; }

    public int ConsecutiveFailures { get; set; }

    public int RequestsMade { get; set; }

    public int RateLimitResponses { get; set; }

    public int ArticlesReceived { get; set; }

    public int ArticlesInserted { get; set; }

    public int ArticlesSkipped { get; set; }

    public int ArticlesRejected { get; set; }

    public long DurationMilliseconds { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class NewsCollectionRun
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset RequestedFromUtc { get; set; }

    public DateTimeOffset RequestedToUtc { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public int RequestsMade { get; set; }

    public int RateLimitResponses { get; set; }

    public int ArticlesReceived { get; set; }

    public int ArticlesInserted { get; set; }

    public int ArticlesSkipped { get; set; }

    public int ArticlesRejected { get; set; }

    public long DurationMilliseconds { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}
