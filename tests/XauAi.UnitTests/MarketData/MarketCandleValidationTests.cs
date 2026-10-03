using XauAi.Application.MarketData;

namespace XauAi.UnitTests.MarketData;

public sealed class MarketCandleValidationTests
{
    private static readonly DateTimeOffset FromUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Valid_candle_is_normalized_to_utc_and_completion_is_derived()
    {
        var candle = ValidCandle() with
        {
            OpenTimeUtc = FromUtc.ToOffset(TimeSpan.FromHours(7)),
            CloseTimeUtc = FromUtc.AddHours(1).ToOffset(TimeSpan.FromHours(7)),
            IsComplete = false
        };

        var result = MarketCandleValidation.ValidateAndNormalize(
            candle,
            "XAUUSD",
            MarketTimeframe.H1,
            FromUtc,
            FromUtc.AddHours(4),
            FromUtc.AddHours(2));

        Assert.True(result.IsValid);
        Assert.Equal(TimeSpan.Zero, result.Candle!.OpenTimeUtc.Offset);
        Assert.True(result.Candle.IsComplete);
        Assert.Equal("UTC", result.Candle.SourceTimeZone);
    }

    [Theory]
    [InlineData(0, 3, 1, 2, "PRICE_NOT_POSITIVE")]
    [InlineData(2, 1, 3, 2, "OHLC_RELATIONSHIP_INVALID")]
    [InlineData(2, 3, 2.5, 4, "OHLC_RELATIONSHIP_INVALID")]
    public void Invalid_ohlc_is_rejected_without_modification(
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        string expectedCode)
    {
        var candle = ValidCandle() with { Open = open, High = high, Low = low, Close = close };

        var result = MarketCandleValidation.ValidateAndNormalize(
            candle,
            "XAUUSD",
            MarketTimeframe.H1,
            FromUtc,
            FromUtc.AddHours(4),
            FromUtc.AddHours(2));

        Assert.False(result.IsValid);
        Assert.Null(result.Candle);
        Assert.Equal(expectedCode, result.ErrorCode);
    }

    [Fact]
    public void Misaligned_timestamp_is_rejected_not_rounded()
    {
        var candle = ValidCandle() with
        {
            OpenTimeUtc = FromUtc.AddMinutes(7),
            CloseTimeUtc = FromUtc.AddHours(1).AddMinutes(7)
        };

        var result = MarketCandleValidation.ValidateAndNormalize(
            candle,
            "XAUUSD",
            MarketTimeframe.H1,
            FromUtc,
            FromUtc.AddHours(4),
            FromUtc.AddHours(2));

        Assert.Equal("TIMESTAMP_NOT_ALIGNED", result.ErrorCode);
    }

    [Fact]
    public void Candle_outside_requested_range_is_rejected()
    {
        var result = MarketCandleValidation.ValidateAndNormalize(
            ValidCandle() with
            {
                OpenTimeUtc = FromUtc.AddHours(-1),
                CloseTimeUtc = FromUtc
            },
            "XAUUSD",
            MarketTimeframe.H1,
            FromUtc,
            FromUtc.AddHours(4),
            FromUtc.AddHours(2));

        Assert.Equal("TIMESTAMP_OUTSIDE_REQUEST", result.ErrorCode);
    }

    private static MarketCandleSnapshot ValidCandle() =>
        new(
            "XAUUSD",
            "XAUUSD.test",
            MarketTimeframe.H1,
            FromUtc,
            FromUtc.AddHours(1),
            2,
            3,
            1,
            2.5m,
            100,
            0,
            10,
            true,
            "UTC",
            FromUtc.AddHours(2));
}
