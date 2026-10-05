using XauAi.Api.Models;
using XauAi.Application.MarketData;

namespace XauAi.Api.Endpoints;

public static class MarketDataEndpoints
{
    public static IEndpointRouteBuilder MapMarketDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/market-data/{symbol}")
            .WithTags("Market Data");

        group.MapGet("/candles", async (
                string symbol,
                string? timeframe,
                DateTimeOffset from,
                DateTimeOffset to,
                int? limit,
                bool? completedOnly,
                IMarketDataQueryService queryService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await queryService.GetRangeAsync(
                    new MarketDataQuery(
                        symbol,
                        ParseTimeframe(timeframe),
                        from,
                        to,
                        limit ?? 500,
                        completedOnly ?? true),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<MarketDataQueryResult>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetStoredMarketCandles")
            .WithSummary("Returns a controlled UTC range of normalized candles from SQL Server.")
            .Produces<ApiSuccessResponse<MarketDataQueryResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/latest", async (
                string symbol,
                string? timeframe,
                int? limit,
                bool? completedOnly,
                IMarketDataQueryService queryService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await queryService.GetLatestAsync(
                    symbol,
                    ParseTimeframe(timeframe),
                    limit ?? 100,
                    completedOnly ?? true,
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<IReadOnlyList<StoredMarketCandle>>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetLatestStoredMarketCandles")
            .WithSummary("Returns the latest normalized candles with a bounded result limit.")
            .Produces<ApiSuccessResponse<IReadOnlyList<StoredMarketCandle>>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/last-completed", async (
                string symbol,
                string? timeframe,
                IMarketDataQueryService queryService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await queryService.GetLastCompletedAsync(
                    symbol,
                    ParseTimeframe(timeframe),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<StoredMarketCandle?>.Create(result, context.TraceIdentifier));
            })
            .WithName("GetLastCompletedMarketCandle")
            .WithSummary("Returns the latest completed candle and never substitutes a forming candle.")
            .Produces<ApiSuccessResponse<StoredMarketCandle?>>(StatusCodes.Status200OK);

        group.MapGet("/status", async (
                string symbol,
                string? timeframe,
                IMarketDataQueryService queryService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await queryService.GetStatusAsync(
                    symbol,
                    ParseTimeframe(timeframe),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<MarketDataPipelineStatus>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetMarketDataPipelineStatus")
            .WithSummary("Returns safe persisted availability and synchronization state.")
            .Produces<ApiSuccessResponse<MarketDataPipelineStatus>>(StatusCodes.Status200OK);

        group.MapGet("/gaps", async (
                string symbol,
                string? timeframe,
                DateTimeOffset from,
                DateTimeOffset to,
                int? limit,
                IMarketDataQueryService queryService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await queryService.GetGapsAsync(
                    new MarketDataQuery(
                        symbol,
                        ParseTimeframe(timeframe),
                        from,
                        to,
                        limit ?? 100,
                        CompletedOnly: true),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<IReadOnlyList<MarketDataGap>>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("GetMarketDataGaps")
            .WithSummary("Returns candidate gaps without fabricating missing candles.")
            .Produces<ApiSuccessResponse<IReadOnlyList<MarketDataGap>>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/source-comparison", async (
                string symbol,
                string? timeframe,
                int? limit,
                IMarketDataQualityService qualityService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await qualityService.CompareAsync(
                    symbol,
                    ParseTimeframe(timeframe),
                    limit ?? 100,
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<MarketDataSourceComparison>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("CompareMarketDataSources")
            .WithSummary("Compares completed primary and reference candles without blending provider data.")
            .Produces<ApiSuccessResponse<MarketDataSourceComparison>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/sync", async (
                string symbol,
                MarketDataSyncApiRequest request,
                IMarketDataSynchronizationService synchronizationService,
                TimeProvider timeProvider,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await synchronizationService.SynchronizeAsync(
                    new MarketDataSynchronizationRequest(
                        symbol,
                        ParseTimeframe(request.Timeframe),
                        request.From,
                        request.To ?? timeProvider.GetUtcNow(),
                        request.IncludeFormingCandle),
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<MarketDataPipelineResult>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("SynchronizeMarketData")
            .WithSummary("Runs bounded, validated, batched, and idempotent market-data synchronization.")
            .Produces<ApiSuccessResponse<MarketDataPipelineResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static MarketTimeframe ParseTimeframe(string? value)
    {
        if (MarketTimeframes.TryParse(value, out var timeframe))
        {
            return timeframe;
        }

        throw new MarketDataException(
            MarketDataErrorCodes.InvalidRequest,
            "The timeframe must be one of M1, M5, M15, M30, H1, H4, or D1.");
    }
}

public sealed record MarketDataSyncApiRequest(
    string? Timeframe,
    DateTimeOffset? From,
    DateTimeOffset? To,
    bool IncludeFormingCandle = false);
