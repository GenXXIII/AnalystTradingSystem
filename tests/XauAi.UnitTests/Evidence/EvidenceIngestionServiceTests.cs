using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.Evidence;

namespace XauAi.UnitTests.Evidence;

public sealed class EvidenceIngestionServiceTests
{
    [Fact]
    public async Task Invalid_records_are_quarantined_while_valid_records_continue_in_bounded_batches()
    {
        var now = DateTimeOffset.Parse("2026-10-04T10:00:00Z");
        var store = new RecordingStore();
        var service = new EvidenceIngestionService(
            new EvidenceNormalizer(),
            store,
            new EvidenceSettings { IngestionBatchSize = 1 },
            new FixedTimeProvider(now),
            NullLogger<EvidenceIngestionService>.Instance);
        var valid = Input("valid", now.AddMinutes(-1));
        var invalid = Input("future", now.AddMinutes(1));

        var result = await service.IngestAsync([valid, invalid]);

        Assert.Equal(2, result.Received);
        Assert.Equal(1, result.Inserted);
        Assert.Equal(1, result.Rejected);
        Assert.Single(store.Persisted);
        var quarantined = Assert.Single(store.Quarantined);
        Assert.Equal("FUTURE_AVAILABILITY", quarantined.ErrorCode);
    }

    private static EvidenceInput Input(string id, DateTimeOffset availableAt) => new()
    {
        EvidenceType = EvidenceType.News,
        SourceType = EvidenceSourceType.NewsProvider,
        SourceKey = "test-news",
        ExternalId = id,
        Instrument = "GOLD",
        EventTime = availableAt,
        AvailableAt = availableAt,
        Title = "Synthetic test-only gold evidence"
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingStore : IEvidenceStore
    {
        public List<NormalizedEvidenceItem> Persisted { get; } = [];

        public List<QuarantinedEvidenceItem> Quarantined { get; } = [];

        public Task<EvidencePersistenceResult> PersistAsync(IReadOnlyList<NormalizedEvidenceItem> items, CancellationToken cancellationToken = default)
        {
            Persisted.AddRange(items);
            return Task.FromResult(new EvidencePersistenceResult(items.Count, 0, 0, 0));
        }

        public Task QuarantineAsync(IReadOnlyList<QuarantinedEvidenceItem> items, CancellationToken cancellationToken = default)
        {
            Quarantined.AddRange(items);
            return Task.CompletedTask;
        }

        public Task<PagedEvidence> QueryAsync(EvidenceStoreQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EvidenceResult?> GetAsync(Guid id, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EvidenceRelationResult>> GetRelationsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EvidenceClusterResult>> GetClustersAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EvidenceResult>> QueryPackAsync(string instrument, DateTimeOffset fromUtc, DateTimeOffset analysisTimeUtc, int maximumItemsPerType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedEvidenceSources> QuerySourcesAsync(EvidenceSourceQuery query, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
