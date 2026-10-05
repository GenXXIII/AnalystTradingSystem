using XauAi.Application.Analysts;

namespace XauAi.UnitTests.Analysts;

public sealed class AnalystItemNormalizerTests
{
    private static readonly DateTimeOffset CollectedAt =
        new(2026, 10, 4, 8, 3, 0, TimeSpan.Zero);

    [Fact]
    public void Normalization_maps_direction_target_horizon_timestamp_and_attribution()
    {
        var normalizer = CreateNormalizer();
        var item = Item(
            title: "Gold outlook: bullish toward $4,100 over 1 week",
            summary: "Synthetic test fixture: gold is bullish toward 4,100 over 1 week as lower-rate expectations increase.",
            publishedAt: new DateTimeOffset(2026, 10, 4, 15, 0, 0, TimeSpan.FromHours(7)));

        var result = normalizer.Normalize(item, CollectedAt);

        Assert.NotNull(result.Item);
        var normalized = result.Item;
        Assert.Null(result.RejectionReason);
        Assert.Equal(TimeSpan.Zero, normalized.PublishedAtUtc.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 8, 0, 0, TimeSpan.Zero), normalized.PublishedAtUtc);
        Assert.Equal("Synthetic Research", normalized.Source.Name);
        Assert.Equal("Test Analyst", normalized.Analyst?.Name);
        Assert.Equal(
            "https://research.example.test/gold-outlook?utm_source=test",
            normalized.SourceUrl);
        var prediction = Assert.Single(normalized.Predictions);
        Assert.Equal("XAUUSD", prediction.Instrument);
        Assert.Equal(AnalystDirection.Bullish, prediction.Direction);
        Assert.Equal(4100m, prediction.TargetPrice);
        Assert.Equal(1, prediction.HorizonValue);
        Assert.Equal(AnalystHorizonUnit.Weeks, prediction.HorizonUnit);
        Assert.Equal("USD", prediction.TargetCurrency);
    }

    [Fact]
    public void Normalization_preserves_unknowns_instead_of_inventing_target_or_analyst()
    {
        var normalizer = CreateNormalizer();
        var source = Item(
            title: "Gold expected to remain range-bound",
            summary: "Synthetic test fixture for a neutral gold view.",
            publishedAt: CollectedAt.AddMinutes(-3)) with
        {
            Analyst = null
        };

        var result = normalizer.Normalize(source, CollectedAt);

        Assert.NotNull(result.Item);
        var normalized = result.Item;
        Assert.Null(normalized.Analyst);
        var prediction = Assert.Single(normalized.Predictions);
        Assert.Equal(AnalystDirection.Neutral, prediction.Direction);
        Assert.Null(prediction.TargetPrice);
        Assert.Null(prediction.TargetRangeLow);
        Assert.Null(prediction.TargetRangeHigh);
        Assert.Equal(AnalystHorizonUnit.Unknown, prediction.HorizonUnit);
    }

    [Fact]
    public void Normalization_preserves_multiple_claims_under_one_publication()
    {
        var normalizer = CreateNormalizer();
        var item = Item(
            title: "Gold and dollar outlook",
            summary: "Synthetic XAUUSD and USD scenario.",
            publishedAt: CollectedAt.AddMinutes(-3)) with
        {
            Claims =
            [
                new ProviderAnalystClaim(
                    "gold", "Gold is bullish.", "XAUUSD", "Commodity", "Bullish",
                    4100m, null, null, "USD", 1, "Weeks", "1 week", null, "Lower rates", "Gold"),
                new ProviderAnalystClaim(
                    "usd", "USD is bearish.", "USD", "Currency", "Bearish",
                    null, null, null, null, 3, "Days", "3 days", null, "Rate cuts", "USD")
            ]
        };

        var result = normalizer.Normalize(item, CollectedAt);

        Assert.NotNull(result.Item);
        var predictions = result.Item.Predictions;
        Assert.Equal(2, predictions.Count);
        Assert.Contains(predictions, prediction => prediction.Instrument == "XAUUSD" && prediction.Direction == AnalystDirection.Bullish);
        Assert.Contains(predictions, prediction => prediction.Instrument == "USD" && prediction.Direction == AnalystDirection.Bearish);
    }

    [Fact]
    public void Normalization_rejects_missing_source_and_missing_timestamp()
    {
        var normalizer = CreateNormalizer();
        var missingSource = Item("Gold outlook", "Gold bullish", CollectedAt.AddMinutes(-3)) with
        {
            Source = new ProviderAnalystSource(null, null, null, null, null)
        };
        var missingTimestamp = Item("Gold outlook", "Gold bullish", CollectedAt.AddMinutes(-3)) with
        {
            PublishedAtUtc = null
        };

        Assert.Equal("MissingSource", normalizer.Normalize(missingSource, CollectedAt).RejectionReason);
        Assert.Equal("MissingPublishedTimestamp", normalizer.Normalize(missingTimestamp, CollectedAt).RejectionReason);
    }

    [Theory]
    [InlineData("Bullish", AnalystDirection.Bullish)]
    [InlineData("Bearish", AnalystDirection.Bearish)]
    [InlineData("Neutral", AnalystDirection.Neutral)]
    [InlineData("unstructured", AnalystDirection.Unknown)]
    public void Direction_mapping_is_controlled(string value, AnalystDirection expected)
    {
        Assert.Equal(expected, AnalystItemNormalizer.ParseDirection(value, string.Empty));
    }

    private static AnalystItemNormalizer CreateNormalizer()
    {
        var settings = new AnalystSettings();
        return new AnalystItemNormalizer(new AnalystRelevanceFilter(settings), settings);
    }

    private static ProviderAnalystItem Item(
        string title,
        string summary,
        DateTimeOffset publishedAt) =>
        new(
            "synthetic-item-1",
            title,
            summary,
            null,
            "https://research.example.test/gold-outlook?utm_source=test",
            publishedAt,
            null,
            "en-US",
            "Research",
            new ProviderAnalystSource(
                "synthetic-source",
                "Synthetic Research",
                "Research",
                "https://research.example.test",
                "US"),
            new ProviderAnalystIdentity(
                "synthetic-analyst",
                "Test Analyst",
                "Strategist",
                "https://research.example.test/analysts/test"),
            []);
}
