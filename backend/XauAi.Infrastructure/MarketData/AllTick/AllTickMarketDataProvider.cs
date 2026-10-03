using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.AllTick;

internal sealed class AllTickMarketDataProvider(
    AllTickOptions options,
    AllTickHttpClient httpClient,
    AllTickRealtimeState realtimeState,
    TimeProvider timeProvider,
    ILogger<AllTickMarketDataProvider> logger) : IMarketDataProvider, ILatestMarketCandleProvider
{
    private const string ProviderName = "AllTick";

    public Task<MarketDataProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (!options.Enabled)
        {
            return Task.FromResult(Status(
                MarketDataProviderState.Disabled,
                enabled: false,
                connected: false,
                "AllTick integration is disabled.",
                now));
        }

        var realtime = realtimeState.Get();
        var fresh = realtime.LastMessageAtUtc is { } observed
            && now - observed <= TimeSpan.FromSeconds(options.QuoteMaxAgeSeconds);
        return Task.FromResult(Status(
            realtime.Connected && fresh ? MarketDataProviderState.Connected : MarketDataProviderState.Available,
            enabled: true,
            connected: realtime.Connected && fresh,
            fresh ? realtime.Message : "AllTick is configured; the live stream is waiting for a fresh market update.",
            now));
    }

    public Task<MarketQuote> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRun(symbol);
        var snapshot = realtimeState.Get();
        var now = timeProvider.GetUtcNow();
        if (snapshot.Quote is null
            || now - snapshot.Quote.TimestampUtc > TimeSpan.FromSeconds(options.QuoteMaxAgeSeconds))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ProviderUnavailable,
                "A fresh AllTick quote is not available yet.");
        }

        return Task.FromResult(snapshot.Quote);
    }

    public async Task<IReadOnlyList<MarketCandleSnapshot>> GetCandlesAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRun(symbol);
        MarketDataRequestValidation.Validate(symbol, fromUtc, toUtc);
        logger.LogInformation(
            "AllTick historical candle request started for {Symbol} {Timeframe} from {FromUtc} to {ToUtc}",
            symbol,
            timeframe.Code(),
            fromUtc,
            toUtc);
        var values = await httpClient.GetCandlesAsync(
            timeframe,
            fromUtc.ToUniversalTime(),
            toUtc.ToUniversalTime(),
            cancellationToken);
        return Normalize(values);
    }

    public async Task<IReadOnlyList<MarketCandleSnapshot>> GetLatestCandlesAsync(
        string symbol,
        IReadOnlyCollection<MarketTimeframe> timeframes,
        CancellationToken cancellationToken = default)
    {
        EnsureCanRun(symbol);
        if (timeframes.Count == 0 || timeframes.Count > 10)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                "AllTick batch reconciliation requires between 1 and 10 timeframes.");
        }

        return Normalize(await httpClient.GetLatestCandlesAsync(timeframes, cancellationToken));
    }

    private IReadOnlyList<MarketCandleSnapshot> Normalize(IReadOnlyList<AllTickKline> values)
    {
        var fetchedAtUtc = timeProvider.GetUtcNow();
        var snapshots = new List<MarketCandleSnapshot>(values.Count);
        foreach (var value in values)
        {
            if (value.Open <= 0 || value.High < value.Low
                || value.Open < value.Low || value.Open > value.High
                || value.Close < value.Low || value.Close > value.High
                || value.Volume < 0)
            {
                throw new MarketDataException(
                    MarketDataErrorCodes.ProviderDataRequestFailed,
                    "AllTick returned an invalid candle.");
            }

            var closeTime = value.OpenTimeUtc.Add(value.Timeframe.Duration());
            snapshots.Add(new MarketCandleSnapshot(
                options.ApplicationSymbol,
                options.Symbol,
                value.Timeframe,
                value.OpenTimeUtc,
                closeTime,
                value.Open,
                value.High,
                value.Low,
                value.Close,
                value.Volume,
                null,
                null,
                closeTime <= fetchedAtUtc,
                "UTC",
                fetchedAtUtc,
                "alltick"));
        }

        return snapshots
            .OrderBy(candle => candle.Timeframe)
            .ThenBy(candle => candle.OpenTimeUtc)
            .ToArray();
    }

    private void EnsureCanRun(string symbol)
    {
        if (!options.Enabled)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ProviderDisabled,
                "AllTick integration is disabled.");
        }

        if (!string.Equals(symbol, options.ApplicationSymbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ProviderSymbolNotFound,
                "The requested market symbol is not configured.");
        }
    }

    private MarketDataProviderStatus Status(
        MarketDataProviderState state,
        bool enabled,
        bool connected,
        string message,
        DateTimeOffset checkedAtUtc) =>
        new(
            ProviderName,
            state,
            enabled,
            enabled,
            connected,
            options.ApplicationSymbol,
            options.Symbol,
            null,
            new Uri(options.HttpBaseUrl).Host,
            message,
            checkedAtUtc);
}
