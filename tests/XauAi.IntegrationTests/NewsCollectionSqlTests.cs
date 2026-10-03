using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application;
using XauAi.Application.News;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace XauAi.IntegrationTests;

public sealed class NewsCollectionSqlTests(ITestOutputHelper output)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-02T10:00:00Z");

    [SqlServerFact]
    public async Task Pipeline_is_incremental_idempotent_restart_safe_queryable_and_measured()
    {
        await using var fixture = await NewsFixture.CreateAsync();
        var timer = Stopwatch.StartNew();

        var first = await fixture.CollectAsync(Now.AddHours(-1), Now);
        timer.Stop();
        var firstDuration = timer.Elapsed;
        var repeated = await fixture.CollectAsync(Now.AddHours(-1), Now);

        Assert.Equal(20, first.RequestsMade);
        Assert.Equal(1_000, first.ArticlesReceived);
        Assert.Equal(1_000, first.ArticlesInserted);
        Assert.Equal(0, first.ArticlesSkipped);
        Assert.Equal(0, repeated.ArticlesInserted);
        Assert.Equal(1_000, repeated.ArticlesSkipped);

        var queryTimer = Stopwatch.StartNew();
        await using (var queryScope = fixture.Services.CreateAsyncScope())
        {
            var query = queryScope.ServiceProvider.GetRequiredService<INewsQueryService>();
            var result = await query.QueryAsync(new NewsArticleQuery(
                FromUtc: Now.AddHours(-1),
                ToUtc: Now,
                Category: "Gold",
                Source: "Deterministic Wire",
                MinimumRelevance: NewsRelevanceLevel.VeryHigh,
                Page: 2,
                PageSize: 100));

            Assert.Equal(1_000, result.TotalItems);
            Assert.Equal(10, result.TotalPages);
            Assert.Equal(100, result.Items.Count);
            Assert.All(result.Items, article => Assert.Equal(NewsRelevanceLevel.VeryHigh, article.Relevance));
        }

        queryTimer.Stop();
        Guid interruptedRunId;
        await using (var interruptedScope = fixture.Services.CreateAsyncScope())
        {
            var state = interruptedScope.ServiceProvider.GetRequiredService<INewsCollectionStateStore>();
            interruptedRunId = await state.StartAsync(
                "newsdata",
                Now.AddMinutes(-30),
                Now,
                Now.AddMinutes(1));
        }

        var recovery = await fixture.CollectAsync(Now.AddMinutes(-30), Now);
        Assert.Equal(0, recovery.ArticlesInserted);

        await using (var verificationScope = fixture.Services.CreateAsyncScope())
        {
            var context = verificationScope.ServiceProvider.GetRequiredService<XauAiDbContext>();
            Assert.Equal(1_000, await context.NewsArticles.CountAsync());
            Assert.Equal(1_000, await context.EvidenceRecords.CountAsync(record => record.Kind == "NewsArticle"));
            Assert.Equal("Interrupted", (await context.NewsCollectionRuns.FindAsync(interruptedRunId))!.Status);
            Assert.Equal(0, await context.NewsArticles
                .GroupBy(article => article.CanonicalUrlHash)
                .Where(group => group.Count() > 1)
                .CountAsync());
            var state = await context.NewsCollectionStates.SingleAsync();
            Assert.Equal("Healthy", state.Status);
            Assert.Equal(0, state.ConsecutiveFailures);
            Assert.NotNull(state.LastSuccessfulCollectionAtUtc);
        }

        output.WriteLine(
            "1,000 news articles: collection+SQL persistence={0} ms; filtered page query={1} ms; provider requests={2}",
            firstDuration.TotalMilliseconds,
            queryTimer.ElapsedMilliseconds,
            first.RequestsMade);
        Assert.True(firstDuration < TimeSpan.FromMinutes(2));
        Assert.True(queryTimer.Elapsed < TimeSpan.FromSeconds(10));
    }

    private sealed class NewsFixture : IAsyncDisposable
    {
        private readonly DbContextOptions<XauAiDbContext> _cleanupOptions;

        private NewsFixture(ServiceProvider services, DbContextOptions<XauAiDbContext> cleanupOptions)
        {
            Services = services;
            _cleanupOptions = cleanupOptions;
        }

        public ServiceProvider Services { get; }

        public static async Task<NewsFixture> CreateAsync()
        {
            var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
            var connectionBuilder = new SqlConnectionStringBuilder(serverConnection)
            {
                InitialCatalog = $"XauAiNewsTests_{Guid.NewGuid():N}"
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = "XAUUSD-AI News Tests",
                ["Application:Version"] = "1.0.0-test",
                ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
                ["Database:Enabled"] = "true",
                ["Database:ConnectionString"] = connectionBuilder.ConnectionString,
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["News:Enabled"] = "true",
                ["News:Provider"] = "NewsData",
                ["News:ApiKey"] = "unit-test-news-key",
                ["News:BaseUrl"] = "https://newsdata.test/api/1/",
                ["News:PageSize"] = "50",
                ["News:MaximumPagesPerCollection"] = "20",
                ["News:MaximumPageSize"] = "100",
                ["News:MaxRetries"] = "0",
                ["News:MinimumRelevance"] = "Medium"
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.RemoveAll<INewsProvider>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
            services.AddSingleton<INewsProvider, DeterministicNewsProvider>();
            var serviceProvider = services.BuildServiceProvider();
            var cleanupOptions = new DbContextOptionsBuilder<XauAiDbContext>()
                .UseSqlServer(connectionBuilder.ConnectionString)
                .Options;
            var fixture = new NewsFixture(serviceProvider, cleanupOptions);
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<XauAiDbContext>();
                await context.Database.MigrateAsync();
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public async Task<NewsCollectionResult> CollectAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<INewsCollectionService>()
                .CollectAsync(new NewsCollectionRequest(fromUtc, toUtc));
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

    private sealed class DeterministicNewsProvider : INewsProvider
    {
        public Task<NewsProviderPage> GetNewsAsync(
            NewsProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            var page = request.PageToken is null ? 0 : int.Parse(request.PageToken);
            var articles = Enumerable.Range(page * request.PageSize, request.PageSize)
                .Select(index => new ProviderNewsArticle(
                    $"provider-{index}",
                    $"Gold market update number {index}",
                    "The Federal Reserve, USD and Treasury yields remain in focus.",
                    null,
                    $"https://publisher.test/gold/{index}?utm_source=integration",
                    "Deterministic Wire",
                    "https://publisher.test",
                    ["Test Reporter"],
                    request.ToUtc.AddSeconds(-index - 1),
                    "en",
                    ["us"],
                    ["business"],
                    ["gold", "federal reserve"],
                    null))
                .ToArray();
            return Task.FromResult(new NewsProviderPage(
                articles,
                page < 19 ? (page + 1).ToString() : null,
                1_000));
        }

        public Task<NewsProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsProviderStatus(
                "NewsData", NewsProviderState.Available, true, "Test provider", Now));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
