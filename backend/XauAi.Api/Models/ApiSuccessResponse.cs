namespace XauAi.Api.Models;

public sealed record ApiSuccessResponse<T>(bool Success, T Data, string TraceId)
{
    public static ApiSuccessResponse<T> Create(T data, string traceId) =>
        new(true, data, traceId);
}
