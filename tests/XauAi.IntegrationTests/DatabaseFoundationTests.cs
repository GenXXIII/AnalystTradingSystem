using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XauAi.Domain.Evidence;
using XauAi.Domain.Market;
using XauAi.Domain.ReferenceData;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class DatabaseFoundationTests
{
    [SqlServerFact]
    public async Task Initial_migration_and_database_constraints_work_on_sql_server()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var databaseName = $"XauAiPhase3Tests_{Guid.NewGuid():N}";
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = databaseName
        };
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;

        try
        {
            await using (var migrationContext = new XauAiDbContext(options))
            {
                await migrationContext.Database.MigrateAsync();
                var appliedMigrations = await migrationContext.Database.GetAppliedMigrationsAsync();
                Assert.Contains(appliedMigrations, migration =>
                    migration.EndsWith("_InitialDatabaseFoundation", StringComparison.Ordinal));
                Assert.Contains(appliedMigrations, migration =>
                    migration.EndsWith("_SeedMt5DataProvider", StringComparison.Ordinal));
                Assert.Contains(appliedMigrations, migration =>
                    migration.EndsWith("_ImplementNewsCollection", StringComparison.Ordinal));
                Assert.Contains(appliedMigrations, migration =>
                    migration.EndsWith("_ImplementEconomicDataPipeline", StringComparison.Ordinal));
                Assert.True(await migrationContext.DataProviders.AnyAsync(provider =>
                    provider.Key == "mt5"
                    && provider.Name == "MetaTrader 5"
                    && provider.IsActive));
                Assert.True(await migrationContext.DataProviders.AnyAsync(provider =>
                    provider.Key == "fred"
                    && provider.ProviderType == "Economic"
                    && provider.IsActive));
            }

            Guid instrumentId;
            Guid timeframeId;
            Guid providerId;
            var candleId = Guid.NewGuid();
            var sourceOpenTime = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.FromHours(7));

            await using (var insertContext = new XauAiDbContext(options))
            {
                instrumentId = await insertContext.Instruments
                    .Where(instrument => instrument.Symbol == "XAUUSD")
                    .Select(instrument => instrument.Id)
                    .SingleAsync();
                timeframeId = await insertContext.Timeframes
                    .Where(timeframe => timeframe.Code == "M1")
                    .Select(timeframe => timeframe.Id)
                    .SingleAsync();

                var provider = new DataProvider
                {
                    Key = "integration-test-market",
                    Name = "Integration Test Market Provider",
                    ProviderType = "Market",
                    IsActive = true
                };
                insertContext.DataProviders.Add(provider);
                await insertContext.SaveChangesAsync();
                providerId = provider.Id;

                insertContext.EvidenceRecords.Add(new EvidenceRecord
                {
                    Id = candleId,
                    Kind = "MarketCandle",
                    ObservedAtUtc = sourceOpenTime
                });
                insertContext.MarketCandles.Add(CreateCandle(
                    candleId,
                    instrumentId,
                    timeframeId,
                    providerId,
                    sourceOpenTime));
                await insertContext.SaveChangesAsync();
            }

            await using (var queryContext = new XauAiDbContext(options))
            {
                var candle = await queryContext.MarketCandles
                    .Where(value =>
                        value.InstrumentId == instrumentId &&
                        value.TimeframeId == timeframeId &&
                        value.DataProviderId == providerId &&
                        value.OpenTimeUtc >= sourceOpenTime.AddMinutes(-1) &&
                        value.OpenTimeUtc < sourceOpenTime.AddMinutes(1))
                    .SingleAsync();

                Assert.Equal(2345.12345678m, candle.Open);
                Assert.Equal(TimeSpan.Zero, candle.OpenTimeUtc.Offset);
                Assert.Equal(sourceOpenTime.UtcDateTime, candle.OpenTimeUtc.UtcDateTime);
            }

            await AssertDuplicateCandleRejectedAsync(
                options,
                instrumentId,
                timeframeId,
                providerId,
                sourceOpenTime);
            await AssertForeignKeyRejectedAsync(options, instrumentId, timeframeId, sourceOpenTime);
            await AssertPhysicalFoundationAsync(connectionBuilder.ConnectionString);
        }
        finally
        {
            await using var cleanupContext = new XauAiDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static async Task AssertDuplicateCandleRejectedAsync(
        DbContextOptions<XauAiDbContext> options,
        Guid instrumentId,
        Guid timeframeId,
        Guid providerId,
        DateTimeOffset sourceOpenTime)
    {
        await using var context = new XauAiDbContext(options);
        var duplicateId = Guid.NewGuid();
        context.EvidenceRecords.Add(new EvidenceRecord
        {
            Id = duplicateId,
            Kind = "MarketCandle",
            ObservedAtUtc = sourceOpenTime
        });
        context.MarketCandles.Add(CreateCandle(
            duplicateId,
            instrumentId,
            timeframeId,
            providerId,
            sourceOpenTime));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertForeignKeyRejectedAsync(
        DbContextOptions<XauAiDbContext> options,
        Guid instrumentId,
        Guid timeframeId,
        DateTimeOffset sourceOpenTime)
    {
        await using var context = new XauAiDbContext(options);
        var candleId = Guid.NewGuid();
        context.EvidenceRecords.Add(new EvidenceRecord
        {
            Id = candleId,
            Kind = "MarketCandle",
            ObservedAtUtc = sourceOpenTime.AddMinutes(1)
        });
        context.MarketCandles.Add(CreateCandle(
            candleId,
            instrumentId,
            timeframeId,
            Guid.NewGuid(),
            sourceOpenTime.AddMinutes(1)));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertPhysicalFoundationAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0";
        Assert.True(Convert.ToInt32(await tableCommand.ExecuteScalarAsync()) >= 25);

        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = """
            SELECT COUNT(*)
            FROM sys.indexes
            WHERE name IN (
                'UX_Instruments_Symbol',
                'UX_Timeframes_Code',
                'UX_MarketCandles_Instrument_Timeframe_OpenTime_Provider',
                'IX_MarketCandles_Provider_Instrument_Timeframe_OpenTime',
                'UX_NewsArticles_Provider_ExternalId',
                'UX_NewsArticles_CanonicalUrlHash',
                'UX_NewsArticles_ContentHash',
                'IX_NewsArticles_PublishedAtUtc',
                'IX_NewsArticles_Instrument_PublishedAtUtc',
                'IX_NewsArticles_Category_PublishedAtUtc',
                'IX_NewsArticles_Relevance_PublishedAtUtc',
                'IX_NewsArticles_Source_PublishedAtUtc',
                'IX_NewsCollectionRuns_Provider_StartedAtUtc',
                'UX_EconomicEvents_Provider_ExternalId',
                'IX_EconomicEvents_ScheduledAtUtc',
                'UX_AnalystStatements_Provider_ExternalId',
                'IX_AnalystStatements_Instrument_PublishedAtUtc')
            """;
        Assert.Equal(17, Convert.ToInt32(await indexCommand.ExecuteScalarAsync()));
    }

    private static MarketCandle CreateCandle(
        Guid id,
        Guid instrumentId,
        Guid timeframeId,
        Guid providerId,
        DateTimeOffset openTime) =>
        new()
        {
            Id = id,
            InstrumentId = instrumentId,
            TimeframeId = timeframeId,
            DataProviderId = providerId,
            ProviderSymbol = "XAUUSD.test",
            OpenTimeUtc = openTime,
            CloseTimeUtc = openTime.AddMinutes(1),
            Open = 2345.12345678m,
            High = 2346.12345678m,
            Low = 2344.12345678m,
            Close = 2345.87654321m,
            TickVolume = 123.45670000m,
            Spread = 0.25000000m,
            IsComplete = true,
            SourceTimeZone = "Asia/Bangkok",
            FetchedAtUtc = DateTimeOffset.UtcNow
        };
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string EnvironmentVariableName = "XAUAI_TEST_SQLSERVER_CONNECTION_STRING";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariableName)))
        {
            Skip = $"Set {EnvironmentVariableName} to run SQL Server integration tests.";
        }
    }
}
