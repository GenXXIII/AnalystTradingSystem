using XauAi.Application.News;

namespace XauAi.UnitTests.News;

public sealed class NewsRelevanceClassifierTests
{
    private readonly INewsRelevanceClassifier _classifier = new NewsRelevanceClassifier(new NewsSettings());

    [Theory]
    [InlineData("Gold climbs as investors seek safety", NewsRelevanceLevel.VeryHigh, "Gold")]
    [InlineData("Federal Reserve signals another interest rate decision", NewsRelevanceLevel.VeryHigh, "FederalReserve")]
    [InlineData("US CPI inflation cools in September", NewsRelevanceLevel.High, "Inflation")]
    [InlineData("Nonfarm payroll report surprises economists", NewsRelevanceLevel.High, "Employment")]
    [InlineData("Regional outlook changes", NewsRelevanceLevel.Medium, "Economy")]
    public void Classifies_supported_xauusd_topics_deterministically(
        string title,
        NewsRelevanceLevel expected,
        string expectedCategory)
    {
        var article = Article(title, description: title.Contains("Regional", StringComparison.Ordinal)
            ? "US economy and GDP growth remain uncertain."
            : null);

        var result = _classifier.Classify(article);

        Assert.Equal(expected, result.Relevance);
        Assert.Contains(expectedCategory, result.Categories);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public void General_provider_category_is_low_without_local_topic_match()
    {
        var result = _classifier.Classify(Article(
            "Local company opens another store",
            providerCategories: ["business"]));

        Assert.Equal(NewsRelevanceLevel.Low, result.Relevance);
        Assert.Empty(result.Categories);
        Assert.Contains("ProviderCategory:GeneralMacroContext", result.Reasons);
    }

    [Fact]
    public void Unrelated_article_is_irrelevant_and_does_not_emit_a_score_or_signal()
    {
        var result = _classifier.Classify(Article("Football club reveals its new uniform"));

        Assert.Equal(NewsRelevanceLevel.Irrelevant, result.Relevance);
        Assert.Empty(result.Categories);
        Assert.Empty(result.Entities);
    }

    private static ProviderNewsArticle Article(
        string title,
        string? description = null,
        IReadOnlyList<string>? providerCategories = null) => new(
        "test-id",
        title,
        description,
        null,
        "https://example.test/article",
        "Example Source",
        "https://example.test",
        [],
        DateTimeOffset.Parse("2026-10-02T06:00:00Z"),
        "en",
        ["us"],
        providerCategories ?? [],
        [],
        null);
}
