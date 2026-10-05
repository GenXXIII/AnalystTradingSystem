using XauAi.Api.Models;
using XauAi.Application.AI;

namespace XauAi.Api.Endpoints;

public static class AiInterpretationEndpoints
{
    public static IEndpointRouteBuilder MapAiInterpretationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var interpretations = endpoints.MapGroup("/api/ai-interpretations").WithTags("AI Interpretations");

        interpretations.MapPost("/", async (
                CreateAiInterpretationRequest request,
                IAiInterpretationService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.InterpretAsync(request, cancellationToken);
                return Results.Ok(ApiSuccessResponse<AiInterpretationResult>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("CreateAiEvidenceInterpretation")
            .WithSummary("Selects eligible evidence and requests one structured specialist interpretation; it never creates a trade decision.")
            .Produces<ApiSuccessResponse<AiInterpretationResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        interpretations.MapGet("/", async (
                string? instrument,
                AiSpecialist? specialist,
                AiInterpretationType? interpretationType,
                string? timeframe,
                AiInterpretationLifecycle? lifecycle,
                DateTimeOffset? from,
                DateTimeOffset? to,
                bool? includeHistorical,
                int? page,
                int? pageSize,
                IAiInterpretationService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.QueryAsync(
                    new AiInterpretationQuery(
                        instrument,
                        specialist,
                        interpretationType,
                        timeframe,
                        lifecycle,
                        from,
                        to,
                        includeHistorical ?? false,
                        page ?? 1,
                        pageSize ?? 50),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedAiInterpretations>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetAiEvidenceInterpretations")
            .WithSummary("Returns current or historical structured interpretations filtered by context and time.")
            .Produces<ApiSuccessResponse<PagedAiInterpretations>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        interpretations.MapGet("/latest", async (
                string instrument,
                AiSpecialist? specialist,
                AiInterpretationType? interpretationType,
                string? timeframe,
                IAiInterpretationService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetLatestAsync(
                    instrument,
                    specialist,
                    interpretationType,
                    timeframe,
                    cancellationToken);
                return result is null
                    ? Results.NotFound(ApiErrorResponse.Create(
                        AiInterpretationErrorCodes.NotFound,
                        "No current AI interpretation matches the requested context.",
                        context.TraceIdentifier))
                    : Results.Ok(ApiSuccessResponse<AiInterpretationResult>.Create(
                        result,
                        context.TraceIdentifier));
            })
            .WithName("GetLatestAiEvidenceInterpretation")
            .WithSummary("Returns the latest current interpretation for an XAUUSD context.")
            .Produces<ApiSuccessResponse<AiInterpretationResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        interpretations.MapGet("/specialists", (
                AiSpecialistCatalog catalog,
                HttpContext context) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<AiSpecialistConfigurationView>>.Create(
                catalog.GetSafeViews(),
                context.TraceIdentifier)))
            .WithName("GetAiSpecialistConfiguration")
            .WithSummary("Returns non-secret independent specialist configuration metadata.")
            .Produces<ApiSuccessResponse<IReadOnlyList<AiSpecialistConfigurationView>>>(StatusCodes.Status200OK);

        interpretations.MapGet("/{id:guid}", async (
                Guid id,
                IAiInterpretationService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAsync(id, cancellationToken);
                return Results.Ok(ApiSuccessResponse<AiInterpretationResult>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetAiEvidenceInterpretationById")
            .WithSummary("Returns a structured interpretation with its exact evidence trace.")
            .Produces<ApiSuccessResponse<AiInterpretationResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/evidence/{evidenceId:guid}/interpretations", async (
                Guid evidenceId,
                int? page,
                int? pageSize,
                IAiInterpretationService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetByEvidenceAsync(
                    evidenceId,
                    page ?? 1,
                    pageSize ?? 50,
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedAiInterpretations>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithTags("AI Interpretations")
            .WithName("GetAiInterpretationsByEvidence")
            .WithSummary("Returns every historical interpretation that used the specified evidence record.")
            .Produces<ApiSuccessResponse<PagedAiInterpretations>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
