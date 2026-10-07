namespace XauAi.Application.LocalAnalysis;

public static class LocalAnalystErrorCodes
{
    public const string InvalidRequest = "LOCAL_ANALYST_INVALID_REQUEST";
    public const string Disabled = "LOCAL_ANALYST_DISABLED";
    public const string DatabaseDisabled = "LOCAL_ANALYST_DATABASE_DISABLED";
    public const string SignalNotFound = "LOCAL_SIGNAL_NOT_FOUND";
}

public sealed class LocalAnalystException(
    string code,
    string safeMessage,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;

    public string SafeMessage { get; } = safeMessage;
}

