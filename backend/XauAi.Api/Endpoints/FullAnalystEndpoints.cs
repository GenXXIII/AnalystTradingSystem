using XauAi.Api.Models;
using XauAi.Application.FullAnalysis;
using XauAi.Application.MarketData;

namespace XauAi.Api.Endpoints;

public static class FullAnalystEndpoints
{
    public static IEndpointRouteBuilder MapFullAnalystEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/full-analyst").WithTags("Full Analyst");

        group.MapPost("/jobs", async (
                CreateFullAnalysisRequest request,
                IFullAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.AnalyzeAsync(request, cancellationToken);
                return Results.Created(
                    $"/api/full-analyst/jobs/{result.Id}",
                    ApiSuccessResponse<FullAnalysisResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("CreateFullAnalysis")
            .WithSummary("Creates one evidence-bound Full Analyst job and returns BUY, SELL, or WAIT.")
            .Produces<ApiSuccessResponse<FullAnalysisResult>>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/jobs/{id:guid}", async (
                Guid id,
                IFullAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<FullAnalysisResult>.Create(
                await service.GetAsync(id, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetFullAnalysis")
            .WithSummary("Returns a Full Analyst snapshot with preserved specialist and master results.")
            .Produces<ApiSuccessResponse<FullAnalysisResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/active/{symbol}", async (
                string symbol,
                IFullAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<FullAnalysisResult>>.Create(
                await service.GetActiveAsync(symbol, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetActiveFullAnalyses")
            .WithSummary("Returns independently active BUY or SELL Full Analyst results.")
            .Produces<ApiSuccessResponse<IReadOnlyList<FullAnalysisResult>>>(StatusCodes.Status200OK);

        group.MapGet("/history/{symbol}", async (
                string symbol,
                string? timeframe,
                string? status,
                int? page,
                int? pageSize,
                IFullAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<PagedFullAnalyses>.Create(
                await service.GetHistoryAsync(
                    new FullAnalysisQuery(
                        symbol,
                        ParseOptionalTimeframe(timeframe),
                        ParseOptionalStatus(status),
                        page ?? 1,
                        pageSize ?? 50),
                    cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetFullAnalysisHistory")
            .WithSummary("Returns newest-first Full Analyst history, including WAIT and cancelled results.")
            .Produces<ApiSuccessResponse<PagedFullAnalyses>>(StatusCodes.Status200OK);

        group.MapGet("/jobs/{id:guid}/lifecycle", async (
                Guid id,
                IFullAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<FullLifecycleItem>>.Create(
                await service.GetLifecycleAsync(id, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetFullAnalysisLifecycle")
            .WithSummary("Returns the append-only Full Analyst lifecycle audit.")
            .Produces<ApiSuccessResponse<IReadOnlyList<FullLifecycleItem>>>(StatusCodes.Status200OK);

        group.MapPost("/jobs/{id:guid}/cancel", async (
                Guid id,
                IFullAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<FullAnalysisResult>.Create(
                await service.CancelAsync(id, cancellationToken),
                context.TraceIdentifier)))
            .WithName("CancelFullAnalysis")
            .WithSummary("Cancels active Full Analyst monitoring while preserving the complete history.")
            .Produces<ApiSuccessResponse<FullAnalysisResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/configuration", (
                FullWorkspaceCatalog catalog,
                HttpContext context) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<FullWorkspaceConfigurationView>>.Create(
                catalog.GetSafeViews(),
                context.TraceIdentifier)))
            .WithName("GetFullAiWorkspaceConfiguration")
            .WithSummary("Returns secret-free configuration for all eight independent Full AI workspaces.")
            .Produces<ApiSuccessResponse<IReadOnlyList<FullWorkspaceConfigurationView>>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static MarketTimeframe? ParseOptionalTimeframe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return MarketTimeframes.TryParse(value, out var timeframe)
            ? timeframe
            : throw Invalid("The timeframe must be one of M1, M5, M15, M30, H1, H4, or D1.");
    }

    private static FullAnalysisStatus? ParseOptionalStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<FullAnalysisStatus>(value, true, out var status)
            ? status
            : throw Invalid("The Full Analyst status is invalid.");
    }

    private static FullAnalysisException Invalid(string message) => new(FullAnalysisErrorCodes.InvalidRequest, message);
}
