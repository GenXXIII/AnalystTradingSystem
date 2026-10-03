namespace XauAi.Application.News;

public interface INewsProvider
{
    Task<NewsProviderPage> GetNewsAsync(
        NewsProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<NewsProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface INewsRelevanceClassifier
{
    NewsClassification Classify(ProviderNewsArticle article);
}

public interface INewsArticleNormalizer
{
    NewsNormalizationResult Normalize(ProviderNewsArticle article, DateTimeOffset collectedAtUtc);
}

public interface INewsArticleStore
{
    Task<NewsPersistenceResult> PersistAsync(
        IReadOnlyList<NormalizedNewsArticle> articles,
        CancellationToken cancellationToken = default);
}

public interface INewsQueryStore
{
    Task<PagedNewsArticles> QueryAsync(
        NewsArticleQuery query,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<NewsArticleResult?> GetByIdAsync(
        Guid id,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface INewsCollectionStateStore
{
    Task<NewsCollectionStateResult?> GetAsync(
        string providerKey,
        CancellationToken cancellationToken = default);

    Task<Guid> StartAsync(
        string providerKey,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset completedAtUtc,
        NewsRunMetrics metrics,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset failedAtUtc,
        NewsRunMetrics metrics,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken = default);
}

public interface INewsCollectionService
{
    Task<NewsCollectionResult> CollectAsync(
        NewsCollectionRequest request,
        CancellationToken cancellationToken = default);
}

public interface INewsQueryService
{
    Task<PagedNewsArticles> QueryAsync(
        NewsArticleQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedNewsArticles> GetLatestAsync(
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedNewsArticles> GetRelevantAsync(
        NewsRelevanceLevel minimumRelevance,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<NewsArticleResult> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<NewsSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface INewsRetryDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
