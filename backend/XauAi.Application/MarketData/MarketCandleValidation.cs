namespace XauAi.Application.MarketData;

public static class MarketCandleValidation
{
    public static CandleValidationOutcome ValidateAndNormalize(
        MarketCandleSnapshot candle,
        string expectedSymbol,
        MarketTimeframe expectedTimeframe,
        DateTimeOffset requestedFromUtc,
        DateTimeOffset requestedToUtc,
        DateTimeOffset observedAtUtc)
    {
        var openTimeUtc = candle.OpenTimeUtc.ToUniversalTime();
        var closeTimeUtc = candle.CloseTimeUtc.ToUniversalTime();
        var fetchedAtUtc = candle.FetchedAtUtc.ToUniversalTime();
        var normalizedFromUtc = requestedFromUtc.ToUniversalTime();
        var normalizedToUtc = requestedToUtc.ToUniversalTime();
        var normalizedObservedAtUtc = observedAtUtc.ToUniversalTime();
        var duration = expectedTimeframe.Duration();

        if (!string.Equals(candle.Symbol, expectedSymbol, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(candle.ProviderSymbol))
        {
            return Invalid("SYMBOL_MISMATCH");
        }

        if (candle.Timeframe != expectedTimeframe)
        {
            return Invalid("TIMEFRAME_MISMATCH");
        }

        if (openTimeUtc < normalizedFromUtc || openTimeUtc > normalizedToUtc)
        {
            return Invalid("TIMESTAMP_OUTSIDE_REQUEST");
        }

        if (closeTimeUtc != openTimeUtc.Add(duration))
        {
            return Invalid("CLOSE_TIME_INVALID");
        }

        var durationSeconds = checked((long)duration.TotalSeconds);
        if (openTimeUtc.ToUnixTimeSeconds() % durationSeconds != 0)
        {
            return Invalid("TIMESTAMP_NOT_ALIGNED");
        }

        if (candle.Open <= 0 || candle.High <= 0 || candle.Low <= 0 || candle.Close <= 0)
        {
            return Invalid("PRICE_NOT_POSITIVE");
        }

        if (candle.High < candle.Open
            || candle.High < candle.Close
            || candle.High < candle.Low
            || candle.Low > candle.Open
            || candle.Low > candle.Close
            || candle.Low > candle.High)
        {
            return Invalid("OHLC_RELATIONSHIP_INVALID");
        }

        if (candle.TickVolume < 0 || candle.RealVolume < 0 || candle.Spread < 0)
        {
            return Invalid("MEASUREMENT_NEGATIVE");
        }

        return new CandleValidationOutcome(
            true,
            candle with
            {
                Symbol = expectedSymbol,
                OpenTimeUtc = openTimeUtc,
                CloseTimeUtc = closeTimeUtc,
                IsComplete = closeTimeUtc <= normalizedObservedAtUtc,
                SourceTimeZone = "UTC",
                FetchedAtUtc = fetchedAtUtc
            },
            null);
    }

    private static CandleValidationOutcome Invalid(string code) => new(false, null, code);
}
