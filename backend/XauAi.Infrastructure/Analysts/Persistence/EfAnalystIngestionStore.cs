using Microsoft.EntityFrameworkCore;
using XauAi.Application.Analysts;
using XauAi.Domain.Analysts;
using XauAi.Domain.Evidence;
using XauAi.Infrastructure.Persistence;
using XauAi.Infrastructure.Evidence.Persistence;

namespace XauAi.Infrastructure.Analysts.Persistence;

internal sealed class EfAnalystIngestionStore(
    XauAiDbContext context,
    AnalystReferenceResolver references,
    AnalystSettings settings) : IAnalystIngestionStore
{
    public async Task<AnalystPersistenceResult> PersistAsync(
        IReadOnlyList<NormalizedAnalystItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return new AnalystPersistenceResult(0, 0, 0, null, null);
        }

        var providerId = await references.ProviderIdAsync(settings.ProviderKey, cancellationToken);
        var instrumentId = await references.InstrumentIdAsync(settings.Symbol, cancellationToken);
        var publicationsInserted = 0;
        var predictionsInserted = 0;
        var duplicates = 0;
        DateTimeOffset? latestPublishedAtUtc = null;
        string? lastExternalId = null;

        foreach (var item in items.OrderBy(value => value.PublishedAtUtc).ThenBy(value => value.ExternalId))
        {
            var source = await ResolveSourceAsync(providerId, item.Source, item.CollectedAtUtc, cancellationToken);
            var analyst = item.Analyst is null
                ? null
                : await ResolveAnalystAsync(source.Id, item.Analyst, item.CollectedAtUtc, cancellationToken);
            var identityVersions = await IdentityVersionsAsync(providerId, item.IdentityHash, cancellationToken);
            var identical = identityVersions.FirstOrDefault(publication => publication.ContentHash == item.ContentHash);
            if (identical is not null)
            {
                duplicates++;
                TrackCursor(item, ref latestPublishedAtUtc, ref lastExternalId);
                continue;
            }

            var contentMatch = await ContentMatchAsync(item.ContentHash, cancellationToken);
            var previousVersion = identityVersions.OrderByDescending(publication => publication.Version).FirstOrDefault();
            var relationshipType = previousVersion is not null ? "Revision"
                : contentMatch is not null ? "Republished"
                : "Original";
            Guid? originalPublicationId = previousVersion is null
                ? null
                : previousVersion.OriginalPublicationId ?? previousVersion.Id;
            var publication = new AnalystPublication
            {
                Id = item.Id,
                DataProviderId = providerId,
                AnalystSourceId = source.Id,
                OriginalPublicationId = originalPublicationId,
                RelatedPublicationId = contentMatch?.Id,
                ExternalId = item.ExternalId,
                IdentityHash = item.IdentityHash,
                Title = item.Title,
                Summary = item.Summary,
                SourceUrl = item.SourceUrl,
                SourceUrlHash = item.SourceUrlHash,
                ContentHash = item.ContentHash,
                Language = item.Language,
                Category = item.Category,
                RelationshipType = relationshipType,
                Version = (previousVersion?.Version ?? 0) + 1,
                PublishedAtUtc = item.PublishedAtUtc,
                CollectedAtUtc = item.CollectedAtUtc,
                ProviderUpdatedAtUtc = item.ProviderUpdatedAtUtc,
                CreatedAtUtc = item.CollectedAtUtc,
                UpdatedAtUtc = item.CollectedAtUtc
            };
            context.AnalystPublications.Add(publication);
            publicationsInserted++;

            if (relationshipType != "Republished")
            {
                foreach (var prediction in item.Predictions.DistinctBy(value => value.ClaimHash, StringComparer.Ordinal))
                {
                    context.EvidenceRecords.Add(EvidenceRecordFactory.AnalystPrediction(
                        prediction.Id,
                        providerId,
                        prediction.Instrument.Equals(settings.Symbol, StringComparison.OrdinalIgnoreCase)
                            ? instrumentId
                            : null,
                        settings.ProviderKey,
                        item,
                        prediction,
                        source.Id,
                        analyst?.Id));
                    context.AnalystStatements.Add(new AnalystStatement
                    {
                        Id = prediction.Id,
                        DataProviderId = providerId,
                        AnalystSourceId = source.Id,
                        AnalystId = analyst?.Id,
                        PublicationId = publication.Id,
                        InstrumentId = prediction.Instrument.Equals(settings.Symbol, StringComparison.OrdinalIgnoreCase)
                            ? instrumentId
                            : null,
                        ExternalId = prediction.ExternalId,
                        AnalystName = analyst?.Name,
                        Title = item.Title,
                        PermittedContent = prediction.ClaimText,
                        SourceUrl = item.SourceUrl,
                        SourceUrlHash = item.SourceUrlHash,
                        InstrumentCode = prediction.Instrument,
                        AssetClass = prediction.AssetClass,
                        Direction = prediction.Direction.ToString(),
                        TargetPrice = prediction.TargetPrice,
                        TargetRangeLow = prediction.TargetRangeLow,
                        TargetRangeHigh = prediction.TargetRangeHigh,
                        TargetCurrency = prediction.TargetCurrency,
                        HorizonValue = prediction.HorizonValue,
                        HorizonUnit = prediction.HorizonUnit.ToString(),
                        TimeHorizon = prediction.TimeHorizon,
                        Confidence = prediction.Confidence,
                        Reason = prediction.Reason,
                        Language = item.Language,
                        Category = prediction.Category,
                        Status = "Published",
                        ClaimHash = prediction.ClaimHash,
                        PublishedAtUtc = item.PublishedAtUtc,
                        FetchedAtUtc = item.CollectedAtUtc,
                        CreatedAtUtc = item.CollectedAtUtc,
                        UpdatedAtUtc = item.CollectedAtUtc
                    });
                    predictionsInserted++;
                }
            }
            else
            {
                duplicates += item.Predictions.Count;
            }

            TrackCursor(item, ref latestPublishedAtUtc, ref lastExternalId);
        }

        await context.SaveChangesAsync(cancellationToken);
        return new AnalystPersistenceResult(
            publicationsInserted,
            predictionsInserted,
            duplicates,
            latestPublishedAtUtc,
            lastExternalId);
    }

    private async Task<AnalystSource> ResolveSourceAsync(
        Guid providerId,
        NormalizedAnalystSource source,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var local = context.AnalystSources.Local.FirstOrDefault(value =>
            value.DataProviderId == providerId && value.IdentityHash == source.IdentityHash);
        var entity = local ?? await context.AnalystSources.SingleOrDefaultAsync(value =>
            value.DataProviderId == providerId && value.IdentityHash == source.IdentityHash,
            cancellationToken);
        if (entity is null)
        {
            entity = new AnalystSource
            {
                Id = Guid.NewGuid(),
                DataProviderId = providerId,
                ExternalId = source.ExternalId,
                IdentityHash = source.IdentityHash,
                Name = source.Name,
                Type = source.Type,
                Website = source.Website,
                CountryCode = source.CountryCode,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.AnalystSources.Add(entity);
        }
        else
        {
            entity.Name = source.Name;
            entity.Type = source.Type;
            entity.Website = source.Website;
            entity.CountryCode = source.CountryCode;
            entity.IsActive = true;
            entity.UpdatedAtUtc = now;
        }

        return entity;
    }

    private async Task<Analyst> ResolveAnalystAsync(
        Guid sourceId,
        NormalizedAnalystIdentity analyst,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var local = context.Analysts.Local.FirstOrDefault(value =>
            value.AnalystSourceId == sourceId && value.IdentityHash == analyst.IdentityHash);
        var entity = local ?? await context.Analysts.SingleOrDefaultAsync(value =>
            value.AnalystSourceId == sourceId && value.IdentityHash == analyst.IdentityHash,
            cancellationToken);
        if (entity is null)
        {
            entity = new Analyst
            {
                Id = Guid.NewGuid(),
                AnalystSourceId = sourceId,
                ExternalId = analyst.ExternalId,
                IdentityHash = analyst.IdentityHash,
                Name = analyst.Name,
                Role = analyst.Role,
                ProfileUrl = analyst.ProfileUrl,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.Analysts.Add(entity);
        }
        else
        {
            entity.Name = analyst.Name;
            entity.Role = analyst.Role;
            entity.ProfileUrl = analyst.ProfileUrl;
            entity.IsActive = true;
            entity.UpdatedAtUtc = now;
        }

        return entity;
    }

    private async Task<IReadOnlyList<AnalystPublication>> IdentityVersionsAsync(
        Guid providerId,
        string identityHash,
        CancellationToken cancellationToken)
    {
        var persisted = await context.AnalystPublications
            .Where(value => value.DataProviderId == providerId && value.IdentityHash == identityHash)
            .ToArrayAsync(cancellationToken);
        return persisted.Concat(context.AnalystPublications.Local.Where(value =>
                value.DataProviderId == providerId && value.IdentityHash == identityHash))
            .DistinctBy(value => value.Id)
            .ToArray();
    }

    private async Task<AnalystPublication?> ContentMatchAsync(
        string contentHash,
        CancellationToken cancellationToken) =>
        context.AnalystPublications.Local.FirstOrDefault(value => value.ContentHash == contentHash)
        ?? await context.AnalystPublications.AsNoTracking()
            .OrderBy(value => value.CreatedAtUtc)
            .FirstOrDefaultAsync(value => value.ContentHash == contentHash, cancellationToken);

    private static void TrackCursor(
        NormalizedAnalystItem item,
        ref DateTimeOffset? latestPublishedAtUtc,
        ref string? lastExternalId)
    {
        if (!latestPublishedAtUtc.HasValue || item.PublishedAtUtc >= latestPublishedAtUtc)
        {
            latestPublishedAtUtc = item.PublishedAtUtc;
            lastExternalId = item.ExternalId;
        }
    }
}
