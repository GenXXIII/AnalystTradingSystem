using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.UnitTests.LocalAnalysis;

public sealed class LocalAnalystServiceTests
{
    private static readonly DateTimeOffset StartUtc = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Same_completed_candle_returns_checkpoint_without_reprocessing()
    {
        var candles = Candles();
        var checkpointSnapshot = Snapshot(candles[^1], LocalSignalState.Buy, "ACTIVE", validUntil: StartUtc.AddDays(2));
        var store = new RecordingStore
        {
            Checkpoint = new LocalAnalystCheckpoint(
                "XAUUSD",
                MarketTimeframe.M5,
                candles[^1].OpenTimeUtc,
                LocalSignalState.Buy,
                null,
                StartUtc,
                checkpointSnapshot)
        };
        var service = CreateService(candles, store, Decision(candles[^1], LocalSignalState.Buy));

        var result = await service.EvaluateAsync("XAUUSD", MarketTimeframe.M5);

        Assert.True(result.IsCached);
        Assert.Equal("LOCAL-XAUUSD-M5-1", result.SignalId);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task Same_candle_is_reprocessed_after_configuration_change()
    {
        var candles = Candles();
        var store = new RecordingStore
        {
            Checkpoint = new LocalAnalystCheckpoint(
                "XAUUSD",
                MarketTimeframe.M5,
                candles[^1].OpenTimeUtc,
                LocalSignalState.Nothing,
                "SCORE_BELOW_THRESHOLD",
                StartUtc,
                Snapshot(candles[^1], LocalSignalState.Nothing, "NONE", StartUtc.AddDays(2)))
        };
        var service = CreateService(
            candles,
            store,
            Decision(candles[^1], LocalSignalState.Buy),
            configurationVersion: "phase12-v2");

        var result = await service.EvaluateAsync("XAUUSD", MarketTimeframe.M5);

        Assert.False(result.IsCached);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task Active_signal_is_not_stopped_on_its_origin_candle_after_configuration_change()
    {
        var candles = Candles();
        var active = Snapshot(
            candles[^1],
            LocalSignalState.Buy,
            "ACTIVE",
            validUntil: StartUtc.AddDays(2),
            persistenceId: Guid.NewGuid());
        var store = new RecordingStore
        {
            Active = active,
            Checkpoint = new LocalAnalystCheckpoint(
                "XAUUSD",
                MarketTimeframe.M5,
                candles[^1].OpenTimeUtc,
                LocalSignalState.Buy,
                null,
                StartUtc,
                active)
        };
        var weakDecision = Decision(candles[^1], LocalSignalState.Buy) with
        {
            Score = 0m,
            Confidence = 0m,
            Conditions = []
        };
        var service = CreateService(candles, store, weakDecision, configurationVersion: "phase12-v2");

        var result = await service.EvaluateAsync("XAUUSD", MarketTimeframe.M5);

        Assert.True(result.IsCached);
        Assert.Equal(LocalSignalState.Buy, result.State);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task Same_candle_is_reprocessed_after_retryable_data_failure()
    {
        var candles = Candles();
        var store = new RecordingStore
        {
            Checkpoint = new LocalAnalystCheckpoint(
                "XAUUSD",
                MarketTimeframe.M5,
                candles[^1].OpenTimeUtc,
                LocalSignalState.Nothing,
                "INSUFFICIENT_HISTORY",
                StartUtc,
                Snapshot(candles[^1], LocalSignalState.Nothing, "NONE", StartUtc.AddDays(2)))
        };
        var service = CreateService(candles, store, Decision(candles[^1], LocalSignalState.Sell));

        var result = await service.EvaluateAsync("XAUUSD", MarketTimeframe.M5);

        Assert.False(result.IsCached);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task Invalid_candle_is_recorded_as_nothing_and_never_generates_a_signal()
    {
        var candles = Candles().ToArray();
        var invalid = candles[^1];
        candles[^1] = invalid with { High = invalid.Close - 1m };
        var store = new RecordingStore();
        var service = CreateService(candles, store, Decision(candles[^1], LocalSignalState.Buy));

        var result = await service.EvaluateAsync("XAUUSD", MarketTimeframe.M5);

        Assert.Equal(LocalSignalState.Nothing, result.State);
        Assert.Equal("INVALID_OR_INCOMPLETE_CANDLE", result.Reason);
        Assert.NotNull(store.LastRequest);
        Assert.Equal(LocalSignalMutation.None, store.LastRequest!.Mutation);
        Assert.Equal(LocalSignalState.Nothing, store.LastRequest.Decision.State);
    }

    [Fact]
    public async Task Expired_active_setup_transitions_to_stop_instead_of_creating_duplicate_signal()
    {
        var candles = Candles();
        var active = Snapshot(
            candles[^2],
            LocalSignalState.Buy,
            "ACTIVE",
            validUntil: StartUtc.AddMinutes(-1),
            persistenceId: Guid.NewGuid());
        var store = new RecordingStore { Active = active };
        var service = CreateService(candles, store, Decision(candles[^1], LocalSignalState.Buy));

        var result = await service.EvaluateAsync("XAUUSD", MarketTimeframe.M5);

        Assert.NotNull(store.LastRequest);
        Assert.Equal(LocalSignalMutation.Stop, store.LastRequest!.Mutation);
        Assert.Equal(active.PersistenceId, store.LastRequest.ExistingSignalId);
        Assert.Equal(LocalSignalState.Stop, result.State);
        Assert.Equal("EXPIRED", result.Reason);
    }

    [Fact]
    public async Task Active_setup_stops_when_its_directional_confluence_falls_below_the_entry_threshold()
    {
        var candles = Candles();
        var active = Snapshot(
            candles[^2],
            LocalSignalState.Buy,
            "ACTIVE",
            validUntil: StartUtc.AddDays(2),
            persistenceId: Guid.NewGuid());
        var store = new RecordingStore { Active = active };
        var weakDecision = Decision(candles[^1], LocalSignalState.Buy) with
        {
            Score = 1m,
            Confidence = 0.166667m,
            Conditions = [new LocalSignalCondition("Trend", "WeakBullish", true, false, 1m, ["fixture"])]
        };
        var service = CreateService(candles, store, weakDecision);

        var result = await service.EvaluateAsync("XAUUSD", MarketTimeframe.M5);

        Assert.Equal(LocalSignalMutation.Stop, store.LastRequest!.Mutation);
        Assert.Equal(LocalSignalState.Stop, result.State);
        Assert.Equal("BUY_CONFLUENCE_LOST", result.Reason);
    }

    private static LocalAnalystService CreateService(
        IReadOnlyList<StoredMarketCandle> candles,
        RecordingStore store,
        LocalSignalDecision engineDecision,
        string configurationVersion = "phase12-v1") =>
        new(
            new TestMarketStore(candles),
            new TestTechnicalAnalysis(),
            new TestEngine(engineDecision),
            store,
            new DefaultMarketSessionCalendar(),
            new LocalAnalystSettings
            {
                Enabled = true,
                Timeframes = [MarketTimeframe.M5],
                HistoryLimit = 250,
                MinimumCandles = 205,
                MaximumAllowedGaps = 0,
                ConfigurationVersion = configurationVersion
            },
            new FixedTimeProvider(candles[^1].CloseTimeUtc.AddMinutes(1)),
            NullLogger<LocalAnalystService>.Instance);

    private static IReadOnlyList<StoredMarketCandle> Candles() =>
        [.. Enumerable.Range(0, 205).Select(index =>
        {
            var openTime = StartUtc.AddMinutes(index * 5);
            var close = 2000m + (index * 0.1m);
            return new StoredMarketCandle(
                "XAUUSD", "GOLD", MarketTimeframe.M5,
                openTime, openTime.AddMinutes(5), close - 0.05m, close + 0.5m, close - 0.5m, close,
                100m, null, null, true, openTime.AddMinutes(5));
        })];

    private static LocalSignalDecision Decision(StoredMarketCandle candle, LocalSignalState state) =>
        new(
            "XAUUSD", MarketTimeframe.M5, "XAUUSD-M5-test", candle.OpenTimeUtc, candle.CloseTimeUtc, state,
            candle.Close, 5m, 6m, 0.833333m,
            "BullishStructure", "NoConfirmedSweep", "BullishContextConfirmed", "Bullish", "SupportReaction", "Normal",
            candle.Close - 2m, candle.Close + 4m, null, candle.CloseTimeUtc.AddHours(1),
            [new LocalSignalCondition("Trend", "Bullish", true, false, 1m, ["fixture"])]);

    private static LocalSignalSnapshot Snapshot(
        StoredMarketCandle candle,
        LocalSignalState state,
        string status,
        DateTimeOffset validUntil,
        Guid? persistenceId = null) =>
        new(
            "LOCAL-XAUUSD-M5-1", "XAUUSD", MarketTimeframe.M5, "XAUUSD-M5-origin", candle.OpenTimeUtc,
            state, state, candle.Close, 5m, 6m, 0.833333m,
            "BullishStructure", "NoConfirmedSweep", "BullishContextConfirmed", "Bullish", "SupportReaction", "Normal",
            candle.Close - 2m, candle.Close + 4m, null, validUntil, status, "phase12-v1",
            [new LocalSignalCondition("Trend", "Bullish", true, false, 1m, ["fixture"])],
            StartUtc, StartUtc, StartUtc, null, false, persistenceId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestMarketStore(IReadOnlyList<StoredMarketCandle> candles) : IMarketDataQueryStore
    {
        public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(string symbol, MarketTimeframe timeframe, int limit, bool completedOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([.. candles.OrderByDescending(value => value.OpenTimeUtc)]);

        public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset atUtc, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult(candles);

        public Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(MarketDataQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MarketDataAvailability> GetAvailabilityAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestTechnicalAnalysis : ITechnicalAnalysisService
    {
        public Task<TechnicalAnalysisResult> AnalyzeAsync(TechnicalAnalysisRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TechnicalAnalysisResult(
                "XAUUSD", request.Timeframe, request.AtUtc ?? StartUtc, request.AtUtc,
                new TrendAnalysisResult(AnalyticalDirection.Bullish, "Strong", "BullishStack", "AboveAllAvailableEma", [], []),
                new MomentumAnalysisResult(
                    new RsiResult(14, 60m, 59m, IndicatorZone.Neutral, AnalyticalDirection.Bullish, AnalysisReadiness.Ready),
                    new MacdResult(12, 26, 9, 1m, 0.5m, 0.5m, 0.4m, CrossoverState.Bullish, AnalyticalDirection.Bullish, AnalysisReadiness.Ready),
                    new StochasticResult(14, 3, 60m, 55m, CrossoverState.Bullish, IndicatorZone.Neutral, AnalysisReadiness.Ready)),
                new VolatilityAnalysisResult(VolatilityRegime.Normal, 1m, 1m, new AtrResult(14, 2m, 1.8m, AnalysisReadiness.Ready), new BollingerBandsResult(20, 2m, 0m, 0m, 0m, 0m, 0m, AnalysisReadiness.Ready), []),
                new MarketStructureResult(AnalyticalDirection.Bullish, "HigherHighsAndHigherLows", [], AnalysisReadiness.Ready),
                [], [], null,
                new PriceActionResult(false, false, false, false, false, false, false, false, false, []),
                new IndicatorSet([], [], new RsiResult(14, 60m, 59m, IndicatorZone.Neutral, AnalyticalDirection.Bullish, AnalysisReadiness.Ready), new MacdResult(12, 26, 9, 1m, 0.5m, 0.5m, 0.4m, CrossoverState.Bullish, AnalyticalDirection.Bullish, AnalysisReadiness.Ready), new AtrResult(14, 2m, 1.8m, AnalysisReadiness.Ready), new AdxResult(14, 30m, 25m, 10m, "Strong", AnalyticalDirection.Bullish, AnalysisReadiness.Ready), new BollingerBandsResult(20, 2m, 0m, 0m, 0m, 0m, 0m, AnalysisReadiness.Ready), new StochasticResult(14, 3, 60m, 55m, CrossoverState.Bullish, IndicatorZone.Neutral, AnalysisReadiness.Ready)),
                new TechnicalConfluenceResult([], [], [], [], [], [], []), [],
                new AnalysisDiagnostics(1, 205, 205, 0, 0, 0, 10, 0, request.AtUtc ?? StartUtc)));

        public Task<MultiTimeframeAnalysisResult> AnalyzeMultiTimeframeAsync(string symbol, DateTimeOffset? atUtc = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestEngine(LocalSignalDecision decision) : ILocalSignalEngine
    {
        public LocalSignalDecision Evaluate(IReadOnlyList<StoredMarketCandle> candles, TechnicalAnalysisResult analysis, string symbol, MarketTimeframe timeframe, DateTimeOffset evaluatedAtUtc) => decision;
    }

    private sealed class RecordingStore : ILocalSignalStore
    {
        public LocalAnalystCheckpoint? Checkpoint { get; init; }
        public LocalSignalSnapshot? Active { get; init; }
        public LocalSignalPersistenceRequest? LastRequest { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<LocalAnalystCheckpoint?> GetCheckpointAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) => Task.FromResult(Checkpoint);
        public Task<LocalSignalSnapshot?> GetActiveAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) => Task.FromResult(Active);
        public Task<IReadOnlyList<LocalSignalSnapshot>> GetHistoryAsync(string symbol, MarketTimeframe? timeframe, int limit, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LocalSignalSnapshot>>([]);
        public Task<IReadOnlyList<LocalSignalLifecycleItem>> GetLifecycleAsync(string signalId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LocalSignalLifecycleItem>>([]);
        public Task<IReadOnlyList<LocalSignalChartMarker>> GetChartMarkersAsync(string symbol, MarketTimeframe timeframe, int limit, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LocalSignalChartMarker>>([]);
        public Task<IReadOnlyList<LocalAnalystCheckpoint>> GetCheckpointsAsync(string symbol, IReadOnlyList<MarketTimeframe> timeframes, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LocalAnalystCheckpoint>>([]);

        public Task<LocalSignalSnapshot> SaveAsync(LocalSignalPersistenceRequest request, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            LastRequest = request;
            var state = request.Mutation == LocalSignalMutation.Stop ? LocalSignalState.Stop : request.Decision.State;
            return Task.FromResult(new LocalSignalSnapshot(
                request.NewSignalId ?? Active?.SignalId,
                request.Decision.Symbol,
                request.Decision.Timeframe,
                request.Decision.SignalCandleId,
                request.Decision.SignalCandleTimeUtc,
                state,
                Active?.OriginDirection ?? (state is LocalSignalState.Buy or LocalSignalState.Sell ? state : null),
                request.Decision.SignalPrice,
                request.Decision.Score,
                request.Decision.MaxScore,
                request.Decision.Confidence,
                request.Decision.StructureState,
                request.Decision.LiquidityState,
                request.Decision.CandleState,
                request.Decision.MomentumState,
                request.Decision.KtrState,
                request.Decision.VolatilityState,
                request.Decision.InvalidationPrice,
                request.Decision.TargetPrice,
                request.Decision.Reason,
                request.Decision.ValidUntilUtc,
                request.Mutation == LocalSignalMutation.Stop ? "STOPPED" : "NONE",
                "phase12-v1",
                request.Decision.Conditions,
                request.EvaluatedAtUtc,
                Active?.CreatedAtUtc,
                request.EvaluatedAtUtc,
                request.Mutation == LocalSignalMutation.Stop ? request.EvaluatedAtUtc : null,
                false,
                request.ExistingSignalId));
        }
    }
}
