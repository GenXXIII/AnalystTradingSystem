using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.MarketData;

namespace XauAi.UnitTests.MarketData;

public sealed class MarketDataSynchronizationServiceTests
{
    private static readonly DateTimeOffset StartUtc = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Synchronization_batches_validates_and_excludes_forming_candle()
    {
        var store = new InMemoryStore();
        var provider = new FakeProvider(GenerateCandles);
        var tracker = new FakeStateStore();
        var service = CreateService(provider, store, tracker, batchSize: 3, nowUtc: StartUtc.AddHours(6.5));

        var result = await service.SynchronizeAsync(new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            StartUtc,
            StartUtc.AddHours(6.5),
            IncludeFormingCandle: false));

        Assert.Equal(3, result.BatchesProcessed);
        Assert.Equal(7, result.Received);
        Assert.Equal(6, result.Accepted);
        Assert.Equal(6, result.Inserted);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(6, store.Candles.Count);
        Assert.True(tracker.Completed);
    }

    [Fact]
    public async Task Incremental_sync_starts_after_latest_completed_candle()
    {
        var store = new InMemoryStore();
        await store.SaveAsync([CreateCandle(StartUtc.AddHours(2), MarketTimeframe.H1, complete: true)]);
        var provider = new FakeProvider(GenerateCandles);
        var service = CreateService(provider, store, new FakeStateStore(), 100, StartUtc.AddHours(5));

        await service.SynchronizeAsync(new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            FromUtc: null,
            ToUtc: StartUtc.AddHours(5),
            IncludeFormingCandle: false));

        Assert.Equal(StartUtc.AddHours(3), provider.Requests.Single().FromUtc);
    }

    [Fact]
    public async Task Incremental_sync_recovers_session_gap_saved_before_startup_history_runs()
    {
        var fridayLastCandle = new DateTimeOffset(2026, 10, 2, 20, 45, 0, TimeSpan.Zero);
        var sundayLatestCandle = new DateTimeOffset(2026, 10, 4, 23, 0, 0, TimeSpan.Zero);
        var requestedToUtc = sundayLatestCandle.AddMinutes(45);
        var store = new InMemoryStore();
        await store.SaveAsync(
        [
            CreateCandle(fridayLastCandle, MarketTimeframe.M15, complete: true),
            CreateCandle(sundayLatestCandle, MarketTimeframe.M15, complete: true)
        ]);
        var provider = new FakeProvider(GenerateCandles);
        var service = CreateService(
            provider,
            store,
            new FakeStateStore(),
            batchSize: 100,
            nowUtc: requestedToUtc,
            timeframe: MarketTimeframe.M15,
            initialHistoryDays: 3);

        await service.SynchronizeAsync(new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.M15,
            FromUtc: null,
            ToUtc: requestedToUtc,
            IncludeFormingCandle: false));

        Assert.Equal(
            new DateTimeOffset(2026, 10, 4, 22, 0, 0, TimeSpan.Zero),
            provider.Requests.Single().FromUtc);
        Assert.Contains(store.Candles, candle =>
            candle.Timeframe == MarketTimeframe.M15
            && candle.OpenTimeUtc == new DateTimeOffset(2026, 10, 4, 22, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Incremental_sync_backfills_when_stored_history_is_below_target()
    {
        var store = new InMemoryStore();
        await store.SaveAsync([CreateCandle(StartUtc.AddHours(2), MarketTimeframe.H1, complete: true)]);
        var provider = new FakeProvider(GenerateCandles);
        var service = CreateService(
            provider,
            store,
            new FakeStateStore(),
            batchSize: 100,
            nowUtc: StartUtc.AddHours(5),
            historyTargetCandles: 50);

        await service.SynchronizeAsync(new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            FromUtc: null,
            ToUtc: StartUtc.AddHours(5),
            IncludeFormingCandle: false,
            EnsureHistoryTarget: true));

        Assert.Equal(StartUtc.AddDays(-4).AddHours(5), provider.Requests[0].FromUtc);
        Assert.True(store.Candles.Count >= 50);
    }

    [Fact]
    public async Task Transient_failure_uses_bounded_retry_and_succeeds()
    {
        var failuresRemaining = 2;
        var provider = new FakeProvider((timeframe, from, to) =>
        {
            if (failuresRemaining-- > 0)
            {
                throw new MarketDataException(MarketDataErrorCodes.ProviderTimeout, "Timed out.");
            }

            return GenerateCandles(timeframe, from, to);
        });
        var service = CreateService(
            provider,
            new InMemoryStore(),
            new FakeStateStore(),
            100,
            StartUtc.AddHours(3),
            maxRetries: 2,
            retryDelaySeconds: 0);

        var result = await service.SynchronizeAsync(new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            StartUtc,
            StartUtc.AddHours(2.5)));

        Assert.Equal(3, provider.CallCount);
        Assert.Equal(3, result.Inserted);
    }

    [Fact]
    public async Task Failed_batch_preserves_prior_batch_and_next_run_resumes()
    {
        var store = new InMemoryStore();
        var tracker = new FakeStateStore();
        var failingProvider = new FakeProvider((timeframe, from, to) =>
        {
            if (from >= StartUtc.AddHours(3))
            {
                throw new MarketDataException(MarketDataErrorCodes.ProviderAuthenticationFailed, "Authentication failed.");
            }

            return GenerateCandles(timeframe, from, to);
        });
        var firstService = CreateService(failingProvider, store, tracker, 3, StartUtc.AddHours(6.5));

        await Assert.ThrowsAsync<MarketDataException>(() => firstService.SynchronizeAsync(
            new MarketDataSynchronizationRequest(
                "XAUUSD",
                MarketTimeframe.H1,
                StartUtc,
                StartUtc.AddHours(6.5))));

        Assert.Equal(3, store.Candles.Count);
        Assert.True(tracker.Failed);
        var recoveringProvider = new FakeProvider(GenerateCandles);
        var secondService = CreateService(recoveringProvider, store, tracker, 3, StartUtc.AddHours(6.5));
        var recovered = await secondService.SynchronizeAsync(new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            FromUtc: null,
            ToUtc: StartUtc.AddHours(6.5)));

        Assert.Equal(StartUtc.AddHours(3), recoveringProvider.Requests[0].FromUtc);
        Assert.Equal(6, store.Candles.Count);
        Assert.Equal(3, recovered.Inserted);
    }

    [Fact]
    public async Task Repeating_explicit_range_is_idempotent()
    {
        var store = new InMemoryStore();
        var service = CreateService(
            new FakeProvider(GenerateCandles),
            store,
            new FakeStateStore(),
            100,
            StartUtc.AddHours(4));
        var request = new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            StartUtc,
            StartUtc.AddHours(3.5));

        var first = await service.SynchronizeAsync(request);
        var second = await service.SynchronizeAsync(request);

        Assert.Equal(4, first.Inserted);
        Assert.Equal(0, second.Inserted);
        Assert.Equal(4, second.Skipped);
        Assert.Equal(4, store.Candles.Count);
    }

    [Fact]
    public async Task Exact_final_batch_boundary_is_not_omitted()
    {
        var store = new InMemoryStore();
        var service = CreateService(
            new FakeProvider(GenerateCandles),
            store,
            new FakeStateStore(),
            batchSize: 4,
            nowUtc: StartUtc.AddHours(3));

        var result = await service.SynchronizeAsync(new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            StartUtc,
            StartUtc.AddHours(2)));

        Assert.Equal(3, result.Inserted);
        Assert.Contains(store.Candles, candle => candle.OpenTimeUtc == StartUtc.AddHours(2));
    }

    [Fact]
    public async Task Same_symbol_and_timeframe_synchronizations_are_serialized()
    {
        var provider = new ConcurrencyTrackingProvider();
        var store = new InMemoryStore();
        var service = CreateService(provider, store, new FakeStateStore(), 100, StartUtc.AddHours(4));
        var request = new MarketDataSynchronizationRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            StartUtc,
            StartUtc.AddHours(2));

        await Task.WhenAll(
            service.SynchronizeAsync(request),
            service.SynchronizeAsync(request));

        Assert.Equal(1, provider.MaxConcurrentCalls);
        Assert.Equal(3, store.Candles.Count);
    }

    private static MarketDataSynchronizationService CreateService(
        IMarketDataProvider provider,
        InMemoryStore store,
        IMarketDataSyncStateStore tracker,
        int batchSize,
        DateTimeOffset nowUtc,
        int maxRetries = 0,
        int retryDelaySeconds = 0,
        MarketTimeframe timeframe = MarketTimeframe.H1,
        int initialHistoryDays = 1,
        int historyTargetCandles = 250) =>
        new(
            provider,
            store,
            store,
            tracker,
            new DefaultMarketSessionCalendar(),
            new MarketDataPipelineSettings
            {
                Symbol = "XAUUSD",
                Timeframes = [timeframe],
                InitialHistoryDays = initialHistoryDays,
                HistoryTargetCandles = historyTargetCandles,
                BatchSize = batchSize,
                MaxQueryRangeDays = 30,
                MaxRetries = maxRetries,
                RetryBaseDelaySeconds = retryDelaySeconds,
                MaxGapResults = 100
            },
            new FixedTimeProvider(nowUtc),
            NullLogger<MarketDataSynchronizationService>.Instance);

    private static IReadOnlyList<MarketCandleSnapshot> GenerateCandles(
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc)
    {
        var candles = new List<MarketCandleSnapshot>();
        for (var cursor = timeframe.AlignDown(fromUtc); cursor <= toUtc; cursor = cursor.Add(timeframe.Duration()))
        {
            if (cursor < fromUtc)
            {
                continue;
            }

            candles.Add(CreateCandle(cursor, timeframe, complete: true));
        }

        return candles;
    }

    private static MarketCandleSnapshot CreateCandle(
        DateTimeOffset openTimeUtc,
        MarketTimeframe timeframe,
        bool complete) =>
        new(
            "XAUUSD",
            "XAUUSD.test",
            timeframe,
            openTimeUtc,
            openTimeUtc.Add(timeframe.Duration()),
            2300,
            2302,
            2299,
            2301,
            100,
            0,
            20,
            complete,
            "UTC",
            openTimeUtc.AddMinutes(1));

    private sealed class FakeProvider(
        Func<MarketTimeframe, DateTimeOffset, DateTimeOffset, IReadOnlyList<MarketCandleSnapshot>> handler)
        : IMarketDataProvider
    {
        public int CallCount { get; private set; }

        public List<(DateTimeOffset FromUtc, DateTimeOffset ToUtc)> Requests { get; } = [];

        public Task<MarketDataProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MarketCandleSnapshot>> GetCandlesAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Requests.Add((fromUtc, toUtc));
            return Task.FromResult(handler(timeframe, fromUtc, toUtc));
        }
    }

    private sealed class ConcurrencyTrackingProvider : IMarketDataProvider
    {
        private int _concurrentCalls;

        public int MaxConcurrentCalls { get; private set; }

        public Task<MarketDataProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async Task<IReadOnlyList<MarketCandleSnapshot>> GetCandlesAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            var concurrent = Interlocked.Increment(ref _concurrentCalls);
            MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, concurrent);
            try
            {
                await Task.Delay(25, cancellationToken);
                return GenerateCandles(timeframe, fromUtc, toUtc);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentCalls);
            }
        }
    }

    private sealed class InMemoryStore : IMarketCandleStore, IMarketDataQueryStore
    {
        public List<MarketCandleSnapshot> Candles { get; } = [];

        public Task<StoredCandleCursor?> GetLatestAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset notAfterUtc,
            CancellationToken cancellationToken = default)
        {
            var latest = Candles
                .Where(candle => candle.Timeframe == timeframe && candle.OpenTimeUtc <= notAfterUtc)
                .OrderByDescending(candle => candle.OpenTimeUtc)
                .FirstOrDefault();
            return Task.FromResult(latest is null
                ? null
                : new StoredCandleCursor(latest.OpenTimeUtc, latest.IsComplete));
        }

        public Task<CandleSaveResult> SaveAsync(
            IReadOnlyCollection<MarketCandleSnapshot> candles,
            CancellationToken cancellationToken = default)
        {
            var inserted = 0;
            var updated = 0;
            var skipped = 0;
            foreach (var candle in candles)
            {
                var existing = Candles.FindIndex(value =>
                    value.Timeframe == candle.Timeframe && value.OpenTimeUtc == candle.OpenTimeUtc);
                if (existing < 0)
                {
                    Candles.Add(candle);
                    inserted++;
                }
                else if (!Candles[existing].IsComplete)
                {
                    Candles[existing] = candle;
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }

            return Task.FromResult(new CandleSaveResult(inserted, updated, skipped));
        }

        public Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(
            MarketDataQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([]);

        public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(
            string symbol,
            MarketTimeframe timeframe,
            int limit,
            bool completedOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([]);

        public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset atUtc,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([]);

        public Task<MarketDataAvailability> GetAvailabilityAsync(
            string symbol,
            MarketTimeframe timeframe,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new MarketDataAvailability(symbol, timeframe, Candles.Count, null, null, null));

        public Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DateTimeOffset>>([.. Candles
                .Where(candle =>
                    candle.Timeframe == timeframe
                    && candle.OpenTimeUtc >= fromUtc
                    && candle.OpenTimeUtc <= toUtc)
                .Select(candle => candle.OpenTimeUtc)
                .OrderBy(value => value)]);
    }

    private sealed class FakeStateStore : IMarketDataSyncStateStore
    {
        public bool Completed { get; private set; }

        public bool Failed { get; private set; }

        public Task<Guid> StartRunAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            DateTimeOffset startedAtUtc,
            CancellationToken cancellationToken = default) => Task.FromResult(Guid.NewGuid());

        public Task CompleteRunAsync(
            Guid runId,
            MarketDataPipelineResult result,
            DateTimeOffset completedAtUtc,
            CancellationToken cancellationToken = default)
        {
            Completed = true;
            return Task.CompletedTask;
        }

        public Task FailRunAsync(
            Guid runId,
            MarketDataSyncProgress progress,
            string errorCode,
            string safeMessage,
            DateTimeOffset failedAtUtc,
            CancellationToken cancellationToken = default)
        {
            Failed = true;
            return Task.CompletedTask;
        }

        public Task<MarketDataSyncStatus?> GetStatusAsync(
            string symbol,
            MarketTimeframe timeframe,
            CancellationToken cancellationToken = default) => Task.FromResult<MarketDataSyncStatus?>(null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
