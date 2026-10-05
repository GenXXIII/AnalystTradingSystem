namespace XauAi.Application.Analysts;

public enum AnalystDirection
{
    Unknown,
    Bullish,
    Bearish,
    Neutral
}

public enum AnalystHorizonUnit
{
    Unknown,
    Intraday,
    Days,
    Weeks,
    Months,
    Years,
    LongTerm
}

public enum AnalystProviderState
{
    Disabled,
    Available,
    RateLimited,
    AuthenticationFailed,
    Unavailable,
    ConfigurationError
}

public sealed record ProviderAnalystSource(
    string? ExternalId,
    string? Name,
    string? Type,
    string? Website,
    string? CountryCode);

public sealed record ProviderAnalystIdentity(
    string? ExternalId,
    string? Name,
    string? Role,
    string? ProfileUrl);

public sealed record ProviderAnalystClaim(
    string? ExternalId,
    string? Text,
    string? Instrument,
    string? AssetClass,
    string? Direction,
    decimal? TargetPrice,
    decimal? TargetRangeLow,
    decimal? TargetRangeHigh,
    string? TargetCurrency,
    int? HorizonValue,
    string? HorizonUnit,
    string? TimeHorizon,
    decimal? Confidence,
    string? Reason,
    string? Category);

public sealed record ProviderAnalystItem(
    string? ExternalId,
    string? Title,
    string? Summary,
    string? PermittedContent,
    string? SourceUrl,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string? Language,
    string? Category,
    ProviderAnalystSource Source,
    ProviderAnalystIdentity? Analyst,
    IReadOnlyList<ProviderAnalystClaim> Claims);

public sealed record AnalystProviderRequest(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int PageSize,
    string? Cursor = null,
    string? Search = null);

public sealed record AnalystProviderPage(
    IReadOnlyList<ProviderAnalystItem> Items,
    string? NextCursor,
    int? TotalResults);

public sealed record AnalystProviderStatus(
    string Provider,
    AnalystProviderState State,
    bool Enabled,
    string Message,
    DateTimeOffset CheckedAtUtc);

public sealed record AnalystRelevance(
    bool IsRelevant,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Reasons);

public sealed record NormalizedAnalystSource(
    string? ExternalId,
    string IdentityHash,
    string Name,
    string Type,
    string? Website,
    string? CountryCode);

public sealed record NormalizedAnalystIdentity(
    string? ExternalId,
    string IdentityHash,
    string Name,
    string? Role,
    string? ProfileUrl);

public sealed record NormalizedAnalystPrediction(
    Guid Id,
    string? ExternalId,
    string Instrument,
    string AssetClass,
    AnalystDirection Direction,
    string? ClaimText,
    decimal? TargetPrice,
    decimal? TargetRangeLow,
    decimal? TargetRangeHigh,
    string? TargetCurrency,
    int? HorizonValue,
    AnalystHorizonUnit HorizonUnit,
    string? TimeHorizon,
    decimal? Confidence,
    string? Reason,
    string Category,
    string ClaimHash);

public sealed record NormalizedAnalystItem(
    Guid Id,
    string Provider,
    string? ExternalId,
    string IdentityHash,
    string Title,
    string? Summary,
    string? SourceUrl,
    string? SourceUrlHash,
    string ContentHash,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset CollectedAtUtc,
    DateTimeOffset? ProviderUpdatedAtUtc,
    string Language,
    string Category,
    NormalizedAnalystSource Source,
    NormalizedAnalystIdentity? Analyst,
    IReadOnlyList<NormalizedAnalystPrediction> Predictions);

public sealed record AnalystNormalizationResult(
    NormalizedAnalystItem? Item,
    string? RejectionReason);

public sealed record AnalystSyncRequest(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null);

public sealed record AnalystPersistenceResult(
    int PublicationsInserted,
    int PredictionsInserted,
    int Duplicates,
    DateTimeOffset? LatestPublishedAtUtc,
    string? LastExternalId);

public sealed record AnalystRunMetrics(
    int RequestsMade,
    int RateLimitResponses,
    int ItemsReceived,
    int PublicationsInserted,
    int PredictionsInserted,
    int ItemsSkipped,
    int ItemsRejected,
    long DurationMilliseconds);

public sealed record AnalystSyncResult(
    Guid RunId,
    string Provider,
    DateTimeOffset RequestedFromUtc,
    DateTimeOffset RequestedToUtc,
    int RequestsMade,
    int RateLimitResponses,
    int ItemsReceived,
    int PublicationsInserted,
    int PredictionsInserted,
    int ItemsSkipped,
    int ItemsRejected,
    long DurationMilliseconds);

public sealed record AnalystSourceQuery(
    string? Type = null,
    bool ActiveOnly = true,
    int Page = 1,
    int PageSize = 25);

public sealed record AnalystIdentityQuery(
    Guid? SourceId = null,
    bool ActiveOnly = true,
    int Page = 1,
    int PageSize = 25);

public sealed record AnalystPredictionQuery(
    string? Instrument = null,
    AnalystDirection? Direction = null,
    Guid? SourceId = null,
    Guid? AnalystId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    AnalystHorizonUnit? Horizon = null,
    DateTimeOffset? AsOfUtc = null,
    int Page = 1,
    int PageSize = 25);

public sealed record AnalystSourceResult(
    Guid Id,
    string Provider,
    string? ExternalId,
    string Name,
    string Type,
    string? Website,
    string? CountryCode,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AnalystIdentityResult(
    Guid Id,
    Guid SourceId,
    string SourceName,
    string? ExternalId,
    string Name,
    string? Role,
    string? ProfileUrl,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AnalystPredictionResult(
    Guid Id,
    Guid PublicationId,
    Guid SourceId,
    string SourceName,
    Guid? AnalystId,
    string? AnalystName,
    string Instrument,
    string AssetClass,
    AnalystDirection Direction,
    string Title,
    string? Summary,
    string? Claim,
    decimal? TargetPrice,
    decimal? TargetRangeLow,
    decimal? TargetRangeHigh,
    string? TargetCurrency,
    int? HorizonValue,
    AnalystHorizonUnit HorizonUnit,
    string? TimeHorizon,
    decimal? Confidence,
    string? Reason,
    string Category,
    string Status,
    string? SourceUrl,
    string? ExternalId,
    string Language,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset CollectedAtUtc,
    int PublicationVersion,
    Guid? RelatedPublicationId,
    string RelationshipType);

public sealed record PagedAnalystSources(
    IReadOnlyList<AnalystSourceResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record PagedAnalysts(
    IReadOnlyList<AnalystIdentityResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record PagedAnalystPredictions(
    IReadOnlyList<AnalystPredictionResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    DateTimeOffset AsOfUtc);

public sealed record AnalystSyncStateResult(
    string Provider,
    string Status,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastPublishedAtUtc,
    string? LastExternalId,
    int ConsecutiveFailures,
    AnalystRunMetrics Metrics,
    string? LastErrorCode,
    string? LastErrorMessage);

public sealed record AnalystSystemStatus(
    AnalystProviderStatus Provider,
    AnalystSyncStateResult? Synchronization,
    int StoredSources,
    int StoredAnalysts,
    int StoredPublications,
    int StoredPredictions);
