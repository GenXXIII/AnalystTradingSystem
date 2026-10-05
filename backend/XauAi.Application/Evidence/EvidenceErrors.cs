namespace XauAi.Application.Evidence;

public static class EvidenceErrorCodes
{
    public const string InvalidRequest = "EVIDENCE_REQUEST_INVALID";
    public const string NotFound = "EVIDENCE_NOT_FOUND";
    public const string DatabaseDisabled = "EVIDENCE_DATABASE_DISABLED";
    public const string InvalidRecord = "EVIDENCE_RECORD_INVALID";
}

public sealed class EvidenceException(
    string code,
    string safeMessage,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;

    public string SafeMessage { get; } = safeMessage;
}
