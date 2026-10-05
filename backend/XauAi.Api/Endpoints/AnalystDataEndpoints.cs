using XauAi.Api.Models;
using XauAi.Application.Analysts;

namespace XauAi.Api.Endpoints;

public static class AnalystDataEndpoints
{
    public static IEndpointRouteBuilder MapAnalystDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sources = endpoints.MapGroup("/api/analyst-sources").WithTags("Analyst Data");
        sources.MapGet("/", async (
                string? type,
                bool? activeOnly,
                int? page,
                int? pageSize,
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetSourcesAsync(
                    new AnalystSourceQuery(type, activeOnly ?? true, page ?? 1, pageSize ?? 25),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedAnalystSources>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetAnalystSources")
            .WithSummary("Returns normalized analyst sources from SQL storage.")
            .Produces<ApiSuccessResponse<PagedAnalystSources>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);
        sources.MapGet("/{id:guid}", async (
                Guid id,
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetSourceAsync(id, cancellationToken);
                return Results.Ok(ApiSuccessResponse<AnalystSourceResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetAnalystSource")
            .WithSummary("Returns one normalized analyst source.")
            .Produces<ApiSuccessResponse<AnalystSourceResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        var analysts = endpoints.MapGroup("/api/analysts").WithTags("Analyst Data");
        analysts.MapGet("/", async (
                Guid? sourceId,
                bool? activeOnly,
                int? page,
                int? pageSize,
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAnalystsAsync(
                    new AnalystIdentityQuery(sourceId, activeOnly ?? true, page ?? 1, pageSize ?? 25),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedAnalysts>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetAnalysts")
            .WithSummary("Returns identifiable analysts without inventing identities for unattributed publications.")
            .Produces<ApiSuccessResponse<PagedAnalysts>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);
        analysts.MapGet("/{id:guid}", async (
                Guid id,
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAnalystAsync(id, cancellationToken);
                return Results.Ok(ApiSuccessResponse<AnalystIdentityResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetAnalyst")
            .WithSummary("Returns one analyst identity and source attribution.")
            .Produces<ApiSuccessResponse<AnalystIdentityResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        var predictions = endpoints.MapGroup("/api/analyst-predictions").WithTags("Analyst Data");
        predictions.MapGet("/", async (
                string? instrument,
                AnalystDirection? direction,
                Guid? sourceId,
                Guid? analystId,
                DateTimeOffset? from,
                DateTimeOffset? to,
                AnalystHorizonUnit? horizon,
                DateTimeOffset? asOf,
                int? page,
                int? pageSize,
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetPredictionsAsync(
                    new AnalystPredictionQuery(
                        instrument,
                        direction,
                        sourceId,
                        analystId,
                        from,
                        to,
                        horizon,
                        asOf,
                        page ?? 1,
                        pageSize ?? 25),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedAnalystPredictions>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetAnalystPredictions")
            .WithSummary("Returns attributed analyst claims published no later than the requested as-of timestamp.")
            .Produces<ApiSuccessResponse<PagedAnalystPredictions>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);
        predictions.MapGet("/latest", async (
                int? pageSize,
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetLatestAsync(pageSize ?? 25, cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedAnalystPredictions>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetLatestAnalystPredictions")
            .WithSummary("Returns recent normalized analyst claims, never future-published records.")
            .Produces<ApiSuccessResponse<PagedAnalystPredictions>>(StatusCodes.Status200OK);
        predictions.MapGet("/status", async (
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetStatusAsync(cancellationToken);
                return Results.Ok(ApiSuccessResponse<AnalystSystemStatus>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetAnalystDataStatus")
            .WithSummary("Returns safe provider configuration and durable analyst synchronization status.")
            .Produces<ApiSuccessResponse<AnalystSystemStatus>>(StatusCodes.Status200OK);
        predictions.MapGet("/{id:guid}", async (
                Guid id,
                IAnalystQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetPredictionAsync(id, cancellationToken);
                return Results.Ok(ApiSuccessResponse<AnalystPredictionResult>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetAnalystPrediction")
            .WithSummary("Returns one immutable, attributed analyst claim.")
            .Produces<ApiSuccessResponse<AnalystPredictionResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);
        predictions.MapPost("/synchronize", async (
                AnalystSyncApiRequest request,
                IAnalystSynchronizationService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.SynchronizeAsync(
                    new AnalystSyncRequest(request.FromUtc, request.ToUtc),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<AnalystSyncResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("SynchronizeAnalystData")
            .WithSummary("Runs a bounded incremental synchronization from the configured legitimate analyst source.")
            .Produces<ApiSuccessResponse<AnalystSyncResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}

public sealed record AnalystSyncApiRequest(DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
