namespace XauAi.Domain.Market;

public sealed class MarketDataSyncRun
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid TimeframeId { get; set; }

    public Guid DataProviderId { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset RequestedFromUtc { get; set; }

    public DateTimeOffset RequestedToUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public int BatchesProcessed { get; set; }

    public int RecordsReceived { get; set; }

    public int RecordsAccepted { get; set; }

    public int RecordsInserted { get; set; }

    public int RecordsUpdated { get; set; }

    public int RecordsSkipped { get; set; }

    public int RecordsRejected { get; set; }

    public int DetectedGapCount { get; set; }

    public long DurationMilliseconds { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}
