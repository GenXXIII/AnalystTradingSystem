using XauAi.Application.News;

namespace XauAi.UnitTests.News;

public sealed class NewsArticleNormalizerTests
{
    private readonly INewsArticleNormalizer _normalizer = new NewsArticleNormalizer(
        new NewsRelevanceClassifier(new NewsSettings()));
    private readonly DateTimeOffset _collectedAt = DateTimeOffset.Parse("2026-10-02T07:00:00Z");

    [Fact]
    public void Normalizes_utc_url_metadata_and_categories()
    {
        var sourcePublishedAt = new DateTimeOffset(2026, 10, 2, 13, 30, 0, TimeSpan.FromHours(7));
        var result = _normalizer.Normalize(Article(
            title: "  Gold   rises on USD weakness  ",
            url: "HTTPS://Example.Test:443/news/gold?utm_source=feed&b=2&a=1#top",
            publishedAt: sourcePublishedAt,
            language: "English",
            countries: ["us", "US", " gb "],
            categories: [" business ", "business"]), _collectedAt);

        var normalized = Assert.IsType<NormalizedNewsArticle>(result.Article);
        Assert.Equal("Gold rises on USD weakness", normalized.Title);
        Assert.Equal("https://example.test/news/gold?a=1&b=2", normalized.CanonicalUrl);
        Assert.Equal(TimeSpan.Zero, normalized.PublishedAtUtc.Offset);
        Assert.Equal(sourcePublishedAt.UtcDateTime, normalized.PublishedAtUtc.UtcDateTime);
        Assert.Equal("en", normalized.Language);
        Assert.Equal(["GB", "US"], normalized.CountryCodes);
        Assert.Equal(["business"], normalized.ProviderCategories);
        Assert.Equal(NewsRelevanceLevel.VeryHigh, normalized.Relevance);
        Assert.Contains("Gold", normalized.Categories);
        Assert.Null(result.RejectionReason);
    }

    [Theory]
    [InlineData(null, "Example Source", "https://example.test/a", "MissingTitle")]
    [InlineData("Gold", null, "https://example.test/a", "MissingSource")]
    [InlineData("Gold", "Example Source", "not-a-url", "InvalidArticleUrl")]
    public void Rejects_incomplete_provider_records(
        string? title,
        string? source,
        string? url,
        string expectedReason)
    {
        var result = _normalizer.Normalize(Article(title, url, source: source), _collectedAt);

        Assert.Null(result.Article);
        Assert.Equal(expectedReason, result.RejectionReason);
    }

    [Fact]
    public void Same_source_title_and_utc_date_share_fingerprint_but_other_sources_do_not()
    {
        var first = _normalizer.Normalize(Article(
            "Gold: rises!",
            "https://first.test/story?utm_source=a",
            source: "Wire One"), _collectedAt).Article!;
        var punctuationVariant = _normalizer.Normalize(Article(
            "GOLD rises",
            "https://second.test/story",
            source: "wire one"), _collectedAt).Article!;
        var independentSource = _normalizer.Normalize(Article(
            "Gold rises",
            "https://third.test/story",
            source: "Wire Two"), _collectedAt).Article!;

        Assert.Equal(first.ContentHash, punctuationVariant.ContentHash);
        Assert.NotEqual(first.ContentHash, independentSource.ContentHash);
    }

    [Fact]
    public void Rejects_future_and_missing_timestamps()
    {
        var missing = _normalizer.Normalize(Article("Gold") with { PublishedAtUtc = null }, _collectedAt);
        var future = _normalizer.Normalize(
            Article("Gold", publishedAt: _collectedAt.AddMinutes(11)),
            _collectedAt);

        Assert.Equal("MissingPublishedTimestamp", missing.RejectionReason);
        Assert.Equal("FuturePublishedTimestamp", future.RejectionReason);
    }

    private static ProviderNewsArticle Article(
        string? title,
        string? url = "https://example.test/article",
        string? source = "Example Source",
        DateTimeOffset? publishedAt = default,
        string? language = "en",
        IReadOnlyList<string>? countries = null,
        IReadOnlyList<string>? categories = null) => new(
        "provider-id",
        title,
        "The Federal Reserve and gold markets are in focus.",
        null,
        url,
        source,
        "https://example.test",
        ["Reporter", "Reporter"],
        publishedAt == default ? DateTimeOffset.Parse("2026-10-02T06:30:00Z") : publishedAt,
        language,
        countries ?? ["us"],
        categories ?? ["business"],
        ["gold"],
        null);
}
