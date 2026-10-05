using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace XauAi.Application.Evidence;

internal sealed class EvidenceIngestionService(
    IEvidenceNormalizer normalizer,
    IEvidenceStore store,
    EvidenceSettings settings,
    TimeProvider timeProvider,
    ILogger<EvidenceIngestionService> logger) : IEvidenceIngestionService
{
    public async Task<EvidenceIngestionResult> IngestAsync(
        IReadOnlyList<EvidenceInput> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0)
        {
            return new EvidenceIngestionResult(0, 0, 0, 0, 0, 0, 0);
        }

        var started = Stopwatch.GetTimestamp();
        var now = timeProvider.GetUtcNow();
        var normalized = 0;
        var inserted = 0;
        var deduplicated = 0;
        var rejected = 0;
        var relations = 0;
        var clusters = 0;
        logger.LogInformation("Evidence normalization started for {RecordsReceived} records", inputs.Count);

        foreach (var batch in inputs.Chunk(settings.IngestionBatchSize))
        {
            var valid = new List<NormalizedEvidenceItem>(batch.Length);
            var invalid = new List<QuarantinedEvidenceItem>();
            foreach (var input in batch)
            {
                var result = normalizer.Normalize(input, now);
                if (result.Item is { } item)
                {
                    valid.Add(item);
                    normalized++;
                    continue;
                }

                invalid.Add(new QuarantinedEvidenceItem(
                    Guid.NewGuid(),
                    input.EvidenceType,
                    input.SourceType,
                    EvidenceNormalization.Collapse(input.SourceKey)?.ToLowerInvariant() ?? "unknown",
                    EvidenceNormalization.Collapse(input.ExternalId),
                    result.PayloadHash,
                    result.ErrorCode ?? EvidenceErrorCodes.InvalidRecord,
                    result.ErrorMessage ?? "The evidence record is invalid.",
                    input.EventTime == default ? null : input.EventTime.ToUniversalTime(),
                    EvidenceNormalization.Collapse(input.MetadataJson),
                    now));
                rejected++;
            }

            if (valid.Count > 0)
            {
                var persisted = await store.PersistAsync(valid, cancellationToken);
                inserted += persisted.Inserted;
                deduplicated += persisted.Deduplicated;
                relations += persisted.RelationsCreated;
                clusters += persisted.ClustersCreated;
            }

            if (invalid.Count > 0)
            {
                await store.QuarantineAsync(invalid, cancellationToken);
            }
        }

        logger.LogInformation(
            "Evidence normalization completed: {RecordsReceived} received, {RecordsNormalized} normalized, {RecordsInserted} inserted, {RecordsDeduplicated} deduplicated, {RecordsRejected} rejected, {RelationsCreated} relations, {ClustersCreated} clusters in {DurationMilliseconds} ms",
            inputs.Count,
            normalized,
            inserted,
            deduplicated,
            rejected,
            relations,
            clusters,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return new EvidenceIngestionResult(
            inputs.Count,
            normalized,
            inserted,
            deduplicated,
            rejected,
            relations,
            clusters);
    }
}
