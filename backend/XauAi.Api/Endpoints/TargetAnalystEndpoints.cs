using XauAi.Api.Models;
using XauAi.Application.MarketData;
using XauAi.Application.TargetAnalysis;

namespace XauAi.Api.Endpoints;

public static class TargetAnalystEndpoints
{
    public static IEndpointRouteBuilder MapTargetAnalystEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/target-analyst")
            .WithTags("Target Analyst");

        group.MapPost("/jobs", async (
                CreateTargetAnalysisRequest request,
                ITargetAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.AnalyzeAsync(request, cancellationToken);
                return Results.Created(
                    $"/api/target-analyst/jobs/{result.Id}",
                    ApiSuccessResponse<TargetAnalysisResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("CreateTargetAnalysis")
            .WithSummary("Creates one user-triggered target-analysis job and returns one target or NO VALID TARGET.")
            .Produces<ApiSuccessResponse<TargetAnalysisResult>>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/jobs/{id:guid}", async (
                Guid id,
                ITargetAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<TargetAnalysisResult>.Create(
                await service.GetAsync(id, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetTargetAnalysis")
            .WithSummary("Returns an immutable target-analysis result and its preserved specialist outputs.")
            .Produces<ApiSuccessResponse<TargetAnalysisResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/active/{symbol}", async (
                string symbol,
                ITargetAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<TargetAnalysisResult>>.Create(
                await service.GetActiveAsync(symbol, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetActiveTargets")
            .WithSummary("Returns active targets without deriving BUY, SELL, or WAIT decisions.")
            .Produces<ApiSuccessResponse<IReadOnlyList<TargetAnalysisResult>>>(StatusCodes.Status200OK);

        group.MapGet("/history/{symbol}", async (
                string symbol,
                string? timeframe,
                string? status,
                int? page,
                int? pageSize,
                ITargetAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<PagedTargetAnalyses>.Create(
                await service.GetHistoryAsync(
                    new TargetAnalysisQuery(
                        symbol,
                        ParseOptionalTimeframe(timeframe),
                        ParseOptionalStatus(status),
                        page ?? 1,
                        pageSize ?? 50),
                    cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetTargetAnalysisHistory")
            .WithSummary("Returns newest-first target history, including cancelled and rejected analyses.")
            .Produces<ApiSuccessResponse<PagedTargetAnalyses>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/jobs/{id:guid}/lifecycle", async (
                Guid id,
                ITargetAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<TargetLifecycleItem>>.Create(
                await service.GetLifecycleAsync(id, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetTargetAnalysisLifecycle")
            .WithSummary("Returns the append-only target lifecycle audit.")
            .Produces<ApiSuccessResponse<IReadOnlyList<TargetLifecycleItem>>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/jobs/{id:guid}/cancel", async (
                Guid id,
                ITargetAnalystService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<TargetAnalysisResult>.Create(
                await service.CancelAsync(id, cancellationToken),
                context.TraceIdentifier)))
            .WithName("CancelTargetAnalysis")
            .WithSummary("Cancels active monitoring while preserving the target, evidence, and AI history.")
            .Produces<ApiSuccessResponse<TargetAnalysisResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/configuration", (
                TargetWorkspaceCatalog catalog,
                HttpContext context) =>
            Results.Ok(ApiSuccessResponse<IReadOnlyList<TargetWorkspaceConfigurationView>>.Create(
                catalog.GetSafeViews(),
                context.TraceIdentifier)))
            .WithName("GetTargetAiWorkspaceConfiguration")
            .WithSummary("Returns safe, secret-free configuration for all eight Target AI workspaces.")
            .Produces<ApiSuccessResponse<IReadOnlyList<TargetWorkspaceConfigurationView>>>(StatusCodes.Status200OK);

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

    private static TargetAnalysisStatus? ParseOptionalStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<TargetAnalysisStatus>(value, true, out var status)
            ? status
            : throw Invalid("The target-analysis status is invalid.");
    }

    private static TargetAnalysisException Invalid(string message) => new(
        TargetAnalysisErrorCodes.InvalidRequest,
        message);
}
