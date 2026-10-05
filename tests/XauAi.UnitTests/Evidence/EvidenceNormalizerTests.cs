using XauAi.Application.Evidence;

namespace XauAi.UnitTests.Evidence;

public sealed class EvidenceNormalizerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-04T10:00:00Z");
    private readonly EvidenceNormalizer _normalizer = new();

    [Theory]
    [InlineData("XAUUSD")]
    [InlineData("XAU/USD")]
    [InlineData("GOLD")]
    [InlineData("Gold")]
    [InlineData("GOLDUSD")]
    public void Gold_aliases_normalize_to_xauusd_without_losing_the_original(string symbol)
    {
        var result = _normalizer.Normalize(ValidInput(symbol: symbol), Now);

        Assert.True(result.IsValid);
        Assert.Equal("XAUUSD", result.Item!.CanonicalSymbol);
        Assert.Equal(symbol, result.Item.OriginalSymbol);
    }

    [Theory]
    [InlineData("BUY", EvidenceDirection.Bullish)]
    [InlineData("LONG", EvidenceDirection.Bullish)]
    [InlineData("SELL", EvidenceDirection.Bearish)]
    [InlineData("SHORT", EvidenceDirection.Bearish)]
    [InlineData("range", EvidenceDirection.Neutral)]
    [InlineData("may react", EvidenceDirection.Unknown)]
    [InlineData(null, EvidenceDirection.Unknown)]
    public void Direction_mapping_is_controlled_and_ambiguous_text_remains_unknown(
        string? value,
        EvidenceDirection expected)
    {
        var result = _normalizer.Normalize(ValidInput(direction: value), Now);

        Assert.Equal(expected, result.Item!.Direction);
        Assert.Equal(value, result.Item.OriginalDirection);
    }

    [Fact]
    public void Timestamp_roles_are_converted_to_utc_and_remain_distinct()
    {
        var result = _normalizer.Normalize(new EvidenceInput
        {
            EvidenceType = EvidenceType.News,
            SourceType = EvidenceSourceType.NewsProvider,
            SourceKey = "test-news",
            ExternalId = "item-1",
            Instrument = "XAU/USD",
            EventTime = DateTimeOffset.Parse("2026-10-04T15:00:00+07:00"),
            AvailableAt = DateTimeOffset.Parse("2026-10-04T15:00:00+07:00"),
            PublishedAt = DateTimeOffset.Parse("2026-10-04T15:00:00+07:00"),
            CollectedAt = DateTimeOffset.Parse("2026-10-04T15:03:00+07:00"),
            Title = "Gold test evidence"
        }, Now);

        Assert.Equal(DateTimeOffset.Parse("2026-10-04T08:00:00Z"), result.Item!.EventTimeUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-10-04T08:00:00Z"), result.Item.PublishedAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-10-04T08:03:00Z"), result.Item.CollectedAtUtc);
    }

    [Fact]
    public void Future_availability_and_invalid_metadata_are_quarantinable_errors()
    {
        var future = _normalizer.Normalize(ValidInput(availableAt: Now.AddMinutes(1)), Now);
        var malformed = _normalizer.Normalize(ValidInput(metadataJson: "{not-json"), Now);

        Assert.False(future.IsValid);
        Assert.Equal("FUTURE_AVAILABILITY", future.ErrorCode);
        Assert.False(malformed.IsValid);
        Assert.Equal("METADATA_INVALID", malformed.ErrorCode);
        Assert.All(new[] { future.PayloadHash, malformed.PayloadHash }, value => Assert.Equal(64, value.Length));
    }

    [Fact]
    public void Quality_uses_integrity_metadata_and_never_direction()
    {
        var bullish = _normalizer.Normalize(ValidInput(direction: "BUY"), Now).Item!;
        var bearish = _normalizer.Normalize(ValidInput(direction: "SELL"), Now).Item!;

        Assert.Equal(EvidenceQuality.High, bullish.Quality);
        Assert.Equal(bullish.Quality, bearish.Quality);
        Assert.Equal(EvidenceUnit.Percent, EvidenceNormalization.NormalizeUnit("%"));
        Assert.Equal(EvidenceCategory.Inflation, EvidenceNormalization.NormalizeCategory("CPI inflation"));
        Assert.Equal(EvidenceImportance.High, EvidenceNormalization.NormalizeImportance("3"));
    }

    private static EvidenceInput ValidInput(
        string symbol = "XAUUSD",
        string? direction = "BUY",
        DateTimeOffset? availableAt = null,
        string? metadataJson = "{\"test\":true}") => new()
    {
        EvidenceType = EvidenceType.Technical,
        SourceType = EvidenceSourceType.InternalTechnicalEngine,
        SourceKey = "test-technical",
        ExternalId = "test-observation-1",
        Instrument = symbol,
        OriginalInstrument = symbol,
        Timeframe = "15m",
        EventTime = Now.AddMinutes(-15),
        AvailableAt = availableAt ?? Now.AddMinutes(-1),
        Title = "Synthetic test-only technical observation",
        Direction = direction,
        Importance = "3",
        Category = "Technical",
        Unit = "%",
        MetadataJson = metadataJson,
        TimestampQuality = EvidenceTimestampQuality.Exact,
        SourceReliability = EvidenceSourceReliability.Known
    };
}
