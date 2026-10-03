using XauAi.Api.Models;
using XauAi.Application.News;

namespace XauAi.Api.Endpoints;

public static class NewsEndpoints
{
    public static IEndpointRouteBuilder MapNewsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/news")
            .WithTags("News");

        group.MapGet("/", async (
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                string? category,
                string? source,
                NewsRelevanceLevel? minimumRelevance,
                string? search,
                int? page,
                int? pageSize,
                INewsQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.QueryAsync(
                    new NewsArticleQuery(
                        fromUtc,
                        toUtc,
                        category,
                        source,
                        minimumRelevance,
                        search,
                        page ?? 1,
                        pageSize ?? 25),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedNewsArticles>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetNews")
            .WithSummary("Returns paginated, normalized application-owned news ordered by publication time.")
            .Produces<ApiSuccessResponse<PagedNewsArticles>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/latest", async (
                int? pageSize,
                INewsQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetLatestAsync(pageSize ?? 25, cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedNewsArticles>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetLatestNews")
            .WithSummary("Returns recent relevant news from local SQL storage without calling the provider.")
            .Produces<ApiSuccessResponse<PagedNewsArticles>>(StatusCodes.Status200OK);

        group.MapGet("/relevant", async (
                NewsRelevanceLevel? minimumRelevance,
                int? page,
                int? pageSize,
                INewsQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetRelevantAsync(
                    minimumRelevance ?? NewsRelevanceLevel.High,
                    page ?? 1,
                    pageSize ?? 25,
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<PagedNewsArticles>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetRelevantNews")
            .WithSummary("Returns news at or above an explicit deterministic relevance classification.")
            .Produces<ApiSuccessResponse<PagedNewsArticles>>(StatusCodes.Status200OK);

        group.MapGet("/status", async (
                INewsQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetStatusAsync(cancellationToken);
                return Results.Ok(ApiSuccessResponse<NewsSystemStatus>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetNewsStatus")
            .WithSummary("Returns safe provider and persisted collection health without credentials.")
            .Produces<ApiSuccessResponse<NewsSystemStatus>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
                Guid id,
                INewsQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetByIdAsync(id, cancellationToken);
                return Results.Ok(ApiSuccessResponse<NewsArticleResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetNewsArticle")
            .WithSummary("Returns one normalized article with source traceability.")
            .Produces<ApiSuccessResponse<NewsArticleResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/collect", async (
                NewsCollectionApiRequest request,
                INewsCollectionService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CollectAsync(
                    new NewsCollectionRequest(request.FromUtc, request.ToUtc),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<NewsCollectionResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("CollectNews")
            .WithSummary("Runs one bounded, incremental, duplicate-safe news collection.")
            .Produces<ApiSuccessResponse<NewsCollectionResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}

public sealed record NewsCollectionApiRequest(DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
