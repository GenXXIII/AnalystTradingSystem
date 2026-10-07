namespace XauAi.Application.TargetAnalysis;

public static class TargetAnalysisErrorCodes
{
    public const string InvalidRequest = "TARGET_ANALYSIS_INVALID_REQUEST";
    public const string Disabled = "TARGET_ANALYSIS_DISABLED";
    public const string DatabaseDisabled = "TARGET_ANALYSIS_DATABASE_DISABLED";
    public const string NotFound = "TARGET_ANALYSIS_NOT_FOUND";
    public const string InvalidState = "TARGET_ANALYSIS_INVALID_STATE";
    public const string MissingMarketData = "TARGET_ANALYSIS_MISSING_MARKET_DATA";
    public const string NoEvidence = "TARGET_ANALYSIS_NO_EVIDENCE";
    public const string WorkspaceDisabled = "TARGET_AI_WORKSPACE_DISABLED";
    public const string WorkspaceNotConfigured = "TARGET_AI_WORKSPACE_NOT_CONFIGURED";
    public const string ProviderNotSupported = "TARGET_AI_PROVIDER_NOT_SUPPORTED";
    public const string AuthenticationFailed = "TARGET_AI_AUTHENTICATION_FAILED";
    public const string RateLimited = "TARGET_AI_RATE_LIMITED";
    public const string Timeout = "TARGET_AI_TIMEOUT";
    public const string InvalidResponse = "TARGET_AI_INVALID_RESPONSE";
    public const string TokenLimit = "TARGET_AI_TOKEN_LIMIT";
    public const string Unavailable = "TARGET_AI_UNAVAILABLE";
}

public sealed class TargetAnalysisException(
    string code,
    string safeMessage,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;

    public string SafeMessage { get; } = safeMessage;
}
