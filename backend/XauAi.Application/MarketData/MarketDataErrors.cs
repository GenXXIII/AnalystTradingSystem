namespace XauAi.Application.MarketData;

public static class MarketDataErrorCodes
{
    public const string ProviderDisabled = "MARKET_DATA_PROVIDER_DISABLED";
    public const string ProviderConfigurationInvalid = "MARKET_DATA_PROVIDER_CONFIGURATION_INVALID";
    public const string ProviderConnectionFailed = "MARKET_DATA_PROVIDER_CONNECTION_FAILED";
    public const string ProviderAuthenticationFailed = "MARKET_DATA_PROVIDER_AUTHENTICATION_FAILED";
    public const string ProviderSymbolNotFound = "MARKET_DATA_PROVIDER_SYMBOL_NOT_FOUND";
    public const string ProviderDataRequestFailed = "MARKET_DATA_PROVIDER_REQUEST_FAILED";
    public const string ProviderRateLimited = "MARKET_DATA_PROVIDER_RATE_LIMITED";
    public const string ProviderTimeout = "MARKET_DATA_PROVIDER_TIMEOUT";
    public const string ProviderUnavailable = "MARKET_DATA_PROVIDER_UNAVAILABLE";

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
