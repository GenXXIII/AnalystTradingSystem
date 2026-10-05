using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.Analysts;
using XauAi.Infrastructure.Analysts.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class AnalystDataSqlTests
{
    [SqlServerFact]
    public async Task Analyst_pipeline_preserves_history_prevents_duplicates_and_enforces_look_ahead()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var databaseName = $"XauAiAnalystTests_{Guid.NewGuid():N}";
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = databaseName
        };
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;
        var settings = new AnalystSettings
        {
            Enabled = true,
            Provider = "SyntheticTestProvider",
            ProviderKey = "analyst-rss",
            Symbol = "XAUUSD",
            MaximumPageSize = 100
        };
        var t1 = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddDays(1);

        try
        {
            await using (var migrationContext = new XauAiDbContext(options))
            {
                await migrationContext.Database.MigrateAsync();
            }

            var original = Item(
                id: Guid.Parse("10000000-0000-0000-0000-000000000101"),
                identityHash: Hash('1'),
                contentHash: Hash('a'),
                claimHash: Hash('b'),
                externalId: "synthetic-opinion-1",
                title: "Synthetic gold outlook version one",
                direction: AnalystDirection.Bullish,
                publishedAt: t1,
                sourceIdentityHash: Hash('c'));

            await using (var firstContext = new XauAiDbContext(options))
            {
                var store = Store(firstContext, settings);
                var first = await store.PersistAsync([original]);
                var duplicate = await store.PersistAsync([original]);

                Assert.Equal(1, first.PublicationsInserted);
                Assert.Equal(1, first.PredictionsInserted);
                Assert.Equal(0, duplicate.PublicationsInserted);
                Assert.Equal(1, duplicate.Duplicates);
            }

            var revision = Item(
                id: Guid.Parse("10000000-0000-0000-0000-000000000102"),
                identityHash: Hash('1'),
                contentHash: Hash('d'),
                claimHash: Hash('e'),
                externalId: "synthetic-opinion-1",
                title: "Synthetic gold outlook version two",
                direction: AnalystDirection.Bearish,
                publishedAt: t2,
                sourceIdentityHash: Hash('c'));
            var republished = Item(
                id: Guid.Parse("10000000-0000-0000-0000-000000000103"),
                identityHash: Hash('f'),
                contentHash: Hash('d'),
                claimHash: Hash('e'),
                externalId: "synthetic-republication-1",
                title: "Synthetic gold outlook version two",
                direction: AnalystDirection.Bearish,
                publishedAt: t2.AddMinutes(5),
                sourceIdentityHash: Hash('9'));

            await using (var secondContext = new XauAiDbContext(options))
            {
                var store = Store(secondContext, settings);
                var persisted = await store.PersistAsync([revision, republished]);

                Assert.Equal(2, persisted.PublicationsInserted);
                Assert.Equal(1, persisted.PredictionsInserted);
                Assert.Equal(1, persisted.Duplicates);
            }

            await using (var queryContext = new XauAiDbContext(options))
            {
                var query = new EfAnalystQueryStore(queryContext);
                var historical = await query.QueryPredictionsAsync(
                    new AnalystPredictionQuery(Page: 1, PageSize: 10),
                    t1.AddHours(1));
                var complete = await query.QueryPredictionsAsync(
                    new AnalystPredictionQuery(Page: 1, PageSize: 1),
                    t2.AddDays(1));
                var bearish = await query.QueryPredictionsAsync(
                    new AnalystPredictionQuery(
                        Instrument: "XAUUSD",
                        Direction: AnalystDirection.Bearish,
                        FromUtc: t2.AddMinutes(-1),
                        ToUtc: t2.AddMinutes(1),
                        Page: 1,
                        PageSize: 10),
                    t2.AddDays(1));

                var first = Assert.Single(historical.Items);
                Assert.Equal("Synthetic gold outlook version one", first.Title);
                Assert.Equal(AnalystDirection.Bullish, first.Direction);
                Assert.Equal(2, complete.TotalItems);
                Assert.Equal(2, complete.TotalPages);
                Assert.Single(complete.Items);
                Assert.Single(bearish.Items);
                Assert.Equal("Synthetic gold outlook version two", bearish.Items[0].Title);

                var publications = await queryContext.AnalystPublications
                    .OrderBy(value => value.PublishedAtUtc)
                    .ToArrayAsync();
                Assert.Equal(3, publications.Length);
                Assert.Equal(new[] { "Original", "Revision", "Republished" }, publications.Select(value => value.RelationshipType));
                Assert.Equal(1, publications[0].Version);
                Assert.Equal(2, publications[1].Version);
                Assert.Equal(publications[1].Id, publications[2].RelatedPublicationId);
                Assert.Equal(2, await queryContext.AnalystStatements.CountAsync());
            }

            await using (var stateContext = new XauAiDbContext(options))
            {
                var references = new AnalystReferenceResolver(stateContext);
                var stateStore = new EfAnalystSyncStateStore(stateContext, references);
                var runId = await stateStore.StartAsync("analyst-rss", t1, t2, t2.AddMinutes(1));
                await stateStore.CompleteAsync(
                    runId,
                    "analyst-rss",
                    t2.AddMinutes(2),
                    t2,
                    "synthetic-opinion-1",
                    new AnalystRunMetrics(2, 0, 2, 2, 1, 1, 0, 50));
                var state = await stateStore.GetAsync("analyst-rss");

                Assert.NotNull(state);
                Assert.Equal("Healthy", state.Status);
                Assert.Equal(t2, state.LastPublishedAtUtc);
                Assert.Equal("synthetic-opinion-1", state.LastExternalId);
                Assert.Equal(2, state.Metrics.RequestsMade);
            }
        }
        finally
        {
            await using var cleanupContext = new XauAiDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static EfAnalystIngestionStore Store(XauAiDbContext context, AnalystSettings settings) =>
        new(context, new AnalystReferenceResolver(context), settings);

    private static NormalizedAnalystItem Item(
        Guid id,
        string identityHash,
        string contentHash,
        string claimHash,
        string externalId,
        string title,
        AnalystDirection direction,
        DateTimeOffset publishedAt,
        string sourceIdentityHash) =>
        new(
            id,
            "SyntheticTestProvider",
            externalId,
            identityHash,
            title,
            "Synthetic test fixture; not real analyst data.",
            $"https://research.example.test/{externalId}",
            Hash(externalId[0]),
            contentHash,
            publishedAt,
            publishedAt.AddMinutes(3),
            null,
            "en",
            "Gold",
            new NormalizedAnalystSource(
                $"source-{sourceIdentityHash[0]}",
                sourceIdentityHash,
                $"Synthetic Source {sourceIdentityHash[0]}",
                "Research",
                "https://research.example.test",
                "US"),
            new NormalizedAnalystIdentity(
                "synthetic-analyst",
                Hash('7'),
                "Synthetic Test Analyst",
                "Strategist",
                null),
            [
                new NormalizedAnalystPrediction(
                    Guid.NewGuid(),
                    $"{externalId}#claim-1",
                    "XAUUSD",
                    "Commodity",
                    direction,
                    $"Synthetic {direction} gold claim.",
                    direction == AnalystDirection.Bullish ? 4100m : 3900m,
                    null,
                    null,
                    "USD",
                    1,
                    AnalystHorizonUnit.Weeks,
                    "1 week",
                    null,
                    "Synthetic test reason.",
                    "Gold",
                    claimHash)
            ]);

    private static string Hash(char value) => new(value, 64);
}
