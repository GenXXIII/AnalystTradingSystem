using System.Net;
using System.Text;
using XauAi.Application.Analysts;
using XauAi.Infrastructure.Analysts.Rss;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.UnitTests.Analysts;

public sealed class RssAtomAnalystDataProviderTests
{
    [Fact]
    public async Task Rss_mapping_keeps_source_author_url_and_utc_publication_time()
    {
        const string xml = """
            <rss version="2.0" xmlns:dc="http://purl.org/dc/elements/1.1/">
              <channel>
                <title>Permitted Research Feed</title>
                <link>https://research.example.test/</link>
                <language>en</language>
                <item>
                  <guid>claim-42</guid>
                  <title>Gold expected higher over 2 weeks</title>
                  <description><![CDATA[Gold is bullish toward $4,100.]]></description>
                  <link>https://research.example.test/claim-42</link>
                  <pubDate>Sun, 04 Oct 2026 08:00:00 GMT</pubDate>
                  <dc:creator>Test Analyst</dc:creator>
                  <category>Gold</category>
                </item>
              </channel>
            </rss>
            """;
        var provider = CreateProvider(new StubHandler(_ => XmlResponse(xml)));

        var page = await provider.GetLatestAnalystItemsAsync(new AnalystProviderRequest(
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 5, 0, 0, 0, TimeSpan.Zero),
            10));

        var item = Assert.Single(page.Items);
        Assert.Equal("claim-42", item.ExternalId);
        Assert.Equal("Permitted Research Feed", item.Source.Name);
        Assert.Equal("Test Analyst", item.Analyst?.Name);
        Assert.Equal("https://research.example.test/claim-42", item.SourceUrl);
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 8, 0, 0, TimeSpan.Zero), item.PublishedAtUtc);
        Assert.Equal("Gold is bullish toward $4,100.", item.Summary);
        Assert.Null(item.PermittedContent);
    }

    [Fact]
    public async Task Provider_maps_rate_limit_without_exposing_response_content()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
        var provider = CreateProvider(new StubHandler(_ => response));

        var exception = await Assert.ThrowsAsync<AnalystException>(() => provider.GetLatestAnalystItemsAsync(Request()));

        Assert.Equal(AnalystErrorCodes.RateLimited, exception.Code);
        Assert.True(exception.IsTransient);
        Assert.Equal(TimeSpan.FromSeconds(7), exception.RetryAfter);
    }

    [Fact]
    public async Task Provider_rejects_malformed_xml()
    {
        var provider = CreateProvider(new StubHandler(_ => XmlResponse("<rss><channel>")));

        var exception = await Assert.ThrowsAsync<AnalystException>(() => provider.GetLatestAnalystItemsAsync(Request()));

        Assert.Equal(AnalystErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public async Task Provider_maps_timeout_as_transient()
    {
        var provider = CreateProvider(new CancelingHandler());

        var exception = await Assert.ThrowsAsync<AnalystException>(() => provider.GetLatestAnalystItemsAsync(Request()));

        Assert.Equal(AnalystErrorCodes.Timeout, exception.Code);
        Assert.True(exception.IsTransient);
    }

    private static RssAtomAnalystDataProvider CreateProvider(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            new AnalystOptions
            {
                Enabled = true,
                Provider = "RssAtom",
                SourceType = AnalystSourceType.RssFeed,
                BaseUrl = "https://research.example.test/feed.xml",
                TimeoutSeconds = 1
            },
            TimeProvider.System);

    private static AnalystProviderRequest Request() => new(
        DateTimeOffset.UtcNow.AddDays(-1),
        DateTimeOffset.UtcNow.AddMinutes(1),
        10);

    private static HttpResponseMessage XmlResponse(string xml) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(xml, Encoding.UTF8, "application/xml")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response(request));
    }

    private sealed class CancelingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true));
    }
}
