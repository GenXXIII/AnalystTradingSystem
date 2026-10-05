namespace XauAi.Application.Evidence;

public interface IEvidenceNormalizer
{
    EvidenceNormalizationResult Normalize(EvidenceInput input, DateTimeOffset normalizedAtUtc);
}

public interface IEvidenceStore
{
    Task<EvidencePersistenceResult> PersistAsync(
        IReadOnlyList<NormalizedEvidenceItem> items,
        CancellationToken cancellationToken = default);

    Task QuarantineAsync(
        IReadOnlyList<QuarantinedEvidenceItem> items,
        CancellationToken cancellationToken = default);

    Task<PagedEvidence> QueryAsync(
        EvidenceStoreQuery query,
        CancellationToken cancellationToken = default);

    Task<EvidenceResult?> GetAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceRelationResult>> GetRelationsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceClusterResult>> GetClustersAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceResult>> QueryPackAsync(
        string instrument,
        DateTimeOffset fromUtc,
        DateTimeOffset analysisTimeUtc,
        int maximumItemsPerType,
        CancellationToken cancellationToken = default);

    Task<PagedEvidenceSources> QuerySourcesAsync(
        EvidenceSourceQuery query,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}

public interface IEvidenceIngestionService
{
    Task<EvidenceIngestionResult> IngestAsync(
        IReadOnlyList<EvidenceInput> inputs,
        CancellationToken cancellationToken = default);
}

public interface IEvidenceQueryService
{
    Task<PagedEvidence> GetEvidenceAsync(
        EvidenceQuery query,
        CancellationToken cancellationToken = default);

    Task<EvidenceDetailResult> GetEvidenceAsync(
        Guid id,
        DateTimeOffset? asOfUtc,
        CancellationToken cancellationToken = default);

    Task<EvidencePack> GetPackAsync(
        EvidencePackRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceConflictResult>> GetConflictsAsync(
        EvidenceConflictQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedEvidenceSources> GetSourcesAsync(
        EvidenceSourceQuery query,
        CancellationToken cancellationToken = default);
}
