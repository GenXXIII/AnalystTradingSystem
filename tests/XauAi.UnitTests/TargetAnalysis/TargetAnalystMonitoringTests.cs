using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.MarketData;
using XauAi.Application.TargetAnalysis;

namespace XauAi.UnitTests.TargetAnalysis;

public sealed class TargetAnalystMonitoringTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(2421, 2390, TargetAnalysisStatus.TargetHit)]
    [InlineData(2410, 2379, TargetAnalysisStatus.Invalidated)]
    public async Task Active_target_transitions_when_price_hits_a_terminal_level(
        decimal high,
        decimal low,
        TargetAnalysisStatus expectedStatus)
    {
        var target = ActiveTarget(Now.AddHours(1));
        var store = new FakeTargetStore(target);
        var candle = Candle(high, low);
        var service = Service(store, new FakeMarketStore([candle]));

        await service.MonitorActiveAsync();

        var transition = Assert.Single(store.Transitions);
        Assert.Equal(expectedStatus, transition.NewStatus);
        Assert.Equal(expectedStatus == TargetAnalysisStatus.TargetHit ? "TARGET_HIT" : "INVALIDATED", transition.EventType);
    }

    [Fact]
    public async Task Active_target_expires_without_waiting_for_a_new_candle()
    {
        var target = ActiveTarget(Now.AddSeconds(-1));
        var store = new FakeTargetStore(target);
        var market = new FakeMarketStore([]);
        var service = Service(store, market);

        await service.MonitorActiveAsync();

        var transition = Assert.Single(store.Transitions);
        Assert.Equal(TargetAnalysisStatus.Expired, transition.NewStatus);
        Assert.Equal("EXPIRED", transition.EventType);
        Assert.Equal(0, market.HistoryRequests);
    }

    private static TargetAnalystService Service(
        ITargetAnalysisStore store,
        IMarketDataQueryStore marketData) => new(
        store,
        null!,
        null!,
        null!,
        marketData,
        null!,
        null!,
        null!,
        null!,
        new TargetAnalystSettings { Enabled = true, Symbol = "XAUUSD" },
        new FixedTimeProvider(Now),
        NullLogger<TargetAnalystService>.Instance);

    private static TargetAnalysisResult ActiveTarget(DateTimeOffset validUntil) => new(
        Guid.NewGuid(),
        "XAUUSD",
        Now.AddMinutes(-5),
        2400m,
        MarketTimeframe.M5,
        2420m,
        2380m,
        TargetDirectionContext.Upward,
        0.8m,
        validUntil,
        [],
        [],
        null,
        "Supported target.",
        "Moderate uncertainty.",
        null,
        TargetAnalysisStatus.Active,
        "test-provider",
        "test-model",
        "phase13-master-v1",
        "phase13-v1",
        null,
        Now.AddMinutes(-5),
        Now.AddMinutes(-5),
        null);

    private static StoredMarketCandle Candle(decimal high, decimal low) => new(
        "XAUUSD",
        "GOLD",
        MarketTimeframe.M5,
        Now.AddMinutes(-5),
        Now,
        2400m,
        high,
        low,
        2405m,
        100m,
        null,
        null,
        true,
        Now,
        "test");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeTargetStore(TargetAnalysisResult active) : ITargetAnalysisStore
    {
        public List<TargetLifecycleTransition> Transitions { get; } = [];

        public Task<IReadOnlyList<TargetAnalysisResult>> GetActiveAsync(
            string symbol,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TargetAnalysisResult>>([active]);

        public Task<bool> TryTransitionAsync(
            TargetLifecycleTransition transition,
            CancellationToken cancellationToken = default)
        {
            Transitions.Add(transition);
            return Task.FromResult(true);
        }

        public Task<TargetAnalysisResult> CreateJobAsync(TargetAnalysisJobWriteModel job, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateSnapshotAsync(TargetSnapshotWriteModel snapshot, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TargetAnalysisResult> CompleteAsync(TargetAnalysisCompletionWriteModel completion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TargetAnalysisResult> CompleteNoValidTargetAsync(TargetNoValidTargetWriteModel completion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TargetAnalysisResult?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedTargetAnalyses> QueryAsync(TargetAnalysisQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TargetLifecycleItem>> GetLifecycleAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeMarketStore(IReadOnlyList<StoredMarketCandle> candles) : IMarketDataQueryStore
    {
        public int HistoryRequests { get; private set; }

        public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset atUtc,
            int limit,
            CancellationToken cancellationToken = default)
        {
            HistoryRequests++;
            return Task.FromResult(candles);
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
