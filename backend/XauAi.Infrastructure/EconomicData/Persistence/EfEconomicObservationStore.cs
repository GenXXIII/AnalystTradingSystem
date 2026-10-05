using Microsoft.EntityFrameworkCore;
using XauAi.Application.EconomicData;
using XauAi.Domain.EconomicData;
using XauAi.Domain.Evidence;
using XauAi.Infrastructure.Persistence;
using XauAi.Infrastructure.Evidence.Persistence;

namespace XauAi.Infrastructure.EconomicData.Persistence;

internal sealed class EfEconomicObservationStore(XauAiDbContext context) : IEconomicObservationStore
{
    public async Task<EconomicObservationPersistenceResult> PersistAsync(
        Guid economicSeriesId,
        IReadOnlyList<ProviderEconomicObservation> observations,
        DateTimeOffset fetchedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (observations.Count == 0)
        {
            return new EconomicObservationPersistenceResult(0, 0, 0, null);
        }

        var unique = observations
            .GroupBy(observation => observation.ObservationDate)
            .Select(group => group.Last())
            .OrderBy(observation => observation.ObservationDate)
            .ToArray();
        var series = await context.EconomicSeries.AsNoTracking()
            .SingleAsync(value => value.Id == economicSeriesId, cancellationToken);
        var providerKey = await context.DataProviders.AsNoTracking()
            .Where(provider => provider.Id == series.DataProviderId)
            .Select(provider => provider.Key)
            .SingleAsync(cancellationToken);
        var instrumentId = await context.Instruments.AsNoTracking()
            .Where(instrument => instrument.Symbol == "XAUUSD" && instrument.IsActive)
            .Select(instrument => (Guid?)instrument.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var dates = unique.Select(observation => observation.ObservationDate).ToArray();
        var existing = await context.EconomicObservations
            .Where(observation => observation.EconomicSeriesId == economicSeriesId
                && dates.Contains(observation.ObservationDate))
            .ToDictionaryAsync(observation => observation.ObservationDate, cancellationToken);
        var inserted = 0;
        var updated = 0;
        var skipped = observations.Count - unique.Length;

        foreach (var incoming in unique)
        {
            if (!existing.TryGetValue(incoming.ObservationDate, out var current))
            {
                var id = Guid.NewGuid();
                context.EvidenceRecords.Add(EvidenceRecordFactory.EconomicObservation(
                    id,
                    instrumentId,
                    series,
                    providerKey,
                    incoming,
                    fetchedAtUtc));
                context.EconomicObservations.Add(new EconomicObservation
                {
                    Id = id,
                    EconomicSeriesId = economicSeriesId,
                    ObservationDate = incoming.ObservationDate,
                    Value = incoming.Value,
                    OriginalValue = incoming.OriginalValue,
                    Status = incoming.Status,
                    RealtimeStartDate = incoming.RealtimeStartDate,
                    RealtimeEndDate = incoming.RealtimeEndDate,
                    FetchedAtUtc = fetchedAtUtc,
                    CreatedAtUtc = fetchedAtUtc,
                    UpdatedAtUtc = fetchedAtUtc
                });
                inserted++;
                continue;
            }

            if (SameValue(current, incoming))
            {
                skipped++;
                continue;
            }

            context.EconomicObservationRevisions.Add(new EconomicObservationRevision
            {
                Id = Guid.NewGuid(),
                EconomicObservationId = current.Id,
                Value = current.Value,
                OriginalValue = current.OriginalValue,
                Status = current.Status,
                RealtimeStartDate = current.RealtimeStartDate,
                RealtimeEndDate = current.RealtimeEndDate,
                FetchedAtUtc = current.FetchedAtUtc,
                SupersededAtUtc = fetchedAtUtc
            });
            current.Value = incoming.Value;
            current.OriginalValue = incoming.OriginalValue;
            current.Status = incoming.Status;
            current.RealtimeStartDate = incoming.RealtimeStartDate;
            current.RealtimeEndDate = incoming.RealtimeEndDate;
            current.FetchedAtUtc = fetchedAtUtc;
            current.UpdatedAtUtc = fetchedAtUtc;
            var externalId = $"{series.ExternalSeriesId}:{incoming.ObservationDate:yyyy-MM-dd}";
            var priorEvidence = await context.EvidenceRecords
                .Where(record => record.SourceKey == providerKey
                    && record.ExternalId == externalId
                    && record.ValidToUtc == null)
                .OrderByDescending(record => record.AvailableAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var revisionEvidence = EvidenceRecordFactory.EconomicObservation(
                Guid.NewGuid(),
                instrumentId,
                series,
                providerKey,
                incoming,
                fetchedAtUtc);
            context.EvidenceRecords.Add(revisionEvidence);
            if (priorEvidence is not null)
            {
                priorEvidence.ValidToUtc = fetchedAtUtc.ToUniversalTime();
                priorEvidence.UpdatedAtUtc = fetchedAtUtc.ToUniversalTime();
                context.EvidenceRelations.Add(new EvidenceRelation
                {
                    EvidenceId = revisionEvidence.Id,
                    RelatedEvidenceId = priorEvidence.Id,
                    RelationType = "Updates",
                    Reason = "The provider supplied a revised value for the same economic observation.",
                    CreatedAtUtc = fetchedAtUtc.ToUniversalTime()
                });
            }

            updated++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new EconomicObservationPersistenceResult(
            inserted,
            updated,
            skipped,
            unique.Max(observation => (DateOnly?)observation.ObservationDate));
    }

    public async Task<PagedEconomicObservations> QueryAsync(
        EconomicObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = context.EconomicObservations.AsNoTracking()
            .Where(observation => observation.EconomicSeriesId == query.EconomicSeriesId);
        if (query.From.HasValue)
        {
            source = source.Where(observation => observation.ObservationDate >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            source = source.Where(observation => observation.ObservationDate <= query.To.Value);
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await Project(source
            .OrderByDescending(observation => observation.ObservationDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize))
            .ToArrayAsync(cancellationToken);
        return new PagedEconomicObservations(
            items,
            query.Page,
            query.PageSize,
            total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public Task<EconomicObservationResult?> GetLatestAsync(
        Guid economicSeriesId,
        CancellationToken cancellationToken = default) =>
        Project(context.EconomicObservations.AsNoTracking()
                .Where(observation => observation.EconomicSeriesId == economicSeriesId)
                .OrderByDescending(observation => observation.ObservationDate)
                .Take(1))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<EconomicObservationResult>> GetLatestForAllAsync(
        CancellationToken cancellationToken = default)
    {
        var source = context.EconomicObservations.AsNoTracking()
            .Where(observation => !context.EconomicObservations.Any(other =>
                other.EconomicSeriesId == observation.EconomicSeriesId
                && other.ObservationDate > observation.ObservationDate));
        return await Project(source.OrderBy(observation => observation.EconomicSeriesId))
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.EconomicObservations.CountAsync(cancellationToken);

    private IQueryable<EconomicObservationResult> Project(IQueryable<EconomicObservation> source) =>
        source.Join(
            context.EconomicSeries.AsNoTracking(),
            observation => observation.EconomicSeriesId,
            series => series.Id,
            (observation, series) => new EconomicObservationResult(
                observation.Id,
                observation.EconomicSeriesId,
                series.ExternalSeriesId,
                series.Name,
                observation.ObservationDate,
                observation.Value,
                observation.OriginalValue,
                observation.Status,
                observation.RealtimeStartDate,
                observation.RealtimeEndDate,
                observation.FetchedAtUtc,
                context.EconomicObservationRevisions.Count(revision =>
                    revision.EconomicObservationId == observation.Id)));

    private static bool SameValue(
        EconomicObservation current,
        ProviderEconomicObservation incoming) =>
        current.Value == incoming.Value
        && current.OriginalValue == incoming.OriginalValue
        && current.Status == incoming.Status
        && current.RealtimeStartDate == incoming.RealtimeStartDate
        && current.RealtimeEndDate == incoming.RealtimeEndDate;
}
