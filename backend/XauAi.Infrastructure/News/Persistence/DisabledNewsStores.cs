using XauAi.Application.News;

namespace XauAi.Infrastructure.News.Persistence;

internal sealed class DisabledNewsArticleStore : INewsArticleStore
{
    public Task<NewsPersistenceResult> PersistAsync(IReadOnlyList<NormalizedNewsArticle> articles, CancellationToken cancellationToken = default) =>
        throw Disabled();

    private static NewsException Disabled() => new(
        NewsErrorCodes.DatabaseDisabled,
        "News persistence requires the database to be enabled.");
}

internal sealed class DisabledNewsQueryStore : INewsQueryStore
{
    public Task<PagedNewsArticles> QueryAsync(NewsArticleQuery query, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<NewsArticleResult?> GetByIdAsync(Guid id, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    private static NewsException Disabled() => new(
        NewsErrorCodes.DatabaseDisabled,
        "News queries require the database to be enabled.");
}

internal sealed class DisabledNewsCollectionStateStore : INewsCollectionStateStore
{
    public Task<NewsCollectionStateResult?> GetAsync(string providerKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<NewsCollectionStateResult?>(null);

    public Task<Guid> StartAsync(string providerKey, DateTimeOffset fromUtc, DateTimeOffset toUtc, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task CompleteAsync(Guid runId, string providerKey, DateTimeOffset completedAtUtc, NewsRunMetrics metrics, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task FailAsync(Guid runId, string providerKey, DateTimeOffset failedAtUtc, NewsRunMetrics metrics, string errorCode, string safeMessage, CancellationToken cancellationToken = default) =>
        throw Disabled();

    private static NewsException Disabled() => new(
        NewsErrorCodes.DatabaseDisabled,
        "News collection state requires the database to be enabled.");
}
