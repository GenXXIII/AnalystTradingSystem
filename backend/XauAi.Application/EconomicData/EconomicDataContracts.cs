namespace XauAi.Application.EconomicData;

public interface IEconomicDataProvider
{
    Task<ProviderEconomicSeries> GetSeriesMetadataAsync(
        string externalSeriesId,
        CancellationToken cancellationToken = default);

    Task<EconomicObservationProviderPage> GetObservationsAsync(
        EconomicObservationProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderEconomicObservation?> GetLatestObservationAsync(
        string externalSeriesId,
        CancellationToken cancellationToken = default);

    Task<EconomicObservationProviderPage> GetObservationsSinceAsync(
        string externalSeriesId,
        DateOnly sinceExclusive,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    Task<EconomicProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface IEconomicSeriesStore
{
    Task<EconomicSeriesResult> UpsertAsync(
        ProviderEconomicSeries metadata,
        EconomicSeriesDefinition definition,
        string providerKey,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EconomicSeriesResult>> QueryAsync(
        EconomicSeriesQuery query,
        CancellationToken cancellationToken = default);

    Task<EconomicSeriesResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EconomicSeriesResult?> GetByExternalIdAsync(
        string providerKey,
        string externalSeriesId,
        CancellationToken cancellationToken = default);
}

public interface IEconomicObservationStore
{
    Task<EconomicObservationPersistenceResult> PersistAsync(
        Guid economicSeriesId,
        IReadOnlyList<ProviderEconomicObservation> observations,
        DateTimeOffset fetchedAtUtc,
        CancellationToken cancellationToken = default);

    Task<PagedEconomicObservations> QueryAsync(
        EconomicObservationQuery query,
        CancellationToken cancellationToken = default);

    Task<EconomicObservationResult?> GetLatestAsync(
        Guid economicSeriesId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EconomicObservationResult>> GetLatestForAllAsync(
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IEconomicSyncStateStore
{
    Task<EconomicSyncStateResult?> GetAsync(Guid economicSeriesId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EconomicSyncStateResult>> ListAsync(CancellationToken cancellationToken = default);

    Task<Guid> StartAsync(
        Guid economicSeriesId,
        DateOnly from,
        DateOnly to,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid runId,
        Guid economicSeriesId,
        DateTimeOffset completedAtUtc,
        DateOnly? lastObservationDate,
        EconomicSyncMetrics metrics,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        Guid runId,
        Guid economicSeriesId,
        DateTimeOffset failedAtUtc,
        EconomicSyncMetrics metrics,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken = default);
}

public interface IEconomicDataSynchronizationService
{
    Task<EconomicSyncResult> SynchronizeAsync(
        EconomicSyncRequest request,
        CancellationToken cancellationToken = default);
}

public interface IEconomicDataQueryService
{
    Task<IReadOnlyList<EconomicSeriesResult>> GetSeriesAsync(
        EconomicSeriesQuery query,
        CancellationToken cancellationToken = default);

    Task<EconomicSeriesResult> GetSeriesByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedEconomicObservations> GetObservationsAsync(
        EconomicObservationQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EconomicObservationResult>> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<EconomicSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface IEconomicRetryDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
