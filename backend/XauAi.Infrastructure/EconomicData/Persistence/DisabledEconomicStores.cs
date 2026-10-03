using XauAi.Application.EconomicData;

namespace XauAi.Infrastructure.EconomicData.Persistence;

internal sealed class DisabledEconomicSeriesStore : IEconomicSeriesStore
{
    public Task<EconomicSeriesResult> UpsertAsync(ProviderEconomicSeries metadata, EconomicSeriesDefinition definition, string providerKey, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<IReadOnlyList<EconomicSeriesResult>> QueryAsync(EconomicSeriesQuery query, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<EconomicSeriesResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<EconomicSeriesResult?> GetByExternalIdAsync(string providerKey, string externalSeriesId, CancellationToken cancellationToken = default) => throw Disabled();
    private static EconomicDataException Disabled() => new(EconomicDataErrorCodes.DatabaseDisabled, "Economic-data persistence requires the database to be enabled.");
}

internal sealed class DisabledEconomicObservationStore : IEconomicObservationStore
{
    public Task<EconomicObservationPersistenceResult> PersistAsync(Guid economicSeriesId, IReadOnlyList<ProviderEconomicObservation> observations, DateTimeOffset fetchedAtUtc, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<PagedEconomicObservations> QueryAsync(EconomicObservationQuery query, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<EconomicObservationResult?> GetLatestAsync(Guid economicSeriesId, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<IReadOnlyList<EconomicObservationResult>> GetLatestForAllAsync(CancellationToken cancellationToken = default) => throw Disabled();
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    private static EconomicDataException Disabled() => new(EconomicDataErrorCodes.DatabaseDisabled, "Economic-data persistence requires the database to be enabled.");
}

internal sealed class DisabledEconomicSyncStateStore : IEconomicSyncStateStore
{
    public Task<EconomicSyncStateResult?> GetAsync(Guid economicSeriesId, CancellationToken cancellationToken = default) => Task.FromResult<EconomicSyncStateResult?>(null);
    public Task<IReadOnlyList<EconomicSyncStateResult>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EconomicSyncStateResult>>([]);
    public Task<Guid> StartAsync(Guid economicSeriesId, DateOnly from, DateOnly to, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default) => throw Disabled();
    public Task CompleteAsync(Guid runId, Guid economicSeriesId, DateTimeOffset completedAtUtc, DateOnly? lastObservationDate, EconomicSyncMetrics metrics, CancellationToken cancellationToken = default) => throw Disabled();
    public Task FailAsync(Guid runId, Guid economicSeriesId, DateTimeOffset failedAtUtc, EconomicSyncMetrics metrics, string errorCode, string safeMessage, CancellationToken cancellationToken = default) => throw Disabled();
    private static EconomicDataException Disabled() => new(EconomicDataErrorCodes.DatabaseDisabled, "Economic-data synchronization state requires the database to be enabled.");
}
