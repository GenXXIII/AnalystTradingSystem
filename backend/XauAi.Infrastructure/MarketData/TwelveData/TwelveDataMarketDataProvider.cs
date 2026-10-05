using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.TwelveData;

internal sealed class TwelveDataMarketDataProvider(
    TwelveDataOptions options,
    TwelveDataHttpClient httpClient,
    TimeProvider timeProvider,
    ILogger<TwelveDataMarketDataProvider> logger) : IReferenceMarketDataProvider
{
    public Task<MarketDataProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var enabled = options.Enabled;
        return Task.FromResult(new MarketDataProviderStatus(
            "Twelve Data",
            enabled ? MarketDataProviderState.Available : MarketDataProviderState.Disabled,
            enabled,
            Connected: false,
            options.ApplicationSymbol,
            options.Symbol,
            new Uri(options.BaseUrl).Host,
            enabled
                ? "Twelve Data is configured as the historical and reference source."
                : "Twelve Data integration is disabled.",
            timeProvider.GetUtcNow()));
    }

    public Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default) =>
        throw new MarketDataException(
            MarketDataErrorCodes.ProviderUnavailable,
            "Twelve Data is configured for candle history and reference checks, not live bid/ask quotes.");

    public async Task<IReadOnlyList<MarketCandleSnapshot>> GetCandlesAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRun(symbol);
        MarketDataRequestValidation.Validate(symbol, fromUtc, toUtc);
        var estimatedPoints = (long)Math.Ceiling(
            (toUtc.ToUniversalTime() - fromUtc.ToUniversalTime()).TotalSeconds
            / timeframe.Duration().TotalSeconds) + 1;
        if (estimatedPoints > options.MaximumPointsPerRequest)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.QueryLimitExceeded,
                $"A Twelve Data request cannot exceed {options.MaximumPointsPerRequest} expected candles.");
        }

        logger.LogInformation(
            "Twelve Data candle request started for {Symbol} {Timeframe} from {FromUtc} to {ToUtc}",
            symbol,
            timeframe.Code(),
            fromUtc,
            toUtc);
        var values = await httpClient.GetCandlesAsync(
            timeframe,
            fromUtc.ToUniversalTime(),
            toUtc.ToUniversalTime(),
            cancellationToken);
        var fetchedAtUtc = timeProvider.GetUtcNow();
        return [.. values.Select(value => Normalize(value, timeframe, fetchedAtUtc))];
    }

    private MarketCandleSnapshot Normalize(
        TwelveDataCandle value,
        MarketTimeframe timeframe,
        DateTimeOffset fetchedAtUtc)
    {
        if (value.Open <= 0 || value.High < value.Low
            || value.Open < value.Low || value.Open > value.High
            || value.Close < value.Low || value.Close > value.High
            || value.Volume < 0)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ProviderDataRequestFailed,
                "Twelve Data returned an invalid candle.");
        }

        var closeTimeUtc = value.OpenTimeUtc.Add(timeframe.Duration());
        return new MarketCandleSnapshot(
            options.ApplicationSymbol,
            options.Symbol,
            timeframe,
            value.OpenTimeUtc,
            closeTimeUtc,
            value.Open,
            value.High,
            value.Low,
            value.Close,
            value.Volume,
            null,
            null,
            closeTimeUtc <= fetchedAtUtc,
            "UTC",
            fetchedAtUtc,
            options.ProviderKey);
    }

    private void EnsureCanRun(string symbol)
    {
        if (!options.Enabled)
        {
            throw new MarketDataException(MarketDataErrorCodes.ProviderDisabled, "Twelve Data integration is disabled.");
        }

        if (!string.Equals(symbol, options.ApplicationSymbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ProviderSymbolNotFound,
                "The requested market symbol is not configured.");
        }
    }
}
