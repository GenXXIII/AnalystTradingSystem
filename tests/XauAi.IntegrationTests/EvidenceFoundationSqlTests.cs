using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.Evidence;
using XauAi.Infrastructure.Evidence.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class EvidenceFoundationSqlTests
{
    [SqlServerFact]
    public async Task Multi_domain_evidence_is_normalized_deduplicated_clustered_and_look_ahead_safe()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var databaseName = $"XauAiEvidenceTests_{Guid.NewGuid():N}";
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection) { InitialCatalog = databaseName };
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;
        var now = DateTimeOffset.Parse("2026-10-04T10:30:00Z");
        var analysisTime = DateTimeOffset.Parse("2026-10-04T10:00:00Z");
        var settings = new EvidenceSettings
        {
            IngestionBatchSize = 3,
            MaximumPackItemsPerType = 100,
            ConflictWindowHours = 24,
            MaximumConflicts = 20
        };
        var inputs = Inputs(analysisTime);

        try
        {
            await using (var migrationContext = new XauAiDbContext(options))
            {
                await migrationContext.Database.MigrateAsync();
            }

            await using (var ingestContext = new XauAiDbContext(options))
            {
                var service = IngestionService(ingestContext, settings, now);
                var first = await service.IngestAsync(inputs);
                var replay = await service.IngestAsync(inputs);

                Assert.Equal(8, first.Received);
                Assert.Equal(7, first.Normalized);
                Assert.Equal(7, first.Inserted);
                Assert.Equal(1, first.Rejected);
                Assert.Equal(2, first.RelationsCreated);
                Assert.Equal(1, first.ClustersCreated);
                Assert.Equal(0, replay.Inserted);
                Assert.Equal(7, replay.Deduplicated);
                Assert.Equal(1, replay.Rejected);
            }

            await using (var queryContext = new XauAiDbContext(options))
            {
                var store = new EfEvidenceStore(queryContext, settings);
                var query = new EvidenceQueryService(
                    store,
                    settings,
                    new FixedTimeProvider(now),
                    NullLogger<EvidenceQueryService>.Instance);
                var pack = await query.GetPackAsync(new EvidencePackRequest(
                    "XAU/USD",
                    "M15",
                    null,
                    analysisTime,
                    1));

                Assert.Equal("XAUUSD", pack.Instrument);
                Assert.Equal("M5", pack.ConfirmationTimeframe);
                Assert.Equal(6, pack.Evidence.Sum(group => group.Items.Count));
                Assert.Empty(pack.Evidence.Single(group => group.EvidenceType == EvidenceType.OrderFlow).Items);
                Assert.Equal(EvidenceCoverageState.Unavailable,
                    pack.Coverage.Single(value => value.EvidenceType == EvidenceType.OrderFlow).State);
                Assert.DoesNotContain(pack.Evidence.SelectMany(group => group.Items),
                    item => item.ExternalId == "future-news");
                Assert.All(pack.Evidence.SelectMany(group => group.Items),
                    item => Assert.True(item.AvailableAtUtc <= analysisTime));
                Assert.Single(pack.Conflicts);
                Assert.Contains(pack.MarketTimeframes, group =>
                    group.Timeframe == "M15" && group.Role == EvidenceTimeframeRole.Primary);

                Assert.Equal(7, await queryContext.EvidenceRecords.CountAsync());
                Assert.Equal(2, await queryContext.EvidenceQuarantineRecords.CountAsync());
                Assert.Single(await queryContext.EvidenceRelations
                    .Where(relation => relation.RelationType == "Republished")
                    .ToArrayAsync());
                var cluster = Assert.Single(await queryContext.EvidenceClusters.ToArrayAsync());
                Assert.Equal(2, await queryContext.EvidenceClusterMembers.CountAsync(member =>
                    member.EvidenceClusterId == cluster.Id));

                var paged = await query.GetEvidenceAsync(new EvidenceQuery(
                    "GOLD",
                    EvidenceType.News,
                    null,
                    analysisTime.AddHours(-1),
                    analysisTime,
                    null,
                    null,
                    null,
                    true,
                    analysisTime,
                    1,
                    1));
                Assert.Equal(2, paged.TotalItems);
                Assert.Equal(2, paged.TotalPages);
                Assert.Single(paged.Items);
            }
        }
        finally
        {
            await using var cleanupContext = new XauAiDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static EvidenceIngestionService IngestionService(
        XauAiDbContext context,
        EvidenceSettings settings,
        DateTimeOffset now) => new(
        new EvidenceNormalizer(),
        new EfEvidenceStore(context, settings),
        settings,
        new FixedTimeProvider(now),
        NullLogger<EvidenceIngestionService>.Instance);

    private static IReadOnlyList<EvidenceInput> Inputs(DateTimeOffset analysisTime) =>
    [
        Input(EvidenceType.Market, EvidenceSourceType.InternalMarketData, "alltick", "market-1", analysisTime.AddMinutes(-15), analysisTime, "M15", "XAUUSD M15 candle", "SELL", "Commodity", "{\"close\":3900}"),
        Input(EvidenceType.News, EvidenceSourceType.NewsProvider, "news-a", "news-1", analysisTime.AddMinutes(-5), analysisTime.AddMinutes(-5), null, "Fed comments support gold", null, "Fed", null, "fed-event-1", EvidenceClusterType.NewsEvent),
        Input(EvidenceType.News, EvidenceSourceType.NewsProvider, "news-b", "news-2", analysisTime.AddMinutes(-5), analysisTime.AddMinutes(-5), null, "Fed comments support gold", null, "Fed", null, "fed-event-1", EvidenceClusterType.NewsEvent),
        Input(EvidenceType.Economic, EvidenceSourceType.EconomicProvider, "fred", "cpi-2026-09", analysisTime.AddDays(-1), analysisTime.AddMinutes(-10), null, "CPI observation", null, "Inflation", null, value: 3.2m, unit: "%", timestampQuality: EvidenceTimestampQuality.DateOnly),
        Input(EvidenceType.Analyst, EvidenceSourceType.AnalystProvider, "analyst-a", "analyst-1", analysisTime.AddMinutes(-20), analysisTime.AddMinutes(-20), null, "Bullish gold outlook", "BUY", "Commodity"),
        Input(EvidenceType.Analyst, EvidenceSourceType.AnalystProvider, "analyst-b", "analyst-2", analysisTime.AddMinutes(-15), analysisTime.AddMinutes(-15), null, "Bearish gold outlook", "SELL", "Commodity"),
        Input(EvidenceType.News, EvidenceSourceType.NewsProvider, "news-c", "future-news", analysisTime.AddMinutes(5), analysisTime.AddMinutes(5), null, "Future gold headline", null, "Commodity"),
        Input(EvidenceType.News, EvidenceSourceType.NewsProvider, "news-invalid", "invalid-future", analysisTime.AddMinutes(40), analysisTime.AddMinutes(40), null, "Not available yet", null, "Commodity")
    ];

    private static EvidenceInput Input(
        EvidenceType evidenceType,
        EvidenceSourceType sourceType,
        string sourceKey,
        string externalId,
        DateTimeOffset eventTime,
        DateTimeOffset availableAt,
        string? timeframe,
        string title,
        string? direction,
        string category,
        string? metadataJson = null,
        string? clusterKey = null,
        EvidenceClusterType? clusterType = null,
        decimal? value = null,
        string? unit = null,
        EvidenceTimestampQuality timestampQuality = EvidenceTimestampQuality.Exact) => new()
        {
            EvidenceType = evidenceType,
            SourceType = sourceType,
            SourceKey = sourceKey,
            ExternalId = externalId,
            Instrument = "GOLD",
            OriginalInstrument = "Gold",
            Timeframe = timeframe,
            EventTime = eventTime,
            AvailableAt = availableAt,
            PublishedAt = evidenceType is EvidenceType.News or EvidenceType.Analyst ? eventTime : null,
            CollectedAt = availableAt,
            Title = title,
            Summary = evidenceType == EvidenceType.News ? "Synthetic test-only summary." : null,
            Value = value,
            OriginalValue = value?.ToString(),
            Unit = unit,
            Direction = direction,
            Category = category,
            TimestampQuality = timestampQuality,
            SourceReliability = EvidenceSourceReliability.Known,
            MetadataJson = metadataJson,
            ClusterKey = clusterKey,
            ClusterType = clusterType
        };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
