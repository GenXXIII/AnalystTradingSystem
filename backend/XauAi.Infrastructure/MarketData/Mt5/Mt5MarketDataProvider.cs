using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.Mt5;

internal sealed class Mt5MarketDataProvider(
    IOptions<Mt5Options> options,
    IMt5BridgeClient bridgeClient,
    IMt5HostEnvironment hostEnvironment,
    TimeProvider timeProvider,
    ILogger<Mt5MarketDataProvider> logger) : IMarketDataProvider
{
    private const string ProviderName = "MetaTrader 5";
    private readonly Mt5Options _options = options.Value;

    public async Task<MarketDataProviderStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = timeProvider.GetUtcNow();
        if (!_options.Enabled)
        {
            return Status(
                MarketDataProviderState.Disabled,
                enabled: false,
                terminalAvailable: false,
                connected: false,
                "MT5 integration is disabled.",
                checkedAtUtc);
        }

        if (!hostEnvironment.IsWindows)
        {
            return Status(
                MarketDataProviderState.ConfigurationError,
                enabled: true,
                terminalAvailable: false,
                connected: false,
                "MT5 Desktop integration requires a Windows host.",
                checkedAtUtc);
        }

        if (!hostEnvironment.FileExists(_options.TerminalPath))
        {
            return Status(
                MarketDataProviderState.TerminalNotFound,
                enabled: true,
                terminalAvailable: false,
                connected: false,
                "The configured MT5 terminal was not found.",
                checkedAtUtc);
        }

        try
        {
            var response = await bridgeClient.ExecuteAsync(CreateRequest("status"), cancellationToken);
            var bridgeStatus = response.Status
                ?? throw new Mt5BridgeException(
                    MarketDataErrorCodes.InitializationFailed,
                    "The MT5 status response was incomplete.");
            var state = bridgeStatus.Connected
                ? MarketDataProviderState.Connected
                : MarketDataProviderState.Disconnected;

            return new MarketDataProviderStatus(
                ProviderName,
                state,
                true,
                true,
                bridgeStatus.Connected,
                _options.ApplicationSymbol,
                _options.Symbol,
                MaskLogin(bridgeStatus.Login),
                bridgeStatus.Server,
                bridgeStatus.Connected
                    ? "MT5 is connected."
                    : "The MT5 terminal is available but disconnected.",
                checkedAtUtc);
        }
        catch (Mt5BridgeException exception)
        {
            logger.LogWarning(
                exception,
                "MT5 status check failed with code {ErrorCode}",
                exception.Code);
            return Status(
                MapState(exception.Code),
                enabled: true,
                terminalAvailable: true,
                connected: false,
                SafeMessage(exception.Code),
                checkedAtUtc);
        }
    }

    public async Task<MarketQuote> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        EnsureRequestCanRun(symbol);
        try
        {
            var response = await bridgeClient.ExecuteAsync(
                CreateRequest("quote", symbol: _options.Symbol),
                cancellationToken);
            var quote = response.Quote
                ?? throw new Mt5BridgeException(
                    MarketDataErrorCodes.DataRequestFailed,
                    "The MT5 quote response was incomplete.");

            if (quote.Bid < 0 || quote.Ask < 0 || quote.Ask < quote.Bid)
            {
                throw new Mt5BridgeException(
                    MarketDataErrorCodes.DataRequestFailed,
                    "The MT5 quote response contained invalid prices.");
            }

            return new MarketQuote(
                _options.ApplicationSymbol,
                quote.Symbol,
                quote.Bid,
                quote.Ask,
                DateTimeOffset.FromUnixTimeMilliseconds(quote.TimestampMilliseconds));
        }
        catch (Mt5BridgeException exception)
        {
            throw ToApplicationException(exception);
        }
    }

    public async Task<IReadOnlyList<MarketCandleSnapshot>> GetCandlesAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureRequestCanRun(symbol);
        MarketDataRequestValidation.Validate(symbol, fromUtc, toUtc);

        var normalizedFrom = fromUtc.ToUniversalTime();
        var normalizedTo = toUtc.ToUniversalTime();
        var estimatedBars = ((normalizedTo - normalizedFrom).Ticks / timeframe.Duration().Ticks) + 1;
        if (estimatedBars > _options.MaxBarsPerRequest)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                $"The candle request exceeds the {_options.MaxBarsPerRequest} bar limit.");
        }

        logger.LogInformation(
            "MT5 historical data request started for {Symbol} {Timeframe} from {FromUtc} to {ToUtc}",
            _options.ApplicationSymbol,
            timeframe.Code(),
            normalizedFrom,
            normalizedTo);

        try
        {
            var response = await bridgeClient.ExecuteAsync(
                CreateRequest(
                    "candles",
                    _options.Symbol,
                    timeframe.Code(),
                    normalizedFrom.ToUnixTimeSeconds(),
                    normalizedTo.ToUnixTimeSeconds()),
                cancellationToken);
            var fetchedAtUtc = timeProvider.GetUtcNow();
            var candles = response.Candles
                .Select(candle => Normalize(candle, timeframe, fetchedAtUtc))
                .Where(candle => candle.OpenTimeUtc >= normalizedFrom && candle.OpenTimeUtc <= normalizedTo)
                .OrderBy(candle => candle.OpenTimeUtc)
                .DistinctBy(candle => candle.OpenTimeUtc)
                .ToArray();

            logger.LogInformation(
                "MT5 historical data request completed with {CandleCount} candles for {Symbol} {Timeframe}",
                candles.Length,
                _options.ApplicationSymbol,
                timeframe.Code());
            return candles;
        }
        catch (Mt5BridgeException exception)
        {
            logger.LogWarning(
                exception,
                "MT5 historical data request failed with code {ErrorCode} for {Symbol} {Timeframe}",
                exception.Code,
                _options.ApplicationSymbol,
                timeframe.Code());
            throw ToApplicationException(exception);
        }
    }

    private MarketCandleSnapshot Normalize(
        Mt5BridgeCandle candle,
        MarketTimeframe requestedTimeframe,
        DateTimeOffset fetchedAtUtc)
    {
        if (!MarketTimeframes.TryParse(candle.Timeframe, out var timeframe)
            || timeframe != requestedTimeframe
            || candle.High < candle.Low
            || candle.Open < candle.Low
            || candle.Open > candle.High
            || candle.Close < candle.Low
            || candle.Close > candle.High
            || candle.TickVolume < 0
            || candle.RealVolume < 0
            || candle.Spread < 0)
        {
            throw new Mt5BridgeException(
                MarketDataErrorCodes.DataRequestFailed,
                "MT5 returned an invalid candle.");
        }

        var openTime = DateTimeOffset.FromUnixTimeSeconds(candle.OpenTimeSeconds);
        return new MarketCandleSnapshot(
            _options.ApplicationSymbol,
            candle.Symbol,
            timeframe,
            openTime,
            openTime.Add(timeframe.Duration()),
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.TickVolume,
            candle.RealVolume,
            candle.Spread,
            candle.IsComplete,
            "UTC",
            fetchedAtUtc,
            "mt5");
    }

    private void EnsureRequestCanRun(string symbol)
    {
        if (!_options.Enabled)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.Disabled,
                "MT5 integration is disabled.");
        }

        if (!hostEnvironment.IsWindows)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ConfigurationInvalid,
                "MT5 Desktop integration requires a Windows host.");
        }

        if (!hostEnvironment.FileExists(_options.TerminalPath))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.TerminalNotFound,
                "The configured MT5 terminal was not found.");
        }

        if (!string.Equals(symbol, _options.ApplicationSymbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.SymbolNotFound,
                "The requested market symbol is not configured.");
        }
    }

    private Mt5BridgeRequest CreateRequest(
        string operation,
        string? symbol = null,
        string? timeframe = null,
        long? fromUnixSeconds = null,
        long? toUnixSeconds = null) =>
        new(
            operation,
            _options.TerminalPath,
            long.Parse(_options.Login, System.Globalization.CultureInfo.InvariantCulture),
            _options.Password,
            _options.Server,
            checked(_options.ConnectionTimeoutSeconds * 1000),
            symbol,
            timeframe,
            fromUnixSeconds,
            toUnixSeconds,
            _options.MaxBarsPerRequest);

    private MarketDataProviderStatus Status(
        MarketDataProviderState state,
        bool enabled,
        bool terminalAvailable,
        bool connected,
        string message,
        DateTimeOffset checkedAtUtc) =>
        new(
            ProviderName,
            state,
            enabled,
            terminalAvailable,
            connected,
            _options.ApplicationSymbol,
            _options.Symbol,
            null,
            null,
            message,
            checkedAtUtc);

    private static MarketDataProviderState MapState(string code) => code switch
    {
        MarketDataErrorCodes.ConfigurationInvalid => MarketDataProviderState.ConfigurationError,
        MarketDataErrorCodes.TerminalNotFound => MarketDataProviderState.TerminalNotFound,
        MarketDataErrorCodes.AuthenticationFailed => MarketDataProviderState.AuthenticationFailed,
        MarketDataErrorCodes.ConnectionFailed or MarketDataErrorCodes.InitializationFailed or MarketDataErrorCodes.Timeout =>
            MarketDataProviderState.ConnectionFailed,
        _ => MarketDataProviderState.Disconnected
    };

    private static string SafeMessage(string code) => code switch
    {
        MarketDataErrorCodes.ConfigurationInvalid => "MT5 configuration is invalid or incomplete.",
        MarketDataErrorCodes.TerminalNotFound => "The configured MT5 terminal was not found.",
        MarketDataErrorCodes.AuthenticationFailed => "MT5 authentication failed.",
        MarketDataErrorCodes.Timeout => "The MT5 request timed out.",
        MarketDataErrorCodes.InitializationFailed => "The MT5 terminal could not be initialized.",
        _ => "The MT5 connection is unavailable."
    };

    private static MarketDataException ToApplicationException(Mt5BridgeException exception) =>
        new(exception.Code, SafeMessage(exception.Code), exception);

    private static string? MaskLogin(long? login)
    {
        if (login is null)
        {
            return null;
        }

        var value = login.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"****{value[^Math.Min(4, value.Length)..]}";
    }
}
