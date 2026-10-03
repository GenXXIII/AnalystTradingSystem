using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XauAi.Application;
using XauAi.Application.EconomicData;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class FredLiveIntegrationTests
{
    [FredLiveFact]
    public async Task Live_fred_response_is_normalized_persisted_and_queryable()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var apiKey = Environment.GetEnvironmentVariable(FredLiveFactAttribute.ApiKeyVariableName)!;
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = $"XauAiFredLiveTests_{Guid.NewGuid():N}"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Application:Name"] = "XAUUSD-AI FRED Live Tests",
            ["Application:Version"] = "1.0.0-test",
            ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
            ["Database:Enabled"] = "true",
            ["Database:ConnectionString"] = connectionBuilder.ConnectionString,
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["EconomicData:Enabled"] = "true",
            ["EconomicData:Provider"] = "FRED",
            ["EconomicData:ProviderKey"] = "fred",
            ["EconomicData:ApiKey"] = apiKey,
            ["EconomicData:BaseUrl"] = "https://api.stlouisfed.org/fred/",
            ["EconomicData:TrackedSeries"] = "CPIAUCSL:Inflation",
            ["EconomicData:RateLimitPerMinute"] = "0",
            ["EconomicData:MaxRetries"] = "0",
            ["EconomicData:ProviderPageSize"] = "1000",
            ["EconomicData:MaximumPagesPerSeries"] = "1"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        var cleanupOptions = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;

        try
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<XauAiDbContext>();
            await context.Database.MigrateAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await scope.ServiceProvider.GetRequiredService<IEconomicDataSynchronizationService>()
                .SynchronizeAsync(new EconomicSyncRequest("CPIAUCSL", today.AddMonths(-6), today));
            var series = Assert.Single(result.Series);
            var stored = await context.EconomicObservations.CountAsync();
            var queriedSeries = await scope.ServiceProvider.GetRequiredService<IEconomicDataQueryService>()
                .GetSeriesAsync(new EconomicSeriesQuery());

            Assert.Equal("Succeeded", series.Status);
            Assert.Equal(2, series.RequestsMade);
            Assert.True(stored > 0);
            Assert.Equal(stored, series.RecordsInserted);
            Assert.Equal("CPIAUCSL", Assert.Single(queriedSeries).ExternalSeriesId);
        }
        finally
        {
            await using var cleanup = new XauAiDbContext(cleanupOptions);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}

public sealed class FredLiveFactAttribute : FactAttribute
{
    public const string EnableVariableName = "XAUAI_RUN_FRED_INTEGRATION";
    public const string ApiKeyVariableName = "FRED_API_KEY";

    public FredLiveFactAttribute()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable(EnableVariableName),
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"Set {EnableVariableName}=true to opt in to the external FRED provider call.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApiKeyVariableName)))
        {
            Skip = $"Set {ApiKeyVariableName} to run the live FRED integration test.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)))
        {
            Skip = $"Set {SqlServerFactAttribute.EnvironmentVariableName} to run the live FRED integration test.";
        }
    }
}
