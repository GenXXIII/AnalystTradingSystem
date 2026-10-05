namespace XauAi.Application.Analysts;

public static class AnalystErrorCodes
{
    public const string Disabled = "ANALYST_DATA_DISABLED";
    public const string InvalidRequest = "ANALYST_REQUEST_INVALID";
    public const string NotFound = "ANALYST_DATA_NOT_FOUND";
    public const string DatabaseDisabled = "ANALYST_DATABASE_DISABLED";
    public const string AuthenticationFailed = "ANALYST_PROVIDER_AUTHENTICATION_FAILED";
    public const string RateLimited = "ANALYST_PROVIDER_RATE_LIMITED";
    public const string Timeout = "ANALYST_PROVIDER_TIMEOUT";
    public const string InvalidResponse = "ANALYST_PROVIDER_RESPONSE_INVALID";
    public const string ProviderUnavailable = "ANALYST_PROVIDER_UNAVAILABLE";
}

public sealed class AnalystException(
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
