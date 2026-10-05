using XauAi.Application.Evidence;

namespace XauAi.Infrastructure.Evidence.Persistence;

internal sealed class DisabledEvidenceStore : IEvidenceStore
{
    public Task<EvidencePersistenceResult> PersistAsync(IReadOnlyList<NormalizedEvidenceItem> items, CancellationToken cancellationToken = default) => throw Disabled();

    public Task QuarantineAsync(IReadOnlyList<QuarantinedEvidenceItem> items, CancellationToken cancellationToken = default) => throw Disabled();

    public Task<PagedEvidence> QueryAsync(EvidenceStoreQuery query, CancellationToken cancellationToken = default) => throw Disabled();

    public Task<EvidenceResult?> GetAsync(Guid id, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default) => throw Disabled();

    public Task<IReadOnlyList<EvidenceRelationResult>> GetRelationsAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();

    public Task<IReadOnlyList<EvidenceClusterResult>> GetClustersAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();

    public Task<IReadOnlyList<EvidenceResult>> QueryPackAsync(string instrument, DateTimeOffset fromUtc, DateTimeOffset analysisTimeUtc, int maximumItemsPerType, CancellationToken cancellationToken = default) => throw Disabled();

    public Task<PagedEvidenceSources> QuerySourcesAsync(EvidenceSourceQuery query, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default) => throw Disabled();

    private static EvidenceException Disabled() => new(
        EvidenceErrorCodes.DatabaseDisabled,
        "Evidence SQL persistence is disabled.");
}
