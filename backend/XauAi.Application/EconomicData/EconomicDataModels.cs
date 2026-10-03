namespace XauAi.Application.EconomicData;

public enum EconomicProviderState
{
    Disabled,
    Available,
    RateLimited,
    AuthenticationFailed,
    Unavailable,
    ConfigurationError
}

public sealed record EconomicSeriesDefinition(
    string ExternalSeriesId,
    string Category,
    string CountryCode = "US",
    string CurrencyCode = "USD");

public sealed record ProviderEconomicSeries(
    string ExternalSeriesId,
    string Name,
    string? Description,
    string Units,
    string Frequency,
    string SeasonalAdjustment,
    DateOnly? ObservationStartDate,
    DateOnly? ObservationEndDate,
    DateTimeOffset? ProviderUpdatedAtUtc);

public sealed record ProviderEconomicObservation(
    DateOnly ObservationDate,
    decimal? Value,
    string OriginalValue,
    string Status,
    DateOnly? RealtimeStartDate,
    DateOnly? RealtimeEndDate);

public sealed record EconomicObservationProviderRequest(
    string ExternalSeriesId,
    DateOnly From,
    DateOnly To,
    int Offset,
    int Limit,
    bool Descending = false);

public sealed record EconomicObservationProviderPage(
    IReadOnlyList<ProviderEconomicObservation> Observations,
    int TotalCount,
    int Offset,
    int Limit);

public sealed record EconomicProviderStatus(
    string Provider,
    EconomicProviderState State,
    bool Enabled,
    string Message,
    DateTimeOffset CheckedAtUtc);

public sealed record EconomicSeriesResult(
    Guid Id,
    string Provider,
    string ExternalSeriesId,
    string Name,
    string? Description,
    string Units,
    string Frequency,
    string SeasonalAdjustment,
    string CountryCode,
    string CurrencyCode,
    string Category,
    DateOnly? ObservationStartDate,
    DateOnly? ObservationEndDate,
    DateTimeOffset? ProviderUpdatedAtUtc,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EconomicSeriesQuery(
    string? Category = null,
    string? Frequency = null,
    bool ActiveOnly = true);

public sealed record EconomicObservationQuery(
    Guid EconomicSeriesId,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 100);

public sealed record EconomicObservationResult(
    Guid Id,
    Guid EconomicSeriesId,
    string ExternalSeriesId,
    string SeriesName,
    DateOnly ObservationDate,
    decimal? Value,
    string OriginalValue,
    string Status,
    DateOnly? RealtimeStartDate,
    DateOnly? RealtimeEndDate,
    DateTimeOffset FetchedAtUtc,
    int RevisionCount);

public sealed record PagedEconomicObservations(
    IReadOnlyList<EconomicObservationResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record EconomicObservationPersistenceResult(
    int Inserted,
    int Updated,
    int Skipped,
    DateOnly? LatestObservationDate);

public sealed record EconomicSyncRequest(
    string? ExternalSeriesId = null,
    DateOnly? From = null,
    DateOnly? To = null);

public sealed record EconomicSeriesSyncResult(
    Guid RunId,
    string Provider,
    string ExternalSeriesId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int RequestsMade,
    int RateLimitResponses,
    int RecordsReceived,
    int RecordsInserted,
    int RecordsUpdated,
    int RecordsSkipped,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    long DurationMilliseconds);

public sealed record EconomicSyncResult(
    IReadOnlyList<EconomicSeriesSyncResult> Series,
    int Succeeded,
    int Failed,
    int RequestsMade,
    int RecordsReceived,
    int RecordsInserted,
    int RecordsUpdated,
    int RecordsSkipped);

public sealed record EconomicSyncMetrics(
    int RequestsMade,
    int RateLimitResponses,
    int RecordsReceived,
    int RecordsInserted,
    int RecordsUpdated,
    int RecordsSkipped,
    long DurationMilliseconds);

public sealed record EconomicSyncStateResult(
    Guid EconomicSeriesId,
    string ExternalSeriesId,
    string SeriesName,
    string Status,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateOnly? LastObservationDate,
    int ConsecutiveFailures,
    EconomicSyncMetrics Metrics,
    string? LastErrorCode,
    string? LastErrorMessage);

public sealed record EconomicSystemStatus(
    EconomicProviderStatus Provider,
    IReadOnlyList<EconomicSyncStateResult> Series,
    int ConfiguredSeries,
    int StoredSeries,
    int StoredObservations);
