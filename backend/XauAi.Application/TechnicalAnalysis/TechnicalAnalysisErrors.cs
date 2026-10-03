namespace XauAi.Application.TechnicalAnalysis;

public static class TechnicalAnalysisErrorCodes
{
    public const string Disabled = "TECHNICAL_ANALYSIS_DISABLED";
    public const string InvalidRequest = "TECHNICAL_ANALYSIS_REQUEST_INVALID";
    public const string NoData = "TECHNICAL_ANALYSIS_NO_DATA";
}

public sealed class TechnicalAnalysisException(string code, string safeMessage) : Exception(safeMessage)
{
    public string Code { get; } = code;

    public string SafeMessage { get; } = safeMessage;
}
