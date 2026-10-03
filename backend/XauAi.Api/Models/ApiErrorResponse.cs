namespace XauAi.Api.Models;

public sealed record ApiError(string Code, string Message);

public sealed record ApiErrorResponse(bool Success, ApiError Error, string TraceId)
{
    public static ApiErrorResponse Create(string code, string message, string traceId) =>
        new(false, new ApiError(code, message), traceId);
}
