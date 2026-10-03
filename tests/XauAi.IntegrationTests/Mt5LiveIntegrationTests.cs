using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XauAi.Application;
using XauAi.Application.MarketData;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class Mt5LiveIntegrationTests
{
    [Mt5LiveFact]
    public async Task Live_mt5_data_is_normalized_persisted_and_idempotent()
    {
        var serverConnection = Required(SqlServerFactAttribute.EnvironmentVariableName);
        var databaseName = $"XauAiMt5Tests_{Guid.NewGuid():N}";
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = databaseName
        };
        var configuration = BuildConfiguration(connectionBuilder.ConnectionString);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        await using var serviceProvider = services.BuildServiceProvider();

        try
        {
            await using (var migrationScope = serviceProvider.CreateAsyncScope())
            {
                var context = migrationScope.ServiceProvider.GetRequiredService<XauAiDbContext>();
                await context.Database.MigrateAsync();
            }

            var marketProvider = serviceProvider.GetRequiredService<IMarketDataProvider>();
            var status = await marketProvider.GetStatusAsync();
            Assert.Equal(MarketDataProviderState.Connected, status.State);

            var quote = await marketProvider.GetQuoteAsync("XAUUSD");
            Assert.True(quote.Ask >= quote.Bid);
            Assert.Equal(TimeSpan.Zero, quote.TimestampUtc.Offset);

            var toUtc = DateTimeOffset.UtcNow;
            var fromUtc = toUtc.AddDays(-2);
            MarketDataPipelineResult firstSync;
            MarketDataPipelineResult secondSync;
            MarketDataPipelineResult incrementalSync;
            MarketDataPipelineResult m15Sync;
            await using (var firstScope = serviceProvider.CreateAsyncScope())
            {
                var synchronization = firstScope.ServiceProvider.GetRequiredService<IMarketDataSynchronizationService>();
                firstSync = await synchronization.SynchronizeAsync(new MarketDataSynchronizationRequest(
                    "XAUUSD", MarketTimeframe.H1, fromUtc, toUtc));
            }

            await using (var secondScope = serviceProvider.CreateAsyncScope())
            {
                var synchronization = secondScope.ServiceProvider.GetRequiredService<IMarketDataSynchronizationService>();
                secondSync = await synchronization.SynchronizeAsync(new MarketDataSynchronizationRequest(
                    "XAUUSD", MarketTimeframe.H1, fromUtc, toUtc));
            }

            await using (var incrementalScope = serviceProvider.CreateAsyncScope())
            {
                var synchronization = incrementalScope.ServiceProvider.GetRequiredService<IMarketDataSynchronizationService>();
                incrementalSync = await synchronization.SynchronizeAsync(new MarketDataSynchronizationRequest(
                    "XAUUSD", MarketTimeframe.H1, FromUtc: null, ToUtc: DateTimeOffset.UtcNow));
            }

            await using (var m15Scope = serviceProvider.CreateAsyncScope())
            {
                var synchronization = m15Scope.ServiceProvider.GetRequiredService<IMarketDataSynchronizationService>();
                m15Sync = await synchronization.SynchronizeAsync(new MarketDataSynchronizationRequest(
                    "XAUUSD", MarketTimeframe.M15, DateTimeOffset.UtcNow.AddHours(-6), DateTimeOffset.UtcNow));
            }

            Assert.True(firstSync.Received > 0);
            Assert.True(firstSync.Inserted > 0);
            Assert.Equal(0, secondSync.Inserted);
            Assert.True(secondSync.Skipped > 0);
            Assert.True(incrementalSync.RequestedFromUtc > firstSync.RequestedFromUtc);
            Assert.True(m15Sync.Inserted > 0);

            await using var queryScope = serviceProvider.CreateAsyncScope();
            var queryContext = queryScope.ServiceProvider.GetRequiredService<XauAiDbContext>();
            var stored = await queryContext.MarketCandles
                .Where(candle => candle.ProviderSymbol == Required("MT5_SYMBOL"))
                .OrderBy(candle => candle.OpenTimeUtc)
                .ToListAsync();
            Assert.Equal(firstSync.Inserted + incrementalSync.Inserted + m15Sync.Inserted, stored.Count);
            Assert.All(stored, candle => Assert.Equal(TimeSpan.Zero, candle.OpenTimeUtc.Offset));
            Assert.Equal(0, stored
                .GroupBy(candle => new { candle.InstrumentId, candle.TimeframeId, candle.DataProviderId, candle.OpenTimeUtc })
                .Count(group => group.Count() > 1));

            var states = await queryContext.MarketDataSyncStates.ToListAsync();
            Assert.Equal(2, states.Count);
            Assert.All(states, state => Assert.Equal("Healthy", state.Status));
        }
        finally
        {
            var options = new DbContextOptionsBuilder<XauAiDbContext>()
                .UseSqlServer(connectionBuilder.ConnectionString)
                .Options;
            await using var cleanup = new XauAiDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static IConfiguration BuildConfiguration(string connectionString)
    {
        var repositoryRoot = FindRepositoryRoot();
        var values = new Dictionary<string, string?>
        {
            ["Application:Name"] = "XAUUSD-AI MT5 Integration Tests",
            ["Application:Version"] = "1.0.0-test",
            ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
            ["Database:Enabled"] = "true",
            ["Database:ConnectionString"] = connectionString,
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["MarketData:Provider"] = "MT5",
            ["MarketData:ProviderKey"] = "mt5",
            ["MT5:Enabled"] = "true",
            ["MT5:Login"] = Required("MT5_LOGIN"),
            ["MT5:Password"] = Required("MT5_PASSWORD"),
            ["MT5:Server"] = Required("MT5_SERVER"),
            ["MT5:TerminalPath"] = Required("MT5_TERMINAL_PATH"),
            ["MT5:ApplicationSymbol"] = "XAUUSD",
            ["MT5:Symbol"] = Required("MT5_SYMBOL"),
            ["MT5:TimeZone"] = "UTC",
            ["MT5:PythonExecutable"] = "py",
            ["MT5:BridgeScriptPath"] = Path.Combine(
                repositoryRoot,
                "backend",
                "XauAi.Infrastructure",
                "MarketData",
                "Mt5",
                "Bridge",
                "mt5_bridge.py")
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"{name} is required for the opt-in MT5 integration test.");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "XauAi.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

public sealed class Mt5LiveFactAttribute : FactAttribute
{
    private static readonly string[] RequiredVariables =
    [
        SqlServerFactAttribute.EnvironmentVariableName,
        "MT5_LOGIN",
        "MT5_PASSWORD",
        "MT5_SERVER",
        "MT5_TERMINAL_PATH",
        "MT5_SYMBOL"
    ];

    public Mt5LiveFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("XAUAI_RUN_MT5_INTEGRATION"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set XAUAI_RUN_MT5_INTEGRATION=true to run the live MT5 integration test.";
            return;
        }

        var missing = RequiredVariables.Where(variable =>
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)));
        if (missing.Any())
        {
            Skip = $"Set these variables to run the live MT5 integration test: {string.Join(", ", missing)}.";
        }
    }
}
