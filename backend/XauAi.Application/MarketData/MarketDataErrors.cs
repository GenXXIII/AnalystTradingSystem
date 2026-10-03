namespace XauAi.Application.MarketData;

public static class MarketDataErrorCodes
{
    public const string Disabled = "MT5_DISABLED";
    public const string ConfigurationInvalid = "MT5_CONFIGURATION_INVALID";
    public const string TerminalNotFound = "MT5_TERMINAL_NOT_FOUND";
    public const string InitializationFailed = "MT5_INITIALIZATION_FAILED";
    public const string ConnectionFailed = "MT5_CONNECTION_FAILED";
    public const string AuthenticationFailed = "MT5_AUTHENTICATION_FAILED";
    public const string SymbolNotFound = "MT5_SYMBOL_NOT_FOUND";
    public const string DataRequestFailed = "MT5_DATA_REQUEST_FAILED";
    public const string Timeout = "MT5_TIMEOUT";
    public const string Unavailable = "MT5_UNAVAILABLE";
    public const string InvalidRequest = "MARKET_DATA_REQUEST_INVALID";
    public const string DatabaseDisabled = "DATABASE_DISABLED";
    public const string QueryLimitExceeded = "MARKET_DATA_LIMIT_EXCEEDED";
    public const string QueryRangeTooLarge = "MARKET_DATA_RANGE_TOO_LARGE";
    public const string SynchronizationFailed = "MARKET_DATA_SYNC_FAILED";
}

public sealed class MarketDataException(
    string code,
    string safeMessage,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;

    public string SafeMessage { get; } = safeMessage;
}
