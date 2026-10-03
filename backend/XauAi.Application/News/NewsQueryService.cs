namespace XauAi.Application.News;

internal sealed class NewsQueryService(
    INewsProvider provider,
    INewsQueryStore queryStore,
    INewsCollectionStateStore stateStore,
    NewsSettings settings,
    TimeProvider timeProvider) : INewsQueryService
{
    public Task<PagedNewsArticles> QueryAsync(
        NewsArticleQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);
        return queryStore.QueryAsync(query, timeProvider.GetUtcNow(), cancellationToken);
    }

    public Task<PagedNewsArticles> GetLatestAsync(
        int pageSize,
        CancellationToken cancellationToken = default) =>
        QueryAsync(
            new NewsArticleQuery(
                FromUtc: timeProvider.GetUtcNow().AddHours(-24),
                MinimumRelevance: settings.MinimumRelevance,
                PageSize: pageSize),
            cancellationToken);

    public Task<PagedNewsArticles> GetRelevantAsync(
        NewsRelevanceLevel minimumRelevance,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        QueryAsync(
            new NewsArticleQuery(
                MinimumRelevance: minimumRelevance,
                Page: page,
                PageSize: pageSize),
            cancellationToken);

    public async Task<NewsArticleResult> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await queryStore.GetByIdAsync(id, timeProvider.GetUtcNow(), cancellationToken)
        ?? throw new NewsException(NewsErrorCodes.NotFound, "The requested news article was not found.");

    public async Task<NewsSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        new(
            await provider.GetStatusAsync(cancellationToken),
            await stateStore.GetAsync(settings.ProviderKey, cancellationToken),
            await queryStore.CountAsync(cancellationToken));

    private void Validate(NewsArticleQuery query)
    {
        if (query.Page < 1)
        {
            throw new NewsException(NewsErrorCodes.InvalidRequest, "The news page must be at least 1.");
        }

        if (query.PageSize is < 1 || query.PageSize > settings.MaximumPageSize)
        {
            throw new NewsException(
                NewsErrorCodes.InvalidRequest,
                $"The news page size must be between 1 and {settings.MaximumPageSize}.");
        }

        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc >= query.ToUtc)
        {
            throw new NewsException(NewsErrorCodes.InvalidRequest, "The news query start must be before the end.");
        }

        if (query.Search?.Length > 200 || query.Category?.Length > 64 || query.Source?.Length > 300)
        {
            throw new NewsException(NewsErrorCodes.InvalidRequest, "A news query filter exceeds its maximum length.");
        }
    }
}
