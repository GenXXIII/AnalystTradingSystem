using XauAi.Api.Models;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.Api.Endpoints;

public static class TechnicalAnalysisEndpoints
{
    public static IEndpointRouteBuilder MapTechnicalAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/analysis/{symbol}")
            .WithTags("Technical Analysis");

        group.MapGet("/{timeframe}", async (
                string symbol,
                string timeframe,
                DateTimeOffset? atUtc,
                ITechnicalAnalysisService analysisService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await analysisService.AnalyzeAsync(
                    new TechnicalAnalysisRequest(symbol, ParseTimeframe(timeframe), atUtc),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<TechnicalAnalysisResult>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetTechnicalAnalysis")
            .WithSummary("Calculates application-owned technical analysis from completed normalized candles.")
            .Produces<ApiSuccessResponse<TechnicalAnalysisResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/multi-timeframe", async (
                string symbol,
                DateTimeOffset? atUtc,
                ITechnicalAnalysisService analysisService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await analysisService.AnalyzeMultiTimeframeAsync(
                    symbol,
                    atUtc,
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<MultiTimeframeAnalysisResult>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetMultiTimeframeTechnicalAnalysis")
            .WithSummary("Compares technical evidence across every enabled market-data timeframe.")
            .Produces<ApiSuccessResponse<MultiTimeframeAnalysisResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
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

        throw new TechnicalAnalysisException(
            TechnicalAnalysisErrorCodes.InvalidRequest,
            "The timeframe must be one of M1, M5, M15, M30, H1, H4, or D1.");
    }
}
