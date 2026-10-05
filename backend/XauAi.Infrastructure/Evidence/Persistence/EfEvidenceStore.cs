using Microsoft.EntityFrameworkCore;
using XauAi.Application.Evidence;
using XauAi.Domain.Evidence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.Evidence.Persistence;

internal sealed class EfEvidenceStore(
    XauAiDbContext context,
    EvidenceSettings settings) : IEvidenceStore
{
    public async Task<EvidencePersistenceResult> PersistAsync(
        IReadOnlyList<NormalizedEvidenceItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return new EvidencePersistenceResult(0, 0, 0, 0);
        }

        var unique = items.DistinctBy(item => item.IdentityHash, StringComparer.Ordinal).ToArray();
        var duplicateCount = items.Count - unique.Length;
        var identities = unique.Select(item => item.IdentityHash).ToArray();
        var existingIdentities = await context.EvidenceRecords.AsNoTracking()
            .Where(record => identities.Contains(record.IdentityHash))
            .Select(record => record.IdentityHash)
            .ToArrayAsync(cancellationToken);
        var identitySet = existingIdentities.ToHashSet(StringComparer.Ordinal);
        var pending = unique.Where(item => !identitySet.Contains(item.IdentityHash)).ToArray();
        duplicateCount += unique.Length - pending.Length;
        if (pending.Length == 0)
        {
            return new EvidencePersistenceResult(0, duplicateCount, 0, 0);
        }

        var contentHashes = pending.Select(item => item.ContentHash).Distinct(StringComparer.Ordinal).ToArray();
        var existingContent = await context.EvidenceRecords.AsNoTracking()
            .Where(record => record.ContentHash != null && contentHashes.Contains(record.ContentHash))
            .Select(record => new { record.Id, record.ContentHash, record.SourceKey })
            .ToArrayAsync(cancellationToken);
        var providerKeys = pending.Select(item => item.SourceKey).Distinct(StringComparer.Ordinal).ToArray();
        var providers = await context.DataProviders.AsNoTracking()
            .Where(provider => providerKeys.Contains(provider.Key))
            .ToDictionaryAsync(provider => provider.Key, provider => provider.Id, StringComparer.Ordinal, cancellationToken);
        var symbols = pending.Select(item => item.CanonicalSymbol).Distinct(StringComparer.Ordinal).ToArray();
        var instruments = await context.Instruments.AsNoTracking()
            .Where(instrument => symbols.Contains(instrument.Symbol))
            .ToDictionaryAsync(instrument => instrument.Symbol, instrument => instrument.Id, StringComparer.Ordinal, cancellationToken);
        var timeframeCodes = pending.Where(item => item.Timeframe is not null)
            .Select(item => item.Timeframe!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var timeframes = await context.Timeframes.AsNoTracking()
            .Where(timeframe => timeframeCodes.Contains(timeframe.Code))
            .ToDictionaryAsync(timeframe => timeframe.Code, timeframe => timeframe.Id, StringComparer.Ordinal, cancellationToken);
        var clusterKeys = pending.Where(item => item.ClusterType.HasValue && item.ClusterKey is not null)
            .Select(item => item.ClusterKey!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var existingClusters = clusterKeys.Length == 0
            ? []
            : await context.EvidenceClusters
                .Where(cluster => clusterKeys.Contains(cluster.DeterministicKey))
                .ToArrayAsync(cancellationToken);
        var clustersByKey = existingClusters.ToDictionary(
            cluster => (cluster.ClusterType, cluster.DeterministicKey),
            cluster => cluster);

        var inserted = new List<NormalizedEvidenceItem>(pending.Length);
        var relations = new HashSet<(Guid EvidenceId, Guid RelatedId, string Type)>();
        var relationsCreated = 0;
        var clustersCreated = 0;
        foreach (var item in pending)
        {
            var sameSourceContent = existingContent.FirstOrDefault(value =>
                value.ContentHash == item.ContentHash && value.SourceKey == item.SourceKey);
            if (sameSourceContent is not null)
            {
                duplicateCount++;
                continue;
            }

            context.EvidenceRecords.Add(Map(
                item,
                providers.TryGetValue(item.SourceKey, out var providerId) ? providerId : null,
                instruments.TryGetValue(item.CanonicalSymbol, out var instrumentId) ? instrumentId : null,
                item.Timeframe is not null && timeframes.TryGetValue(item.Timeframe, out var timeframeId)
                    ? timeframeId
                    : null));

            var republished = existingContent.FirstOrDefault(value =>
                value.ContentHash == item.ContentHash && value.SourceKey != item.SourceKey);
            republished ??= inserted
                .Where(value => value.SourceKey != item.SourceKey && value.ContentHash == item.ContentHash)
                .Select(value => new { value.Id, ContentHash = (string?)value.ContentHash, value.SourceKey })
                .FirstOrDefault();
            if (republished is not null
                && relations.Add((item.Id, republished.Id, EvidenceRelationType.Republished.ToString())))
            {
                context.EvidenceRelations.Add(new EvidenceRelation
                {
                    EvidenceId = item.Id,
                    RelatedEvidenceId = republished.Id,
                    RelationType = EvidenceRelationType.Republished.ToString(),
                    Reason = "Exact normalized content fingerprint matched a different source.",
                    CreatedAtUtc = item.CreatedAtUtc
                });
                relationsCreated++;
            }

            foreach (var conflict in inserted.Where(value => IsConflict(item, value)))
            {
                if (!relations.Add((item.Id, conflict.Id, EvidenceRelationType.Contradicts.ToString())))
                {
                    continue;
                }

                context.EvidenceRelations.Add(new EvidenceRelation
                {
                    EvidenceId = item.Id,
                    RelatedEvidenceId = conflict.Id,
                    RelationType = EvidenceRelationType.Contradicts.ToString(),
                    Reason = "Opposite normalized directions were published by different sources within the conflict window.",
                    CreatedAtUtc = item.CreatedAtUtc
                });
                relationsCreated++;
            }

            if (item.ClusterType.HasValue && item.ClusterKey is not null)
            {
                var key = (item.ClusterType.Value.ToString(), item.ClusterKey);
                if (!clustersByKey.TryGetValue(key, out var cluster))
                {
                    cluster = new EvidenceCluster
                    {
                        Id = Guid.NewGuid(),
                        ClusterType = item.ClusterType.Value.ToString(),
                        DeterministicKey = item.ClusterKey,
                        Title = item.Title ?? $"{item.EvidenceType} event",
                        EventTimeUtc = item.EventTimeUtc,
                        CreatedAtUtc = item.CreatedAtUtc,
                        UpdatedAtUtc = item.CreatedAtUtc
                    };
                    context.EvidenceClusters.Add(cluster);
                    clustersByKey[key] = cluster;
                    clustersCreated++;
                }

                context.EvidenceClusterMembers.Add(new EvidenceClusterMember
                {
                    EvidenceClusterId = cluster.Id,
                    EvidenceId = item.Id,
                    Role = "Member",
                    CreatedAtUtc = item.CreatedAtUtc
                });
            }

            inserted.Add(item);
        }

        if (inserted.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return new EvidencePersistenceResult(
            inserted.Count,
            duplicateCount,
            relationsCreated,
            clustersCreated);
    }

    public async Task QuarantineAsync(
        IReadOnlyList<QuarantinedEvidenceItem> items,
        CancellationToken cancellationToken = default)
    {
        context.EvidenceQuarantineRecords.AddRange(items.Select(item => new EvidenceQuarantineRecord
        {
            Id = item.Id,
            EvidenceType = item.EvidenceType.ToString(),
            SourceType = item.SourceType.ToString(),
            SourceKey = item.SourceKey,
            ExternalId = item.ExternalId,
            PayloadHash = item.PayloadHash,
            ErrorCode = item.ErrorCode,
            ErrorMessage = item.ErrorMessage,
            OriginalTimestampUtc = item.OriginalTimestampUtc,
            MetadataJson = item.MetadataJson,
            QuarantinedAtUtc = item.QuarantinedAtUtc
        }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedEvidence> QueryAsync(
        EvidenceStoreQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = ApplyFilters(AvailableAt(query.AsOfUtc), query);
        var total = await source.CountAsync(cancellationToken);
        var rows = await source
            .OrderByDescending(record => record.AvailableAtUtc)
            .ThenByDescending(record => record.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedEvidence(
            rows.Select(Map).ToArray(),
            query.Page,
            query.PageSize,
            total,
            Pages(total, query.PageSize),
            query.AsOfUtc);
    }

    public async Task<EvidenceResult?> GetAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var record = await AvailableAt(asOfUtc)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyList<EvidenceRelationResult>> GetRelationsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.EvidenceRelations.AsNoTracking()
            .Where(relation => relation.EvidenceId == id || relation.RelatedEvidenceId == id)
            .OrderBy(relation => relation.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return rows.Select(relation => new EvidenceRelationResult(
            relation.EvidenceId,
            relation.RelatedEvidenceId,
            Parse(relation.RelationType, EvidenceRelationType.References),
            relation.Reason,
            relation.CreatedAtUtc)).ToArray();
    }

    public async Task<IReadOnlyList<EvidenceClusterResult>> GetClustersAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await (from member in context.EvidenceClusterMembers.AsNoTracking()
               join cluster in context.EvidenceClusters.AsNoTracking()
                   on member.EvidenceClusterId equals cluster.Id
               where member.EvidenceId == id
               orderby cluster.EventTimeUtc
               select new EvidenceClusterResult(
                   cluster.Id,
                   Parse(cluster.ClusterType, EvidenceClusterType.Other),
                   cluster.Title,
                   cluster.EventTimeUtc,
                   member.Role))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<EvidenceResult>> QueryPackAsync(
        string instrument,
        DateTimeOffset fromUtc,
        DateTimeOffset analysisTimeUtc,
        int maximumItemsPerType,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EvidenceResult>();
        foreach (var type in Enum.GetValues<EvidenceType>())
        {
            var typeName = type.ToString();
            var rows = await AvailableAt(analysisTimeUtc)
                .Where(record => record.CanonicalSymbol == instrument
                    && record.EvidenceType == typeName
                    && record.AvailableAtUtc >= fromUtc
                    && record.IsRelevant)
                .OrderByDescending(record => record.AvailableAtUtc)
                .ThenByDescending(record => record.Id)
                .Take(maximumItemsPerType)
                .ToArrayAsync(cancellationToken);
            results.AddRange(rows.Select(Map));
        }

        return results;
    }

    public async Task<PagedEvidenceSources> QuerySourcesAsync(
        EvidenceSourceQuery query,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var source = AvailableAt(asOfUtc);
        if (query.SourceType.HasValue)
        {
            var sourceType = query.SourceType.Value.ToString();
            source = source.Where(record => record.SourceType == sourceType);
        }

        var grouped = source.GroupBy(record => new { record.SourceKey, record.SourceType });
        var total = await grouped.CountAsync(cancellationToken);
        var rows = await grouped
            .Select(group => new
            {
                group.Key.SourceKey,
                group.Key.SourceType,
                Count = group.Count(),
                Latest = group.Max(record => record.AvailableAtUtc)
            })
            .OrderBy(row => row.SourceKey)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedEvidenceSources(
            rows.Select(row => new EvidenceSourceSummary(
                row.SourceKey,
                Parse(row.SourceType, EvidenceSourceType.Other),
                row.Count,
                row.Latest)).ToArray(),
            query.Page,
            query.PageSize,
            total,
            Pages(total, query.PageSize),
            asOfUtc);
    }

    private IQueryable<EvidenceRecord> AvailableAt(DateTimeOffset asOfUtc) =>
        context.EvidenceRecords.AsNoTracking()
            .Where(record => record.AvailableAtUtc <= asOfUtc
                && (!record.ValidFromUtc.HasValue || record.ValidFromUtc <= asOfUtc)
                && (!record.ValidToUtc.HasValue || record.ValidToUtc > asOfUtc));

    private static IQueryable<EvidenceRecord> ApplyFilters(
        IQueryable<EvidenceRecord> source,
        EvidenceStoreQuery query)
    {
        if (query.Instrument is not null) source = source.Where(record => record.CanonicalSymbol == query.Instrument);
        if (query.EvidenceType.HasValue)
        {
            var value = query.EvidenceType.Value.ToString();
            source = source.Where(record => record.EvidenceType == value);
        }

        if (query.SourceType.HasValue)
        {
            var value = query.SourceType.Value.ToString();
            source = source.Where(record => record.SourceType == value);
        }

        if (query.FromUtc.HasValue) source = source.Where(record => record.AvailableAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue) source = source.Where(record => record.AvailableAtUtc <= query.ToUtc.Value);
        if (query.Timeframe is not null) source = source.Where(record => record.TimeframeCode == query.Timeframe);
        if (query.Direction.HasValue)
        {
            var value = query.Direction.Value.ToString();
            source = source.Where(record => record.Direction == value);
        }

        if (query.Importance.HasValue)
        {
            var value = query.Importance.Value.ToString();
            source = source.Where(record => record.Importance == value);
        }

        if (query.RelevantOnly.HasValue) source = source.Where(record => record.IsRelevant == query.RelevantOnly.Value);
        return source;
    }

    private bool IsConflict(NormalizedEvidenceItem first, NormalizedEvidenceItem second) =>
        first.SourceKey != second.SourceKey
        && first.CanonicalSymbol == second.CanonicalSymbol
        && first.Timeframe == second.Timeframe
        && first.Category == second.Category
        && first.Direction is EvidenceDirection.Bullish or EvidenceDirection.Bearish
        && second.Direction is EvidenceDirection.Bullish or EvidenceDirection.Bearish
        && first.Direction != second.Direction
        && Math.Abs((first.AvailableAtUtc - second.AvailableAtUtc).TotalHours) <= settings.ConflictWindowHours;

    private static EvidenceRecord Map(
        NormalizedEvidenceItem item,
        Guid? providerId,
        Guid? instrumentId,
        Guid? timeframeId) => new()
        {
            Id = item.Id,
            DataProviderId = providerId,
            InstrumentId = instrumentId,
            TimeframeId = timeframeId,
            Kind = item.EvidenceType.ToString(),
            EvidenceType = item.EvidenceType.ToString(),
            SourceType = item.SourceType.ToString(),
            SourceKey = item.SourceKey,
            ExternalId = item.ExternalId,
            IdentityHash = item.IdentityHash,
            ContentHash = item.ContentHash,
            CanonicalSymbol = item.CanonicalSymbol,
            OriginalSymbol = item.OriginalSymbol,
            TimeframeCode = item.Timeframe,
            ObservedAtUtc = item.EventTimeUtc,
            AvailableAtUtc = item.AvailableAtUtc,
            PublishedAtUtc = item.PublishedAtUtc,
            CollectedAtUtc = item.CollectedAtUtc,
            ValidFromUtc = item.ValidFromUtc,
            ValidToUtc = item.ValidToUtc,
            Title = item.Title,
            Summary = item.Summary,
            NumericValue = item.Value,
            OriginalValue = item.OriginalValue,
            Unit = item.Unit.ToString(),
            OriginalUnit = item.OriginalUnit,
            Direction = item.Direction.ToString(),
            OriginalDirection = item.OriginalDirection,
            Importance = item.Importance.ToString(),
            OriginalImportance = item.OriginalImportance,
            Category = item.Category.ToString(),
            OriginalCategory = item.OriginalCategory,
            CurrencyCode = item.CurrencyCode,
            OriginalSourceUrl = item.OriginalSourceUrl,
            Quality = item.Quality.ToString(),
            Completeness = item.Completeness.ToString(),
            TimestampQuality = item.TimestampQuality.ToString(),
            SourceReliability = item.SourceReliability.ToString(),
            IsRelevant = item.IsRelevant,
            RelevanceReason = item.RelevanceReason,
            MetadataJson = item.MetadataJson,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.CreatedAtUtc
        };

    private static EvidenceResult Map(EvidenceRecord record) => new(
        record.Id,
        Parse(record.EvidenceType, EvidenceType.Market),
        Parse(record.SourceType, EvidenceSourceType.Other),
        record.SourceKey,
        record.ExternalId,
        record.CanonicalSymbol,
        record.OriginalSymbol,
        record.TimeframeCode,
        record.ObservedAtUtc,
        record.AvailableAtUtc,
        record.PublishedAtUtc,
        record.CollectedAtUtc,
        record.ValidFromUtc,
        record.ValidToUtc,
        record.Title,
        record.Summary,
        record.NumericValue,
        record.OriginalValue,
        Parse(record.Unit, EvidenceUnit.Unknown),
        record.OriginalUnit,
        Parse(record.Direction, EvidenceDirection.Unknown),
        record.OriginalDirection,
        Parse(record.Importance, EvidenceImportance.Unknown),
        record.OriginalImportance,
        Parse(record.Category, EvidenceCategory.Other),
        record.OriginalCategory,
        record.CurrencyCode,
        record.OriginalSourceUrl,
        Parse(record.Quality, EvidenceQuality.Unknown),
        Parse(record.Completeness, EvidenceCompleteness.Unknown),
        Parse(record.TimestampQuality, EvidenceTimestampQuality.Unknown),
        Parse(record.SourceReliability, EvidenceSourceReliability.Unknown),
        record.IsRelevant,
        record.RelevanceReason,
        record.MetadataJson,
        record.CreatedAtUtc,
        record.UpdatedAtUtc);

    private static TEnum Parse<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;

    private static int Pages(int total, int pageSize) =>
        total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
}
