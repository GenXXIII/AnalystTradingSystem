namespace XauAi.Domain.EconomicData;

public sealed class EconomicSyncState
{
    public Guid EconomicSeriesId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? LastSuccessfulSyncAtUtc { get; set; }

    public DateOnly? LastObservationDate { get; set; }

    public int ConsecutiveFailures { get; set; }

    public int RequestsMade { get; set; }

    public int RateLimitResponses { get; set; }

    public int RecordsReceived { get; set; }

    public int RecordsInserted { get; set; }

    public int RecordsUpdated { get; set; }

    public int RecordsSkipped { get; set; }

    public long DurationMilliseconds { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class EconomicSyncRun
{
    public Guid Id { get; set; }

    public Guid EconomicSeriesId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateOnly RequestedFromDate { get; set; }

    public DateOnly RequestedToDate { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public int RequestsMade { get; set; }

    public int RateLimitResponses { get; set; }

    public int RecordsReceived { get; set; }

    public int RecordsInserted { get; set; }

    public int RecordsUpdated { get; set; }

    public int RecordsSkipped { get; set; }

    public long DurationMilliseconds { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}
