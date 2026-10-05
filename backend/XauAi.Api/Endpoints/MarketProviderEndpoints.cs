using XauAi.Api.Models;
using XauAi.Application.MarketData;

namespace XauAi.Api.Endpoints;

public static class MarketProviderEndpoints
{
    private const string ApplicationSymbol = "XAUUSD";

    public static IEndpointRouteBuilder MapMarketProviderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/market-data/provider/status", async (
                IMarketDataProvider provider,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<MarketDataProviderStatus>.Create(
                await provider.GetStatusAsync(cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetMarketDataProviderStatus")
            .WithSummary("Returns safe connectivity status for AllTick.")
            .Produces<ApiSuccessResponse<MarketDataProviderStatus>>(StatusCodes.Status200OK);

        endpoints.MapGet("/api/market/xauusd/quote", async (
                IMarketDataProvider provider,
                HttpContext context,
                CancellationToken cancellationToken) =>
            Results.Ok(ApiSuccessResponse<MarketQuote>.Create(
                await provider.GetQuoteAsync(ApplicationSymbol, cancellationToken),
                context.TraceIdentifier)))
            .WithName("GetXauUsdQuote")
            .WithSummary("Returns the current normalized XAUUSD bid and ask from AllTick.")
            .Produces<ApiSuccessResponse<MarketQuote>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapGet("/api/market/xauusd/candles", async (
                string timeframe,
                DateTimeOffset from,
                DateTimeOffset to,
                IMarketDataProvider provider,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var parsedTimeframe = ParseTimeframe(timeframe);
                var candles = await provider.GetCandlesAsync(
                    ApplicationSymbol,
                    parsedTimeframe,
                    from,
                    to,
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<IReadOnlyList<MarketCandleSnapshot>>.Create(
                    candles,
                    context.TraceIdentifier));
            })
            .WithName("GetXauUsdCandles")
            .WithSummary("Returns normalized XAUUSD candles from AllTick for an explicit UTC range.")
            .Produces<ApiSuccessResponse<IReadOnlyList<MarketCandleSnapshot>>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPost("/api/market/xauusd/candles/sync", async (
                CandleSyncRequest request,
                IMarketDataIngestionService ingestionService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await ingestionService.SyncAsync(
                    ApplicationSymbol,
                    ParseTimeframe(request.Timeframe),
                    request.From,
                    request.To,
                    cancellationToken);
                return Results.Ok(ApiSuccessResponse<MarketDataSyncResult>.Create(
                    result,
                    context.TraceIdentifier));
            })
            .WithName("SyncXauUsdCandles")
            .WithSummary("Incrementally retrieves and idempotently persists an explicit XAUUSD candle range.")
            .Produces<ApiSuccessResponse<MarketDataSyncResult>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static MarketTimeframe ParseTimeframe(string value)
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

public sealed record CandleSyncRequest(string Timeframe, DateTimeOffset From, DateTimeOffset To);
