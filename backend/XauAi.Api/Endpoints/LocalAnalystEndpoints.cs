using XauAi.Api.Models;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;

namespace XauAi.Api.Endpoints;

public static class LocalAnalystEndpoints
{
    public static IEndpointRouteBuilder MapLocalAnalystEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/local-analyst")
            .WithTags("Local Analyst");

        group.MapGet("/status", async (
                ILocalAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<LocalAnalystStatus>.Create(
                await service.GetStatusAsync(cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetLocalAnalystStatus")
            .WithSummary("Returns deterministic local-analyst processing status by timeframe.")
            .Produces<ApiSuccessResponse<LocalAnalystStatus>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{symbol}/{timeframe}/evaluate", async (
                string symbol,
                string timeframe,
                ILocalAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<LocalSignalSnapshot>.Create(
                await service.EvaluateAsync(symbol, ParseTimeframe(timeframe), cancellationToken),
                context.TraceIdentifier)))
            .WithName("EvaluateLocalSignal")
            .WithSummary("Evaluates the latest completed candle with the deterministic, AI-free signal engine.")
            .Produces<ApiSuccessResponse<LocalSignalSnapshot>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{symbol}/{timeframe}/current", async (
                string symbol,
                string timeframe,
                ILocalAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<LocalSignalSnapshot>.Create(
                await service.GetCurrentAsync(symbol, ParseTimeframe(timeframe), cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetCurrentLocalSignal")
            .WithSummary("Returns the latest local-analyst decision and active signal state.")
            .Produces<ApiSuccessResponse<LocalSignalSnapshot>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{symbol}/{timeframe}/markers", async (
                string symbol,
                string timeframe,
                int? limit,
                ILocalAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<LocalSignalChartMarker>>.Create(
                await service.GetChartMarkersAsync(
                    symbol,
                    ParseTimeframe(timeframe),
                    limit ?? 100,
                    cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetLocalSignalChartMarkers")
            .WithSummary("Returns only persisted BUY, SELL, and STOP events for chart annotations.")
            .Produces<ApiSuccessResponse<IReadOnlyList<LocalSignalChartMarker>>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{symbol}/history", async (
                string symbol,
                string? timeframe,
                int? limit,
                ILocalAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<LocalSignalHistoryResult>.Create(
                await service.GetHistoryAsync(
                    symbol,
                    string.IsNullOrWhiteSpace(timeframe) ? null : ParseTimeframe(timeframe),
                    limit ?? 100,
                    cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetLocalSignalHistory")
            .WithSummary("Returns preserved local signal history with origin and terminal state.")
            .Produces<ApiSuccessResponse<LocalSignalHistoryResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/signals/{signalId}/lifecycle", async (
                string signalId,
                ILocalAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<LocalSignalLifecycleItem>>.Create(
                await service.GetLifecycleAsync(signalId, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetLocalSignalLifecycle")
            .WithSummary("Returns the append-only lifecycle audit for a public local signal identifier.")
            .Produces<ApiSuccessResponse<IReadOnlyList<LocalSignalLifecycleItem>>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static MarketTimeframe ParseTimeframe(string value)
    {
        if (MarketTimeframes.TryParse(value, out var timeframe))
        {
            return timeframe;
        }

        throw new LocalAnalystException(
            LocalAnalystErrorCodes.InvalidRequest,
            "The timeframe must be one of M1, M5, M15, M30, H1, H4, or D1.");
    }
}
