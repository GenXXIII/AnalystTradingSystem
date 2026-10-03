using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XauAi.Application;
using XauAi.Application.News;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class NewsDataLiveIntegrationTests
{
    [NewsDataLiveFact]
    public async Task Live_newsdata_response_is_normalized_persisted_and_queryable()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var apiKey = Environment.GetEnvironmentVariable(NewsDataLiveFactAttribute.ApiKeyVariableName)!;
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = $"XauAiNewsDataLiveTests_{Guid.NewGuid():N}"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Application:Name"] = "XAUUSD-AI NewsData Live Tests",
            ["Application:Version"] = "1.0.0-test",
            ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
            ["Database:Enabled"] = "true",
            ["Database:ConnectionString"] = connectionBuilder.ConnectionString,
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["News:Enabled"] = "true",
            ["News:Provider"] = "NewsData",
            ["News:ApiKey"] = apiKey,
            ["News:BaseUrl"] = "https://newsdata.io/api/1/",
            ["News:PageSize"] = "10",
            ["News:MaximumPagesPerCollection"] = "1",
            ["News:MaxRetries"] = "0",
            ["News:MinimumRelevance"] = "Medium"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        await using var serviceProvider = services.BuildServiceProvider();
        var cleanupOptions = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<XauAiDbContext>();
            await context.Database.MigrateAsync();
            var result = await scope.ServiceProvider.GetRequiredService<INewsCollectionService>()
                .CollectAsync(new NewsCollectionRequest());
            var query = await scope.ServiceProvider.GetRequiredService<INewsQueryService>()
                .QueryAsync(new NewsArticleQuery(PageSize: 10));

            Assert.Equal(1, result.RequestsMade);
            Assert.Equal(result.ArticlesInserted, await context.NewsArticles.CountAsync());
            Assert.Equal(result.ArticlesInserted, query.TotalItems);
            Assert.All(query.Items, article => Assert.Equal("NewsData", article.Provider));
            Assert.All(query.Items, article => Assert.StartsWith("http", article.Url, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await using var cleanup = new XauAiDbContext(cleanupOptions);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}

public sealed class NewsDataLiveFactAttribute : FactAttribute
{
    public const string EnableVariableName = "XAUAI_RUN_NEWSDATA_INTEGRATION";
    public const string ApiKeyVariableName = "NEWS_API_KEY";

    public NewsDataLiveFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(EnableVariableName),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"Set {EnableVariableName}=true to opt in to the paid external provider call.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApiKeyVariableName)))
        {
            Skip = $"Set {ApiKeyVariableName} to run the live NewsData.io integration test.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)))
        {
            Skip = $"Set {SqlServerFactAttribute.EnvironmentVariableName} to run the live NewsData.io integration test.";
        }
    }
}
