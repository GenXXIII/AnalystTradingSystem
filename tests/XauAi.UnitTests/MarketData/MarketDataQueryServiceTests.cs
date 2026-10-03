using XauAi.Application.MarketData;

namespace XauAi.UnitTests.MarketData;

public sealed class MarketDataQueryServiceTests
{
    private static readonly DateTimeOffset StartUtc = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Excessive_limit_is_rejected_before_store_access()
    {
        var store = new FakeQueryStore();
        var service = CreateService(store, maxLimit: 100);

        var exception = await Assert.ThrowsAsync<MarketDataException>(() => service.GetRangeAsync(
            new MarketDataQuery(
                "XAUUSD",
                MarketTimeframe.H1,
                StartUtc,
                StartUtc.AddDays(1),
                101,
                true)));

        Assert.Equal(MarketDataErrorCodes.QueryLimitExceeded, exception.Code);
        Assert.Equal(0, store.CallCount);
    }

    [Fact]
    public async Task Excessive_date_range_is_rejected()
    {
        var service = CreateService(new FakeQueryStore(), maxRangeDays: 7);

        var exception = await Assert.ThrowsAsync<MarketDataException>(() => service.GetRangeAsync(
            new MarketDataQuery(
                "XAUUSD",
                MarketTimeframe.H1,
                StartUtc,
                StartUtc.AddDays(8),
                100,
                true)));

        Assert.Equal(MarketDataErrorCodes.QueryRangeTooLarge, exception.Code);
    }

    [Fact]
    public async Task Latest_completed_never_returns_forming_candle()
    {
        var store = new FakeQueryStore
        {
            Latest =
            [
                Candle(StartUtc.AddHours(2), complete: true)
            ]
        };
        var service = CreateService(store);

        var result = await service.GetLastCompletedAsync("XAUUSD", MarketTimeframe.H1);

        Assert.NotNull(result);
        Assert.True(store.LastCompletedOnly);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public async Task Empty_range_returns_an_empty_controlled_result()
    {
        var service = CreateService(new FakeQueryStore());

        var result = await service.GetRangeAsync(new MarketDataQuery(
            "XAUUSD",
            MarketTimeframe.M5,
            StartUtc,
            StartUtc.AddHours(1),
            50,
            true));

        Assert.Empty(result.Candles);
        Assert.Equal(50, result.Limit);
    }

    private static MarketDataQueryService CreateService(
        FakeQueryStore store,
        int maxLimit = 500,
        int maxRangeDays = 30) =>
        new(
            store,
            new FakeStateStore(),
            new DefaultMarketSessionCalendar(),
            new MarketDataPipelineSettings
            {
                Symbol = "XAUUSD",
                Timeframes = [MarketTimeframe.M5, MarketTimeframe.H1],
                MaxApiLimit = maxLimit,
                MaxQueryRangeDays = maxRangeDays,
                MaxGapResults = 100
            });

    private static StoredMarketCandle Candle(DateTimeOffset openTime, bool complete) =>
        new(
            "XAUUSD",
            "XAUUSD.test",
            MarketTimeframe.H1,
            openTime,
            openTime.AddHours(1),
            2,
            3,
            1,
            2.5m,
            100,
            0,
            10,
            complete,
            openTime.AddMinutes(1));

    private sealed class FakeQueryStore : IMarketDataQueryStore
    {
        public int CallCount { get; private set; }

        public bool LastCompletedOnly { get; private set; }

        public IReadOnlyList<StoredMarketCandle> Latest { get; init; } = [];

        public Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(
            MarketDataQuery query,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<StoredMarketCandle>>([]);
        }

        public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(
            string symbol,
            MarketTimeframe timeframe,
            int limit,
            bool completedOnly,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCompletedOnly = completedOnly;
            return Task.FromResult(Latest);
        }

        public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset atUtc,
            int limit,
            CancellationToken cancellationToken = default) => Task.FromResult(Latest);

        public Task<MarketDataAvailability> GetAvailabilityAsync(
            string symbol,
            MarketTimeframe timeframe,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new MarketDataAvailability(symbol, timeframe, 0, null, null, null));

        public Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DateTimeOffset>>([]);
    }

    private sealed class FakeStateStore : IMarketDataSyncStateStore
    {
        public Task<Guid> StartRunAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            DateTimeOffset startedAtUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task CompleteRunAsync(
            Guid runId,
            MarketDataPipelineResult result,
            DateTimeOffset completedAtUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task FailRunAsync(
            Guid runId,
            MarketDataSyncProgress progress,
            string errorCode,
            string safeMessage,
            DateTimeOffset failedAtUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<MarketDataSyncStatus?> GetStatusAsync(
            string symbol,
            MarketTimeframe timeframe,
            CancellationToken cancellationToken = default) => Task.FromResult<MarketDataSyncStatus?>(null);
    }
}
