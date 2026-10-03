namespace XauAi.Application.MarketData;

public sealed class MarketDataPipelineSettings
{
    public bool SyncEnabled { get; init; }

    public string Symbol { get; init; } = "XAUUSD";

    public IReadOnlyList<MarketTimeframe> Timeframes { get; init; } = Enum.GetValues<MarketTimeframe>();

    public int InitialHistoryDays { get; init; } = 7;

    public int SyncIntervalSeconds { get; init; } = 60;

    public int BatchSize { get; init; } = 1000;

    public int MaxApiLimit { get; init; } = 5000;

    public int MaxQueryRangeDays { get; init; } = 366;

    public int MaxRetries { get; init; } = 2;

    public int RetryBaseDelaySeconds { get; init; } = 2;

    public int MaxGapResults { get; init; } = 1000;

    public bool IncludeFormingCandle { get; init; }
}

public sealed record MarketDataSynchronizationRequest(
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset? FromUtc,
    DateTimeOffset ToUtc,
    bool IncludeFormingCandle = false);

public sealed record MarketDataPipelineResult(
    Guid RunId,
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset RequestedFromUtc,
    DateTimeOffset RequestedToUtc,
    DateTimeOffset? LastStoredCandleOpenTimeUtc,
    int BatchesProcessed,
    int Received,
    int Accepted,
    int Inserted,
    int Updated,
    int Skipped,
    int Rejected,
    int DetectedGaps,
    long DurationMilliseconds);

public sealed record MarketDataSyncProgress(
    int BatchesProcessed,
    int Received,
    int Accepted,
    int Inserted,
    int Updated,
    int Skipped,
    int Rejected);

public sealed record MarketDataSyncStatus(
    string Symbol,
    MarketTimeframe Timeframe,
    string Status,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastRequestedFromUtc,
    DateTimeOffset? LastRequestedToUtc,
    DateTimeOffset? LastStoredCandleOpenTimeUtc,
    int ConsecutiveFailures,
    int DetectedGapCount,
    string? LastErrorCode,
    string? LastErrorMessage);

public sealed record StoredMarketCandle(
    string Symbol,
    string ProviderSymbol,
    MarketTimeframe Timeframe,
    DateTimeOffset OpenTimeUtc,
    DateTimeOffset CloseTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal? TickVolume,
    decimal? RealVolume,
    decimal? Spread,
    bool IsComplete,
    DateTimeOffset FetchedAtUtc);

public sealed record MarketDataQuery(
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit,
    bool CompletedOnly);

public sealed record MarketDataQueryResult(
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit,
    bool CompletedOnly,
    IReadOnlyList<StoredMarketCandle> Candles);

public sealed record MarketDataAvailability(
    string Symbol,
    MarketTimeframe Timeframe,
    long StoredCandles,
    DateTimeOffset? OldestCandleOpenTimeUtc,
    DateTimeOffset? LatestCandleOpenTimeUtc,
    DateTimeOffset? LastCompletedCandleOpenTimeUtc);

public sealed record MarketDataGap(
    DateTimeOffset ExpectedOpenTimeUtc,
    string Classification);

public sealed record MarketDataPipelineStatus(
    MarketDataAvailability Availability,
    MarketDataSyncStatus? Synchronization);

public sealed record CandleValidationOutcome(
    bool IsValid,
    MarketCandleSnapshot? Candle,
    string? ErrorCode);
