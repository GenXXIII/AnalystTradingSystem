using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XauAi.Application;
using XauAi.Application.EconomicData;
using XauAi.Domain.EconomicData;
using XauAi.Domain.Evidence;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace XauAi.IntegrationTests;

public sealed class EconomicDataSqlTests(ITestOutputHelper output)
{
    [SqlServerFact]
    public async Task Status_reads_scoped_economic_stores_without_parallel_dbcontext_operations()
    {
        await using var fixture = await EconomicFixture.CreateAsync();
        await using var scope = fixture.Services.CreateAsyncScope();

        var status = await scope.ServiceProvider
            .GetRequiredService<IEconomicDataQueryService>()
            .GetStatusAsync();

        Assert.Empty(status.Series);
        Assert.Equal(0, status.StoredSeries);
        Assert.Equal(0, status.StoredObservations);
    }

    [SqlServerFact]
    public async Task Pipeline_schema_is_precise_idempotent_revision_safe_and_queryable()
    {
        await using var fixture = await EconomicFixture.CreateAsync();
        var now = DateTimeOffset.Parse("2026-10-03T00:00:00Z");
        Guid seriesId;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var seriesStore = scope.ServiceProvider.GetRequiredService<IEconomicSeriesStore>();
            var series = await seriesStore.UpsertAsync(
                new ProviderEconomicSeries(
                    "TEST_CPI", "TEST_ONLY CPI", null, "Percent", "Daily", "Test adjustment",
                    new DateOnly(2024, 1, 1), new DateOnly(2026, 9, 26), now),
                new EconomicSeriesDefinition("TEST_CPI", "Inflation"),
                "fred",
                now);
            seriesId = series.Id;
        }

        var observations = Enumerable.Range(0, 1_000)
            .Select(index => new ProviderEconomicObservation(
                new DateOnly(2024, 1, 1).AddDays(index),
                index + 0.125m,
                $"{index}.125",
                "Available",
                null,
                null))
            .ToArray();
        var timer = Stopwatch.StartNew();
        EconomicObservationPersistenceResult first;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IEconomicObservationStore>();
            first = await store.PersistAsync(seriesId, observations, now);
        }

        timer.Stop();
        EconomicObservationPersistenceResult replay;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IEconomicObservationStore>();
            replay = await store.PersistAsync(seriesId, observations, now.AddHours(1));
            var revision = await store.PersistAsync(
                seriesId,
                [observations[0] with { Value = 999.875m, OriginalValue = "999.875" }],
                now.AddDays(1));
            Assert.Equal(1, revision.Updated);
        }

        var queryTimer = Stopwatch.StartNew();
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IEconomicObservationStore>();
            var page = await store.QueryAsync(new EconomicObservationQuery(
                seriesId,
                new DateOnly(2024, 1, 1),
                new DateOnly(2026, 9, 26),
                2,
                100));
            queryTimer.Stop();
            Assert.Equal(1_000, page.TotalItems);
            Assert.Equal(100, page.Items.Count);
        }

        Assert.Equal(1_000, first.Inserted);
        Assert.Equal(1_000, replay.Skipped);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<XauAiDbContext>();
            Assert.Equal(1_000, await context.EconomicObservations.CountAsync());
            Assert.Equal(1, await context.EconomicObservationRevisions.CountAsync());
            Assert.Equal(999.875m, (await context.EconomicObservations
                .SingleAsync(item => item.ObservationDate == new DateOnly(2024, 1, 1))).Value);

            var duplicateId = Guid.NewGuid();
            context.EvidenceRecords.Add(new EvidenceRecord
            {
                Id = duplicateId,
                Kind = "EconomicObservation",
                ObservedAtUtc = now,
                CreatedAtUtc = now
            });
            context.EconomicObservations.Add(new EconomicObservation
            {
                Id = duplicateId,
                EconomicSeriesId = seriesId,
                ObservationDate = new DateOnly(2024, 1, 1),
                Value = 1m,
                OriginalValue = "1",
                Status = "Available",
                FetchedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await fixture.AssertSchemaAsync();
        output.WriteLine(
            "1,000 economic observations: insert={0} ms; indexed 100-row range page={1} ms; provider requests=0",
            timer.ElapsedMilliseconds,
            queryTimer.ElapsedMilliseconds);
        Assert.True(timer.Elapsed < TimeSpan.FromMinutes(1));
        Assert.True(queryTimer.Elapsed < TimeSpan.FromSeconds(10));
    }

    private sealed class EconomicFixture : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly DbContextOptions<XauAiDbContext> _cleanupOptions;

        private EconomicFixture(ServiceProvider services, string connectionString, DbContextOptions<XauAiDbContext> cleanupOptions)
        {
            Services = services;
            _connectionString = connectionString;
            _cleanupOptions = cleanupOptions;
        }

        public ServiceProvider Services { get; }

        public static async Task<EconomicFixture> CreateAsync()
        {
            var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
            var connectionBuilder = new SqlConnectionStringBuilder(serverConnection)
            {
                InitialCatalog = $"XauAiEconomicTests_{Guid.NewGuid():N}"
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = "XAUUSD-AI Economic Tests",
                ["Application:Version"] = "1.0.0-test",
                ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
                ["Database:Enabled"] = "true",
                ["Database:ConnectionString"] = connectionBuilder.ConnectionString,
                ["Database:ApplyMigrationsOnStartup"] = "false"
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);
            var provider = services.BuildServiceProvider();
            var cleanupOptions = new DbContextOptionsBuilder<XauAiDbContext>()
                .UseSqlServer(connectionBuilder.ConnectionString)
                .Options;
            var fixture = new EconomicFixture(provider, connectionBuilder.ConnectionString, cleanupOptions);
            try
            {
                await using var scope = provider.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<XauAiDbContext>().Database.MigrateAsync();
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public async Task AssertSchemaAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    (SELECT COUNT(*) FROM sys.indexes WHERE name = 'UX_EconomicObservations_Series_ObservationDate'),
                    (SELECT COUNT(*) FROM sys.foreign_keys WHERE name = 'FK_EconomicObservations_EconomicSeries_EconomicSeriesId'),
                    (SELECT COUNT(*) FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
                     WHERE c.object_id = OBJECT_ID('EconomicObservations') AND c.name = 'Value'
                       AND t.name = 'decimal' AND c.precision = 28 AND c.scale = 8),
                    (SELECT COUNT(*) FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
                     WHERE c.object_id = OBJECT_ID('EconomicObservations') AND c.name = 'ObservationDate'
                       AND t.name = 'date')
                """;
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(1, reader.GetInt32(0));
            Assert.Equal(1, reader.GetInt32(1));
            Assert.Equal(1, reader.GetInt32(2));
            Assert.Equal(1, reader.GetInt32(3));
        }

        public async ValueTask DisposeAsync()
        {
            await using (var cleanup = new XauAiDbContext(_cleanupOptions))
            {
                await cleanup.Database.EnsureDeletedAsync();
            }

            await Services.DisposeAsync();
        }
    }
}
