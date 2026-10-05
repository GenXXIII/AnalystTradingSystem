using XauAi.Application.Analysts;

namespace XauAi.Infrastructure.Analysts.Persistence;

internal sealed class DisabledAnalystIngestionStore : IAnalystIngestionStore
{
    public Task<AnalystPersistenceResult> PersistAsync(
        IReadOnlyList<NormalizedAnalystItem> items,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    private static AnalystException Disabled() => new(
        AnalystErrorCodes.DatabaseDisabled,
        "Analyst-data SQL persistence is disabled.");
}

internal sealed class DisabledAnalystQueryStore : IAnalystQueryStore
{
    public Task<PagedAnalystSources> QuerySourcesAsync(AnalystSourceQuery query, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<AnalystSourceResult?> GetSourceAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<PagedAnalysts> QueryAnalystsAsync(AnalystIdentityQuery query, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<AnalystIdentityResult?> GetAnalystAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<PagedAnalystPredictions> QueryPredictionsAsync(
        AnalystPredictionQuery query,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<AnalystPredictionResult?> GetPredictionAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<(int Sources, int Analysts, int Publications, int Predictions)> CountAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult((0, 0, 0, 0));

    private static AnalystException Disabled() => new(
        AnalystErrorCodes.DatabaseDisabled,
        "Analyst-data SQL persistence is disabled.");
}

internal sealed class DisabledAnalystSyncStateStore : IAnalystSyncStateStore
{
    public Task<AnalystSyncStateResult?> GetAsync(string providerKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<AnalystSyncStateResult?>(null);

    public Task<Guid> StartAsync(
        string providerKey,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task CompleteAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset completedAtUtc,
        DateTimeOffset? lastPublishedAtUtc,
        string? lastExternalId,
        AnalystRunMetrics metrics,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task FailAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset failedAtUtc,
        AnalystRunMetrics metrics,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken = default) =>
        throw Disabled();

    private static AnalystException Disabled() => new(
        AnalystErrorCodes.DatabaseDisabled,
        "Analyst-data SQL persistence is disabled.");
}
