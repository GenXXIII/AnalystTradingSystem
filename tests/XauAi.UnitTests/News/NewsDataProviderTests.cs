using System.Net;
using System.Net.Http.Headers;
using System.Text;
using XauAi.Application.News;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.News.NewsData;

namespace XauAi.UnitTests.News;

public sealed class NewsDataProviderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-02T10:00:00Z");

    [Fact]
    public async Task Maps_latest_response_and_builds_one_bounded_composite_request()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Json("""
                {
                  "status": "success",
                  "totalResults": 1,
                  "nextPage": "next-1",
                  "results": [{
                    "article_id": "article-1",
                    "title": "Gold gains after Fed decision",
                    "link": "https://publisher.test/gold",
                    "keywords": ["gold", "fed"],
                    "creator": ["Reporter"],
                    "description": "USD and yields moved.",
                    "content": null,
                    "pubDate": "2026-10-02 09:45:00",
                    "image_url": "https://publisher.test/image.jpg",
                    "source_id": "wire",
                    "source_name": "Example Wire",
                    "source_url": "https://publisher.test",
                    "country": ["united states of america"],
                    "category": ["business"],
                    "language": "english"
                  }]
                }
                """)
        });
        var provider = CreateProvider(handler, useTimeframeParameter: true);

        var page = await provider.GetNewsAsync(Request());

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https", request.Scheme);
        Assert.Equal("newsdata.test", request.Host);
        Assert.Equal("/api/1/latest", request.AbsolutePath);
        Assert.Contains("apikey=unit-test-api-key", request.Query, StringComparison.Ordinal);
        Assert.Contains("q=gold+OR+Federal+Reserve", request.Query, StringComparison.Ordinal);
        Assert.Contains("timeframe=60m", request.Query, StringComparison.Ordinal);
        Assert.Contains("size=10", request.Query, StringComparison.Ordinal);
        Assert.Equal("next-1", page.NextPageToken);
        var article = Assert.Single(page.Articles);
        Assert.Equal("article-1", article.ProviderArticleId);
        Assert.Equal("Example Wire", article.SourceName);
        Assert.Equal(TimeSpan.Zero, article.PublishedAtUtc?.Offset);
        Assert.Equal(["Reporter"], article.Authors);
        Assert.Equal(["business"], article.ProviderCategories);
    }

    [Fact]
    public async Task Maps_rate_limit_and_retry_after_without_exposing_api_key()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(12));
        var provider = CreateProvider(new StubHandler(_ => response));

        var exception = await Assert.ThrowsAsync<NewsException>(() => provider.GetNewsAsync(Request()));

        Assert.Equal(NewsErrorCodes.RateLimited, exception.Code);
        Assert.True(exception.IsTransient);
        Assert.Equal(TimeSpan.FromSeconds(12), exception.RetryAfter);
        Assert.DoesNotContain("unit-test-api-key", exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, NewsErrorCodes.AuthenticationFailed, false)]
    [InlineData(HttpStatusCode.Forbidden, NewsErrorCodes.AuthenticationFailed, false)]
    [InlineData(HttpStatusCode.BadRequest, NewsErrorCodes.InvalidRequest, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, NewsErrorCodes.InvalidRequest, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, NewsErrorCodes.ProviderUnavailable, true)]
    public async Task Maps_provider_failures_to_safe_stable_errors(
        HttpStatusCode status,
        string expectedCode,
        bool transient)
    {
        var provider = CreateProvider(new StubHandler(_ => new HttpResponseMessage(status)));

        var exception = await Assert.ThrowsAsync<NewsException>(() => provider.GetNewsAsync(Request()));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(transient, exception.IsTransient);
        Assert.DoesNotContain("unit-test-api-key", exception.SafeMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_malformed_success_payload_safely()
    {
        var provider = CreateProvider(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Json("{not-json}")
        }));

        var exception = await Assert.ThrowsAsync<NewsException>(() => provider.GetNewsAsync(Request()));

        Assert.Equal(NewsErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public async Task Omits_paid_timeframe_parameter_by_default()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Json("{\"status\":\"success\",\"results\":[]}")
        });
        var provider = CreateProvider(handler);

        await provider.GetNewsAsync(Request());

        Assert.DoesNotContain("timeframe=", Assert.Single(handler.Requests).Query, StringComparison.Ordinal);
    }

    private static NewsDataProvider CreateProvider(
        HttpMessageHandler handler,
        bool useTimeframeParameter = false) => new(
        new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan },
        new NewsOptions
        {
            Enabled = true,
            Provider = "NewsData",
            ApiKey = "unit-test-api-key",
            BaseUrl = "https://newsdata.test/api/1/",
            TimeoutSeconds = 5,
            RateLimitPerMinute = 0,
            UseTimeframeParameter = useTimeframeParameter
        },
        new FixedTimeProvider(Now));

    private static NewsProviderRequest Request() => new(
        Now.AddHours(-1),
        Now,
        "gold OR Federal Reserve",
        "en",
        10,
        null,
        false);

    private static StringContent Json(string value) =>
        new(value, Encoding.UTF8, "application/json");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(response(request));
        }
    }
}
