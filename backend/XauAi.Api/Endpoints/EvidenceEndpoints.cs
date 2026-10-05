using XauAi.Api.Models;
using XauAi.Application.Evidence;

namespace XauAi.Api.Endpoints;

public static class EvidenceEndpoints
{
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var evidence = endpoints.MapGroup("/api/evidence").WithTags("Evidence");
        evidence.MapGet("/", async (
                string? instrument,
                EvidenceType? evidenceType,
                EvidenceSourceType? sourceType,
                DateTimeOffset? from,
                DateTimeOffset? to,
                string? timeframe,
                EvidenceDirection? direction,
                EvidenceImportance? importance,
                bool? relevantOnly,
                DateTimeOffset? asOf,
                int? page,
                int? pageSize,
                IEvidenceQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetEvidenceAsync(
                    new EvidenceQuery(
                        instrument,
                        evidenceType,
                        sourceType,
                        from,
                        to,
                        timeframe,
                        direction,
                        importance,
                        relevantOnly,
                        asOf,
                        page ?? 1,
                        pageSize ?? 50),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedEvidence>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetEvidence")
            .WithSummary("Returns normalized, attributed evidence available no later than the requested as-of timestamp.")
            .Produces<ApiSuccessResponse<PagedEvidence>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        evidence.MapGet("/pack", async (
                string instrument,
                string primaryTimeframe,
                string? confirmationTimeframe,
                DateTimeOffset? analysisTime,
                int? lookbackDays,
                IEvidenceQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetPackAsync(
                    new EvidencePackRequest(
                        instrument,
                        primaryTimeframe,
                        confirmationTimeframe,
                        analysisTime,
                        lookbackDays),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<EvidencePack>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetEvidencePack")
            .WithSummary("Builds a multi-source evidence pack with strict availability-time look-ahead protection.")
            .Produces<ApiSuccessResponse<EvidencePack>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        evidence.MapGet("/conflicts", async (
                string instrument,
                DateTimeOffset? from,
                DateTimeOffset? to,
                string? timeframe,
                DateTimeOffset? asOf,
                IEvidenceQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetConflictsAsync(
                    new EvidenceConflictQuery(instrument, from, to, timeframe, asOf),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<IReadOnlyList<EvidenceConflictResult>>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetEvidenceConflicts")
            .WithSummary("Returns deterministic bullish-versus-bearish conflicts without choosing a winner.")
            .Produces<ApiSuccessResponse<IReadOnlyList<EvidenceConflictResult>>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        evidence.MapGet("/sources", async (
                EvidenceSourceType? sourceType,
                DateTimeOffset? asOf,
                int? page,
                int? pageSize,
                IEvidenceQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetSourcesAsync(
                    new EvidenceSourceQuery(sourceType, asOf, page ?? 1, pageSize ?? 50),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedEvidenceSources>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetEvidenceSources")
            .WithSummary("Returns normalized evidence-source coverage and latest availability timestamps.")
            .Produces<ApiSuccessResponse<PagedEvidenceSources>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        evidence.MapGet("/{id:guid}", async (
                Guid id,
                DateTimeOffset? asOf,
                IEvidenceQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetEvidenceAsync(id, asOf, cancellationToken);
                return Results.Ok(ApiSuccessResponse<EvidenceDetailResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetEvidenceById")
            .WithSummary("Returns one normalized evidence record with deterministic relations and clusters.")
            .Produces<ApiSuccessResponse<EvidenceDetailResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
