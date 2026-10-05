namespace XauAi.Application.AI;

public static class AiInterpretationErrorCodes
{
    public const string InvalidRequest = "AI_INTERPRETATION_REQUEST_INVALID";
    public const string NotFound = "AI_INTERPRETATION_NOT_FOUND";
    public const string NoEvidence = "AI_INTERPRETATION_NO_EVIDENCE";
    public const string SpecialistDisabled = "AI_SPECIALIST_DISABLED";
    public const string SpecialistNotConfigured = "AI_SPECIALIST_NOT_CONFIGURED";
    public const string ProviderNotSupported = "AI_PROVIDER_NOT_SUPPORTED";
    public const string AuthenticationFailed = "AI_PROVIDER_AUTHENTICATION_FAILED";
    public const string RateLimited = "AI_PROVIDER_RATE_LIMITED";
    public const string Timeout = "AI_PROVIDER_TIMEOUT";
    public const string Unavailable = "AI_PROVIDER_UNAVAILABLE";
    public const string InvalidResponse = "AI_PROVIDER_INVALID_RESPONSE";
    public const string TokenLimit = "AI_PROVIDER_TOKEN_LIMIT";
    public const string DatabaseDisabled = "AI_INTERPRETATION_DATABASE_DISABLED";
}

public class AiInterpretationException(
    string code,
    string safeMessage,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;

    public string SafeMessage { get; } = safeMessage;
}

public sealed class AiProviderException(
    string code,
    string safeMessage,
    Exception? innerException = null) : AiInterpretationException(code, safeMessage, innerException);
