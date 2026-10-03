using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application.News;

namespace XauAi.IntegrationTests;

public sealed class NewsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-02T10:00:00Z");
    private static readonly Guid ArticleId = Guid.Parse("70000000-0000-0000-0000-000000000001");

    [Theory]
    [InlineData("/api/news/?page=1&pageSize=10")]
    [InlineData("/api/news/latest?pageSize=10")]
    [InlineData("/api/news/relevant?minimumRelevance=High&page=1&pageSize=10")]
    public async Task News_lists_return_application_owned_paginated_results(string path)
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();

        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("success").GetBoolean());
        var data = body.GetProperty("data");
        Assert.Equal(1, data.GetProperty("totalItems").GetInt32());
        Assert.Equal("Gold gains after Fed decision", data.GetProperty("items")[0].GetProperty("title").GetString());
        Assert.Equal("VeryHigh", data.GetProperty("items")[0].GetProperty("relevance").GetString());
    }

    [Fact]
    public async Task Detail_status_and_collection_endpoints_return_safe_structured_results()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();

        using var detailResponse = await client.GetAsync($"/api/news/{ArticleId}");
        using var statusResponse = await client.GetAsync("/api/news/status");
        using var collectResponse = await client.PostAsJsonAsync("/api/news/collect", new { });
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        var collected = await collectResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal("Available", status.GetProperty("data").GetProperty("provider").GetProperty("state").GetString());
        Assert.Equal(1, status.GetProperty("data").GetProperty("storedArticles").GetInt32());
        Assert.Equal(HttpStatusCode.OK, collectResponse.StatusCode);
        Assert.Equal(1, collected.GetProperty("data").GetProperty("articlesInserted").GetInt32());
    }

    [Fact]
    public async Task News_errors_are_safe_and_do_not_include_credentials_or_stack_traces()
    {
        const string sentinel = "secret-news-key-must-not-leak";
        using var configuredFactory = CreateFactory(failCollection: true);
        using var client = configuredFactory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/news/collect", new { });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var json = body.ToString();

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Contains(NewsErrorCodes.RateLimited, json, StringComparison.Ordinal);
        Assert.DoesNotContain(sentinel, json, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", json, StringComparison.OrdinalIgnoreCase);
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory(
        bool failCollection = false) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<INewsQueryService>();
            services.RemoveAll<INewsCollectionService>();
            services.AddSingleton<INewsQueryService>(new ApiNewsService());
            services.AddSingleton<INewsCollectionService>(new ApiCollectionService(failCollection));
        }));

    private static NewsArticleResult Article() => new(
        ArticleId,
        "NewsData",
        "provider-1",
        "Gold gains after Fed decision",
        "USD and Treasury yields moved.",
        null,
        "https://publisher.test/gold",
        "Example Wire",
        "https://publisher.test",
        "Example Wire",
        "Reporter",
        Now.AddMinutes(-15),
        Now,
        "en",
        ["US"],
        ["Gold", "FederalReserve"],
        ["Gold", "Federal Reserve"],
        NewsRelevanceLevel.VeryHigh,
        NewsFreshness.VeryRecent,
        null);

    private sealed class ApiNewsService : INewsQueryService
    {
        private static PagedNewsArticles Page() => new([Article()], 1, 10, 1, 1);

        public Task<PagedNewsArticles> QueryAsync(NewsArticleQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Page());

        public Task<PagedNewsArticles> GetLatestAsync(int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(Page());

        public Task<PagedNewsArticles> GetRelevantAsync(NewsRelevanceLevel minimumRelevance, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(Page());

        public Task<NewsArticleResult> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            id == ArticleId
                ? Task.FromResult(Article())
                : Task.FromException<NewsArticleResult>(new NewsException(NewsErrorCodes.NotFound, "Article not found."));

        public Task<NewsSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsSystemStatus(
                new NewsProviderStatus("NewsData", NewsProviderState.Available, true, "Configured", Now),
                null,
                1));
    }

    private sealed class ApiCollectionService(bool fail) : INewsCollectionService
    {
        public Task<NewsCollectionResult> CollectAsync(
            NewsCollectionRequest request,
            CancellationToken cancellationToken = default) => fail
                ? Task.FromException<NewsCollectionResult>(new NewsException(
                    NewsErrorCodes.RateLimited,
                    "The provider rate limit was reached.",
                    transient: true))
                : Task.FromResult(new NewsCollectionResult(
                    Guid.NewGuid(), "NewsData", Now.AddHours(-1), Now,
                    1, 0, 1, 1, 0, 0, 25));
    }
}
