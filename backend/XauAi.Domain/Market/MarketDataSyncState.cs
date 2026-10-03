namespace XauAi.Domain.Market;

public sealed class MarketDataSyncState
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid TimeframeId { get; set; }

    public Guid DataProviderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? LastSuccessfulSyncAtUtc { get; set; }

    public DateTimeOffset? LastRequestedFromUtc { get; set; }

    public DateTimeOffset? LastRequestedToUtc { get; set; }

    public DateTimeOffset? LastStoredCandleOpenTimeUtc { get; set; }

    public int ConsecutiveFailures { get; set; }

    public int DetectedGapCount { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
