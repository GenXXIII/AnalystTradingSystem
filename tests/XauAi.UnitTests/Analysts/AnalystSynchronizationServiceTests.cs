using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.Analysts;

namespace XauAi.UnitTests.Analysts;

public sealed class AnalystSynchronizationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Synchronization_uses_incremental_overlap_pagination_retry_and_durable_state()
    {
        var provider = new PagedProvider();
        var state = new StateStore
        {
            Current = State(Now.AddHours(-2))
        };
        var ingestion = new IngestionStore();
        var retry = new RetryDelay();
        var settings = Settings(maxRetries: 1);
        var normalizer = new AnalystItemNormalizer(new AnalystRelevanceFilter(settings), settings);
        var service = new AnalystSynchronizationService(
            provider,
            normalizer,
            ingestion,
            state,
            retry,
            settings,
            new FixedTimeProvider(Now),
            NullLogger<AnalystSynchronizationService>.Instance);

        var result = await service.SynchronizeAsync(new AnalystSyncRequest());

        Assert.Equal(Now.AddHours(-2).AddMinutes(-30), state.StartedFromUtc);
        Assert.Equal(Now, state.StartedToUtc);
        Assert.Equal(3, result.RequestsMade);
        Assert.Equal(1, result.RateLimitResponses);
        Assert.Equal(2, result.ItemsReceived);
        Assert.Equal(2, result.PublicationsInserted);
        Assert.Equal(2, result.PredictionsInserted);
        Assert.Equal(2, ingestion.Items.Count);
        Assert.Equal(TimeSpan.FromSeconds(2), Assert.Single(retry.Delays));
        Assert.True(state.Completed);
        Assert.False(state.Failed);
        Assert.Equal(new[] { null, null, "next" }, provider.RequestCursors);
    }

    [Fact]
    public async Task Synchronization_marks_failure_without_deleting_existing_data()
    {
        var provider = new FailingProvider();
        var state = new StateStore();
        var ingestion = new IngestionStore();
        var settings = Settings(maxRetries: 0);
        var service = new AnalystSynchronizationService(
            provider,
            new AnalystItemNormalizer(new AnalystRelevanceFilter(settings), settings),
            ingestion,
            state,
            new RetryDelay(),
            settings,
            new FixedTimeProvider(Now),
            NullLogger<AnalystSynchronizationService>.Instance);

        var exception = await Assert.ThrowsAsync<AnalystException>(() =>
            service.SynchronizeAsync(new AnalystSyncRequest(Now.AddDays(-1), Now)));

        Assert.Equal(AnalystErrorCodes.InvalidResponse, exception.Code);
        Assert.True(state.Failed);
        Assert.False(state.Completed);
        Assert.Equal(AnalystErrorCodes.InvalidResponse, state.ErrorCode);
        Assert.Empty(ingestion.Items);
    }

    private static AnalystSettings Settings(int maxRetries) => new()
    {
        Enabled = true,
        Provider = "SyntheticTestProvider",
        ProviderKey = "analyst-rss",
        InitialLookbackDays = 7,
        CollectionOverlapMinutes = 30,
        ProviderPageSize = 10,
        MaximumPagesPerSync = 3,
        MaximumCollectionRangeDays = 30,
        MaxRetries = maxRetries,
        RetryBaseDelaySeconds = 2
    };

    private static AnalystSyncStateResult State(DateTimeOffset lastSuccessful) => new(
        "analyst-rss",
        "Healthy",
        lastSuccessful,
        lastSuccessful,
        lastSuccessful,
        "previous",
        0,
        new AnalystRunMetrics(1, 0, 1, 1, 1, 0, 0, 10),
        null,
        null);

    private static ProviderAnalystItem Item(string id, DateTimeOffset published) => new(
        id,
        $"Gold bullish outlook {id}",
        "Synthetic test fixture: gold is bullish toward $4,100 over 1 week.",
        null,
        $"https://research.example.test/{id}",
        published,
        null,
        "en",
        "Gold",
        new ProviderAnalystSource("source", "Synthetic Research", "Research", "https://research.example.test", "US"),
        null,
        []);

    private sealed class PagedProvider : IAnalystDataProvider
    {
        private int _calls;

        public List<string?> RequestCursors { get; } = [];

        public Task<AnalystProviderPage> GetLatestAnalystItemsAsync(
            AnalystProviderRequest request,
            CancellationToken cancellationToken = default) => GetPage(request);

        public Task<AnalystProviderPage> GetHistoricalAnalystItemsAsync(
            AnalystProviderRequest request,
            CancellationToken cancellationToken = default) => GetPage(request);

        public Task<ProviderAnalystItem?> GetAnalystItemAsync(string externalId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProviderAnalystItem?>(null);

        public Task<AnalystProviderPage> SearchAsync(AnalystProviderRequest request, CancellationToken cancellationToken = default) =>
            GetPage(request);

        public Task<AnalystProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AnalystProviderStatus("Test", AnalystProviderState.Available, true, "Test", Now));

        private Task<AnalystProviderPage> GetPage(AnalystProviderRequest request)
        {
            RequestCursors.Add(request.Cursor);
            _calls++;
            if (_calls == 1)
            {
                throw new AnalystException(
                    AnalystErrorCodes.RateLimited,
                    "Synthetic rate limit.",
                    transient: true);
            }

            return Task.FromResult(request.Cursor is null
                ? new AnalystProviderPage([Item("one", Now.AddHours(-1))], "next", 2)
                : new AnalystProviderPage([Item("two", Now.AddMinutes(-30))], null, 2));
        }
    }

    private sealed class FailingProvider : IAnalystDataProvider
    {
        public Task<AnalystProviderPage> GetLatestAnalystItemsAsync(AnalystProviderRequest request, CancellationToken cancellationToken = default) =>
            throw Failure();

        public Task<AnalystProviderPage> GetHistoricalAnalystItemsAsync(AnalystProviderRequest request, CancellationToken cancellationToken = default) =>
            throw Failure();

        public Task<ProviderAnalystItem?> GetAnalystItemAsync(string externalId, CancellationToken cancellationToken = default) =>
            throw Failure();

        public Task<AnalystProviderPage> SearchAsync(AnalystProviderRequest request, CancellationToken cancellationToken = default) =>
            throw Failure();

        public Task<AnalystProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AnalystProviderStatus("Test", AnalystProviderState.Unavailable, true, "Test", Now));

        private static AnalystException Failure() => new(
            AnalystErrorCodes.InvalidResponse,
            "Synthetic malformed response.");
    }

    private sealed class IngestionStore : IAnalystIngestionStore
    {
        public IReadOnlyList<NormalizedAnalystItem> Items { get; private set; } = [];

        public Task<AnalystPersistenceResult> PersistAsync(
            IReadOnlyList<NormalizedAnalystItem> items,
            CancellationToken cancellationToken = default)
        {
            Items = items;
            return Task.FromResult(new AnalystPersistenceResult(
                items.Count,
                items.Sum(item => item.Predictions.Count),
                0,
                items.Max(item => (DateTimeOffset?)item.PublishedAtUtc),
                items.OrderBy(item => item.PublishedAtUtc).LastOrDefault()?.ExternalId));
        }
    }

    private sealed class StateStore : IAnalystSyncStateStore
    {
        public AnalystSyncStateResult? Current { get; init; }

        public DateTimeOffset? StartedFromUtc { get; private set; }

        public DateTimeOffset? StartedToUtc { get; private set; }

        public bool Completed { get; private set; }

        public bool Failed { get; private set; }

        public string? ErrorCode { get; private set; }

        public Task<AnalystSyncStateResult?> GetAsync(string providerKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Current);

        public Task<Guid> StartAsync(
            string providerKey,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            DateTimeOffset startedAtUtc,
            CancellationToken cancellationToken = default)
        {
            StartedFromUtc = fromUtc;
            StartedToUtc = toUtc;
            return Task.FromResult(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        }

        public Task CompleteAsync(
            Guid runId,
            string providerKey,
            DateTimeOffset completedAtUtc,
            DateTimeOffset? lastPublishedAtUtc,
            string? lastExternalId,
            AnalystRunMetrics metrics,
            CancellationToken cancellationToken = default)
        {
            Completed = true;
            return Task.CompletedTask;
        }

        public Task FailAsync(
            Guid runId,
            string providerKey,
            DateTimeOffset failedAtUtc,
            AnalystRunMetrics metrics,
            string errorCode,
            string safeMessage,
            CancellationToken cancellationToken = default)
        {
            Failed = true;
            ErrorCode = errorCode;
            return Task.CompletedTask;
        }
    }

    private sealed class RetryDelay : IAnalystRetryDelay
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
