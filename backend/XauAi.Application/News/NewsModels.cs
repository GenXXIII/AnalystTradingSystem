namespace XauAi.Application.News;

public enum NewsProviderState
{
    Disabled,
    Available,
    RateLimited,
    AuthenticationFailed,
    Unavailable,
    ConfigurationError
}

public enum NewsRelevanceLevel
{
    Irrelevant,
    Low,
    Medium,
    High,
    VeryHigh
}

public enum NewsFreshness
{
    VeryRecent,
    Recent,
    Older,
    Historical
}

public sealed record NewsProviderRequest(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    string Query,
    string Language,
    int PageSize,
    string? PageToken,
    bool UseArchive);

public sealed record ProviderNewsArticle(
    string? ProviderArticleId,
    string? Title,
    string? Description,
    string? PermittedContent,
    string? OriginalArticleUrl,
    string? SourceName,
    string? SourceUrl,
    IReadOnlyList<string> Authors,
    DateTimeOffset? PublishedAtUtc,
    string? Language,
    IReadOnlyList<string> CountryCodes,
    IReadOnlyList<string> ProviderCategories,
    IReadOnlyList<string> ProviderKeywords,
    string? ImageUrl);

public sealed record NewsProviderPage(
    IReadOnlyList<ProviderNewsArticle> Articles,
    string? NextPageToken,
    int? TotalResults);

public sealed record NewsProviderStatus(
    string Provider,
    NewsProviderState State,
    bool Enabled,
    string Message,
    DateTimeOffset CheckedAtUtc);

public sealed record NewsClassification(
    NewsRelevanceLevel Relevance,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Entities,
    IReadOnlyList<string> Reasons);

public sealed record NormalizedNewsArticle(
    Guid Id,
    string Provider,
    string? ProviderArticleId,
    string Title,
    string? Description,
    string? PermittedContent,
    string CanonicalUrl,
    string CanonicalUrlHash,
    string ContentHash,
    string SourceName,
    string? SourceUrl,
    string? Publisher,
    string? Author,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset CollectedAtUtc,
    string Language,
    IReadOnlyList<string> CountryCodes,
    IReadOnlyList<string> ProviderCategories,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Entities,
    NewsRelevanceLevel Relevance,
    string? ImageUrl,
    IReadOnlyList<string> ClassificationReasons);

public sealed record NewsNormalizationResult(
    NormalizedNewsArticle? Article,
    string? RejectionReason);

public sealed record NewsCollectionRequest(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null);

public sealed record NewsCollectionResult(
    Guid RunId,
    string Provider,
    DateTimeOffset RequestedFromUtc,
    DateTimeOffset RequestedToUtc,
    int RequestsMade,
    int RateLimitResponses,
    int ArticlesReceived,
    int ArticlesInserted,
    int ArticlesSkipped,
    int ArticlesRejected,
    long DurationMilliseconds);

public sealed record NewsArticleQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string? Category = null,
    string? Source = null,
    NewsRelevanceLevel? MinimumRelevance = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 25);

public sealed record NewsArticleResult(
    Guid Id,
    string Provider,
    string? ProviderArticleId,
    string Title,
    string? Description,
    string? Summary,
    string Url,
    string SourceName,
    string? SourceUrl,
    string? Publisher,
    string? Author,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset CollectedAtUtc,
    string Language,
    IReadOnlyList<string> CountryCodes,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Entities,
    NewsRelevanceLevel Relevance,
    NewsFreshness Freshness,
    string? ImageUrl);

public sealed record PagedNewsArticles(
    IReadOnlyList<NewsArticleResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record NewsCollectionStateResult(
    string Provider,
    string Status,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? LastSuccessfulCollectionAtUtc,
    DateTimeOffset? LastRequestedFromUtc,
    DateTimeOffset? LastRequestedToUtc,
    int ConsecutiveFailures,
    int RequestsMade,
    int RateLimitResponses,
    int ArticlesReceived,
    int ArticlesInserted,
    int ArticlesSkipped,
    int ArticlesRejected,
    long DurationMilliseconds,
    string? LastErrorCode,
    string? LastErrorMessage);

public sealed record NewsSystemStatus(
    NewsProviderStatus Provider,
    NewsCollectionStateResult? Collection,
    int StoredArticles);

public sealed record NewsPersistenceResult(int Inserted, int Duplicates);

public sealed record NewsRunMetrics(
    int RequestsMade,
    int RateLimitResponses,
    int ArticlesReceived,
    int ArticlesInserted,
    int ArticlesSkipped,
    int ArticlesRejected,
    long DurationMilliseconds);
