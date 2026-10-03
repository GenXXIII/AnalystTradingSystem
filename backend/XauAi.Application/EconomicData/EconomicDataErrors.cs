namespace XauAi.Application.EconomicData;

public static class EconomicDataErrorCodes
{
    public const string Disabled = "ECONOMIC_DATA_DISABLED";
    public const string InvalidRequest = "ECONOMIC_DATA_REQUEST_INVALID";
    public const string SeriesNotFound = "ECONOMIC_DATA_SERIES_NOT_FOUND";
    public const string DatabaseDisabled = "ECONOMIC_DATA_DATABASE_DISABLED";
    public const string AuthenticationFailed = "ECONOMIC_DATA_PROVIDER_AUTHENTICATION_FAILED";
    public const string RateLimited = "ECONOMIC_DATA_PROVIDER_RATE_LIMITED";
    public const string Timeout = "ECONOMIC_DATA_PROVIDER_TIMEOUT";
    public const string InvalidResponse = "ECONOMIC_DATA_PROVIDER_RESPONSE_INVALID";
    public const string ProviderUnavailable = "ECONOMIC_DATA_PROVIDER_UNAVAILABLE";
    public const string SyncFailed = "ECONOMIC_DATA_SYNC_FAILED";
}

public sealed class EconomicDataException(
    string code,
    string safeMessage,
    bool transient = false,
    TimeSpan? retryAfter = null,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;

    public string SafeMessage { get; } = safeMessage;

    public bool IsTransient { get; } = transient;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}
