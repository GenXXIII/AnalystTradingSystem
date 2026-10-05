namespace XauAi.Application.Analysts;

public interface IAnalystDataProvider
{
    Task<AnalystProviderPage> GetLatestAnalystItemsAsync(
        AnalystProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<AnalystProviderPage> GetHistoricalAnalystItemsAsync(
        AnalystProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderAnalystItem?> GetAnalystItemAsync(
        string externalId,
        CancellationToken cancellationToken = default);

    Task<AnalystProviderPage> SearchAsync(
        AnalystProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<AnalystProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface IAnalystRelevanceFilter
{
    AnalystRelevance Classify(ProviderAnalystItem item);
}

public interface IAnalystItemNormalizer
{
    AnalystNormalizationResult Normalize(ProviderAnalystItem item, DateTimeOffset collectedAtUtc);
}

public interface IAnalystIngestionStore
{
    Task<AnalystPersistenceResult> PersistAsync(
        IReadOnlyList<NormalizedAnalystItem> items,
        CancellationToken cancellationToken = default);
}

public interface IAnalystQueryStore
{
    Task<PagedAnalystSources> QuerySourcesAsync(
        AnalystSourceQuery query,
        CancellationToken cancellationToken = default);

    Task<AnalystSourceResult?> GetSourceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedAnalysts> QueryAnalystsAsync(
        AnalystIdentityQuery query,
        CancellationToken cancellationToken = default);

    Task<AnalystIdentityResult?> GetAnalystAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedAnalystPredictions> QueryPredictionsAsync(
        AnalystPredictionQuery query,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task<AnalystPredictionResult?> GetPredictionAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task<(int Sources, int Analysts, int Publications, int Predictions)> CountAsync(
        CancellationToken cancellationToken = default);
}

public interface IAnalystSyncStateStore
{
    Task<AnalystSyncStateResult?> GetAsync(string providerKey, CancellationToken cancellationToken = default);

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
        DateTimeOffset? lastPublishedAtUtc,
        string? lastExternalId,
        AnalystRunMetrics metrics,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset failedAtUtc,
        AnalystRunMetrics metrics,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken = default);
}

public interface IAnalystSynchronizationService
{
    Task<AnalystSyncResult> SynchronizeAsync(
        AnalystSyncRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAnalystQueryService
{
    Task<PagedAnalystSources> GetSourcesAsync(
        AnalystSourceQuery query,
        CancellationToken cancellationToken = default);

    Task<AnalystSourceResult> GetSourceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedAnalysts> GetAnalystsAsync(
        AnalystIdentityQuery query,
        CancellationToken cancellationToken = default);

    Task<AnalystIdentityResult> GetAnalystAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedAnalystPredictions> GetPredictionsAsync(
        AnalystPredictionQuery query,
        CancellationToken cancellationToken = default);

    Task<AnalystPredictionResult> GetPredictionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedAnalystPredictions> GetLatestAsync(
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AnalystSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface IAnalystRetryDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
