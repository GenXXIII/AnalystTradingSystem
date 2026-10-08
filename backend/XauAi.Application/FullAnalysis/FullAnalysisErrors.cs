namespace XauAi.Application.FullAnalysis;

public static class FullAnalysisErrorCodes
{
    public const string InvalidRequest = "FULL_ANALYSIS_INVALID_REQUEST";
    public const string Disabled = "FULL_ANALYSIS_DISABLED";
    public const string DatabaseDisabled = "FULL_ANALYSIS_DATABASE_DISABLED";
    public const string NotFound = "FULL_ANALYSIS_NOT_FOUND";
    public const string InvalidState = "FULL_ANALYSIS_INVALID_STATE";
    public const string MissingMarketData = "FULL_ANALYSIS_MISSING_MARKET_DATA";
    public const string NoEvidence = "FULL_ANALYSIS_NO_EVIDENCE";
    public const string WorkspaceDisabled = "FULL_AI_WORKSPACE_DISABLED";
    public const string WorkspaceNotConfigured = "FULL_AI_WORKSPACE_NOT_CONFIGURED";
    public const string ProviderNotSupported = "FULL_AI_PROVIDER_NOT_SUPPORTED";
    public const string AuthenticationFailed = "FULL_AI_AUTHENTICATION_FAILED";
    public const string RateLimited = "FULL_AI_RATE_LIMITED";
    public const string Timeout = "FULL_AI_TIMEOUT";
    public const string TokenLimit = "FULL_AI_TOKEN_LIMIT";
    public const string InvalidResponse = "FULL_AI_INVALID_RESPONSE";
    public const string Unavailable = "FULL_AI_UNAVAILABLE";
}

public sealed class FullAnalysisException : Exception
{
    public FullAnalysisException(string code, string safeMessage, Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        Code = code;
        SafeMessage = safeMessage;
    }

    public string Code { get; }
    public string SafeMessage { get; }
}
