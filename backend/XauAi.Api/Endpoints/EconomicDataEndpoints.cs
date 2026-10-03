using XauAi.Api.Models;
using XauAi.Application.EconomicData;

namespace XauAi.Api.Endpoints;

public static class EconomicDataEndpoints
{
    public static IEndpointRouteBuilder MapEconomicDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/economic-data")
            .WithTags("Economic Data");

        group.MapGet("/series", async (
                string? category,
                string? frequency,
                bool? activeOnly,
                IEconomicDataQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetSeriesAsync(
                    new EconomicSeriesQuery(category, frequency, activeOnly ?? true),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<IReadOnlyList<EconomicSeriesResult>>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetEconomicSeries")
            .WithSummary("Returns configured normalized economic series from SQL storage.")
            .Produces<ApiSuccessResponse<IReadOnlyList<EconomicSeriesResult>>>(StatusCodes.Status200OK);

        group.MapGet("/series/{id:guid}", async (
                Guid id,
                IEconomicDataQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetSeriesByIdAsync(id, cancellationToken);
                return Results.Ok(ApiSuccessResponse<EconomicSeriesResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetEconomicSeriesById")
            .WithSummary("Returns one normalized economic series.")
            .Produces<ApiSuccessResponse<EconomicSeriesResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/series/{id:guid}/observations", async (
                Guid id,
                DateOnly? from,
                DateOnly? to,
                int? page,
                int? pageSize,
                IEconomicDataQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetObservationsAsync(
                    new EconomicObservationQuery(id, from, to, page ?? 1, pageSize ?? 100),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedEconomicObservations>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetEconomicObservations")
            .WithSummary("Returns bounded current observations for an economic series and date range.")
            .Produces<ApiSuccessResponse<PagedEconomicObservations>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/latest", async (
                IEconomicDataQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetLatestAsync(cancellationToken);
                return Results.Ok(ApiSuccessResponse<IReadOnlyList<EconomicObservationResult>>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetLatestEconomicObservations")
            .WithSummary("Returns the latest locally stored observation for each series.")
            .Produces<ApiSuccessResponse<IReadOnlyList<EconomicObservationResult>>>(StatusCodes.Status200OK);

        group.MapGet("/status", async (
                IEconomicDataQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetStatusAsync(cancellationToken);
                return Results.Ok(ApiSuccessResponse<EconomicSystemStatus>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetEconomicDataStatus")
            .WithSummary("Returns safe provider configuration and durable synchronization status.")
            .Produces<ApiSuccessResponse<EconomicSystemStatus>>(StatusCodes.Status200OK);

        group.MapPost("/synchronize", async (
                EconomicSyncApiRequest request,
                IEconomicDataSynchronizationService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.SynchronizeAsync(
                    new EconomicSyncRequest(request.ExternalSeriesId, request.From, request.To),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<EconomicSyncResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("SynchronizeEconomicData")
            .WithSummary("Runs a bounded incremental synchronization for all or one configured series.")
            .Produces<ApiSuccessResponse<EconomicSyncResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}

public sealed record EconomicSyncApiRequest(string? ExternalSeriesId, DateOnly? From, DateOnly? To);
