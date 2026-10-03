namespace XauAi.Application.News;

public static class NewsErrorCodes
{
    public const string Disabled = "NEWS_DISABLED";
    public const string InvalidRequest = "NEWS_REQUEST_INVALID";
    public const string NotFound = "NEWS_ARTICLE_NOT_FOUND";
    public const string DatabaseDisabled = "NEWS_DATABASE_DISABLED";
    public const string AuthenticationFailed = "NEWS_PROVIDER_AUTHENTICATION_FAILED";
    public const string RateLimited = "NEWS_PROVIDER_RATE_LIMITED";
    public const string Timeout = "NEWS_PROVIDER_TIMEOUT";
    public const string InvalidResponse = "NEWS_PROVIDER_RESPONSE_INVALID";
    public const string ProviderUnavailable = "NEWS_PROVIDER_UNAVAILABLE";
}

public sealed class NewsException(
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
