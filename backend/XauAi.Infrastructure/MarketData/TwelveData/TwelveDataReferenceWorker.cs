using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.TwelveData;

internal sealed class TwelveDataReferenceWorker(
    TwelveDataOptions options,
    TwelveDataMarketDataProvider provider,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<TwelveDataReferenceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Twelve Data reference synchronization is disabled");
            return;
        }

        await SynchronizeAsync(initial: true, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(options.ReferenceSyncIntervalSeconds), timeProvider, stoppingToken);
            await SynchronizeAsync(initial: false, stoppingToken);
        }
    }

    private async Task SynchronizeAsync(bool initial, CancellationToken cancellationToken)
    {
        try
        {
            var toUtc = MarketTimeframe.M1.AlignDown(timeProvider.GetUtcNow());
            var fromUtc = initial
                ? toUtc.AddDays(-options.InitialHistoryDays)
                : toUtc.AddMinutes(-options.IncrementalLookbackMinutes);
            var minuteCandles = await FetchMinuteCandlesAsync(fromUtc, toUtc, cancellationToken);
            await PersistAllTimeframesAsync(minuteCandles, toUtc, cancellationToken);
            logger.LogInformation(
                "Twelve Data reference synchronization stored {CandleCount} source M1 candles from {FromUtc} to {ToUtc}",
                minuteCandles.Count,
                fromUtc,
                toUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (MarketDataException exception)
        {
            logger.LogWarning("Twelve Data reference synchronization failed with code {ErrorCode}", exception.Code);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Twelve Data reference synchronization failed unexpectedly");
        }
    }

    private async Task<IReadOnlyList<MarketCandleSnapshot>> FetchMinuteCandlesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var results = new List<MarketCandleSnapshot>();
        var cursor = MarketTimeframe.M1.AlignDown(fromUtc);
        var pointsPerRequest = Math.Max(1, options.MaximumPointsPerRequest - 1);
        while (cursor <= toUtc)
        {
            var chunkTo = cursor.AddMinutes(pointsPerRequest);
            if (chunkTo > toUtc)
            {
                chunkTo = toUtc;
            }

            results.AddRange(await provider.GetCandlesAsync(
                options.ApplicationSymbol,
                MarketTimeframe.M1,
                cursor,
                chunkTo,
                cancellationToken));
            cursor = chunkTo.AddMinutes(1);
        }

        return [.. results
            .OrderBy(candle => candle.OpenTimeUtc)
            .DistinctBy(candle => candle.OpenTimeUtc)];
    }

    private async Task PersistAllTimeframesAsync(
        IReadOnlyCollection<MarketCandleSnapshot> minuteCandles,
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken)
    {
        if (minuteCandles.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMarketCandleStore>();
        foreach (var timeframe in Enum.GetValues<MarketTimeframe>())
        {
            var candles = MarketCandleAggregator.Aggregate(minuteCandles, timeframe, observedAtUtc);
            await store.SaveAsync(candles, cancellationToken);
        }
    }
}
