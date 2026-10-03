using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.UnitTests.TechnicalAnalysis;

public sealed class TechnicalAnalysisServiceTests
{
    [Fact]
    public async Task Historical_cutoff_excludes_future_forming_invalid_and_duplicate_candles()
    {
        var valid = TechnicalAnalysisTestData.FromCloses(
            Enumerable.Range(0, 120).Select(index => 2000m + index)).ToList();
        var cutoff = valid[99].CloseTimeUtc;
        var duplicate = valid[50] with { Close = valid[50].Close + 0.1m, FetchedAtUtc = cutoff.AddMinutes(-1) };
        var invalid = TechnicalAnalysisTestData.Candle(90, -1m, 1m, -2m, -1m);
        var forming = TechnicalAnalysisTestData.Candle(91, 2090m, 2092m, 2089m, 2091m, complete: false);
        var store = new FakeStore([.. valid.AsEnumerable().Reverse(), duplicate, invalid, forming]);
        var service = CreateService(store, new TechnicalAnalysisSettings(), cutoff.AddHours(1));

        var result = await service.AnalyzeAsync(new TechnicalAnalysisRequest("XAUUSD", MarketTimeframe.H1, cutoff));

        Assert.Equal(cutoff, store.LastCutoffUtc);
        Assert.Equal(100, result.Diagnostics.CandlesUsed);
        Assert.Equal(1, result.Diagnostics.DuplicateCandlesIgnored);
        Assert.True(result.Diagnostics.InvalidCandlesIgnored >= 2);
        Assert.Equal(cutoff, result.LastCandleCloseTimeUtc);
        Assert.Equal(cutoff, result.AnalyzedAtUtc);
    }

    [Fact]
    public async Task Same_dataset_configuration_and_cutoff_produce_same_analytical_result()
    {
        var candles = TechnicalAnalysisTestData.FromCloses(
            Enumerable.Range(0, 300).Select(index => 2000m + (index * 0.2m)));
        var cutoff = candles[^1].CloseTimeUtc;
        var service = CreateService(new FakeStore(candles), new TechnicalAnalysisSettings(), cutoff);

        var first = await service.AnalyzeAsync(new TechnicalAnalysisRequest("XAUUSD", MarketTimeframe.H1, cutoff));
        var second = await service.AnalyzeAsync(new TechnicalAnalysisRequest("XAUUSD", MarketTimeframe.H1, cutoff));

        Assert.True(first.Indicators.Sma.SequenceEqual(second.Indicators.Sma));
        Assert.True(first.Indicators.Ema.SequenceEqual(second.Indicators.Ema));
        Assert.Equal(first.Indicators.Rsi, second.Indicators.Rsi);
        Assert.Equal(first.Indicators.Macd, second.Indicators.Macd);
        Assert.Equal(first.Trend.Direction, second.Trend.Direction);
        Assert.Equal(first.Trend.EmaAlignment, second.Trend.EmaAlignment);
        Assert.True(first.MarketStructure.Swings.SequenceEqual(second.MarketStructure.Swings));
        Assert.Equal(first.MarketStructure.Direction, second.MarketStructure.Direction);
        Assert.Equal(first.Conflicts, second.Conflicts);
        Assert.Equal(first.LastCandleCloseTimeUtc, second.LastCandleCloseTimeUtc);
    }

    [Fact]
    public async Task Multi_timeframe_analysis_preserves_each_timeframe_and_reports_alignment()
    {
        var store = new FakeStore([
            .. TechnicalAnalysisTestData.FromCloses(
                Enumerable.Range(0, 300).Select(index => 2000m + index),
                MarketTimeframe.H1),
            .. TechnicalAnalysisTestData.FromCloses(
                Enumerable.Range(0, 300).Select(index => 1900m + index),
                MarketTimeframe.H4)
        ]);
        var cutoff = TechnicalAnalysisTestData.StartUtc.AddDays(60);
        var settings = new TechnicalAnalysisSettings { Timeframes = [MarketTimeframe.H1, MarketTimeframe.H4] };
        var service = CreateService(store, settings, cutoff);

        var result = await service.AnalyzeMultiTimeframeAsync("XAUUSD", cutoff);

        Assert.Equal(2, result.Analyses.Count);
        Assert.Equal("BullishAligned", result.TrendAlignment);
        Assert.Contains(result.Agreements, value => value.Contains("BullishTrend", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Conflicts, value => value == "TimeframeTrendDisagreement");
    }

    [Fact]
    public async Task Empty_dataset_and_future_cutoff_fail_safely()
    {
        var now = TechnicalAnalysisTestData.StartUtc.AddDays(1);
        var service = CreateService(new FakeStore([]), new TechnicalAnalysisSettings(), now);

        var noData = await Assert.ThrowsAsync<TechnicalAnalysisException>(() =>
            service.AnalyzeAsync(new TechnicalAnalysisRequest("XAUUSD", MarketTimeframe.H1, now)));
        var future = await Assert.ThrowsAsync<TechnicalAnalysisException>(() =>
            service.AnalyzeAsync(new TechnicalAnalysisRequest("XAUUSD", MarketTimeframe.H1, now.AddMinutes(2))));

        Assert.Equal(TechnicalAnalysisErrorCodes.NoData, noData.Code);
        Assert.Equal(TechnicalAnalysisErrorCodes.InvalidRequest, future.Code);
    }

    [Fact]
    public async Task Disabled_engine_and_unknown_symbol_fail_without_provider_access()
    {
        var store = new FakeStore([]);
        var disabled = CreateService(store, new TechnicalAnalysisSettings { Enabled = false }, TechnicalAnalysisTestData.StartUtc);
        var enabled = CreateService(store, new TechnicalAnalysisSettings(), TechnicalAnalysisTestData.StartUtc);

        var disabledError = await Assert.ThrowsAsync<TechnicalAnalysisException>(() =>
            disabled.AnalyzeAsync(new TechnicalAnalysisRequest("XAUUSD", MarketTimeframe.H1)));
        var symbolError = await Assert.ThrowsAsync<TechnicalAnalysisException>(() =>
            enabled.AnalyzeAsync(new TechnicalAnalysisRequest("EURUSD", MarketTimeframe.H1)));

        Assert.Equal(TechnicalAnalysisErrorCodes.Disabled, disabledError.Code);
        Assert.Equal(TechnicalAnalysisErrorCodes.InvalidRequest, symbolError.Code);
        Assert.Equal(0, store.HistoryCalls);
    }

    private static TechnicalAnalysisService CreateService(
        IMarketDataQueryStore store,
        TechnicalAnalysisSettings settings,
        DateTimeOffset now) =>
        new(
            store,
            new IndicatorCalculator(),
            new CandlestickAnalyzer(),
            new MarketStructureAnalyzer(),
            new SupportResistanceAnalyzer(),
            new PriceActionAnalyzer(),
            new VolatilityAnalyzer(),
            new DefaultMarketSessionCalendar(),
            settings,
            new FixedTimeProvider(now),
            NullLogger<TechnicalAnalysisService>.Instance);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore(IReadOnlyList<StoredMarketCandle> candles) : IMarketDataQueryStore
    {
        public DateTimeOffset? LastCutoffUtc { get; private set; }

        public int HistoryCalls { get; private set; }

        public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset atUtc,
            int limit,
            CancellationToken cancellationToken = default)
        {
            HistoryCalls++;
            LastCutoffUtc = atUtc;
            return Task.FromResult<IReadOnlyList<StoredMarketCandle>>([.. candles
                .Where(candle => candle.Timeframe == timeframe)
                .Take(limit)]);
        }

        public Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(MarketDataQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(string symbol, MarketTimeframe timeframe, int limit, bool completedOnly, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MarketDataAvailability> GetAvailabilityAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
