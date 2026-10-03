using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.News;

namespace XauAi.UnitTests.News;

public sealed class NewsCollectionServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-02T10:00:00Z");

    [Fact]
    public async Task Uses_incremental_overlap_pages_and_filters_before_persistence()
    {
        var provider = new FakeProvider([
            new NewsProviderPage([
                Article("one", "Gold rises after Federal Reserve decision", Now.AddMinutes(-20)),
                Article("two", "Local football result", Now.AddMinutes(-10))
            ], "next-token", 3),
            new NewsProviderPage([
                Article("three", null, Now.AddMinutes(-5))
            ], null, 3)
        ]);
        var state = new FakeStateStore
        {
            Current = State(lastSuccess: Now.AddHours(-1))
        };
        var articleStore = new FakeArticleStore();
        var service = CreateService(provider, state, articleStore, new FakeRetryDelay());

        var result = await service.CollectAsync(new NewsCollectionRequest());

        Assert.Equal(Now.AddMinutes(-70), result.RequestedFromUtc);
        Assert.Equal(Now, result.RequestedToUtc);
        Assert.Equal(2, result.RequestsMade);
        Assert.Equal(3, result.ArticlesReceived);
        Assert.Equal(1, result.ArticlesInserted);
        Assert.Equal(1, result.ArticlesSkipped);
        Assert.Equal(1, result.ArticlesRejected);
        Assert.Equal(2, provider.Requests.Count);
        Assert.Null(provider.Requests[0].PageToken);
        Assert.Equal("next-token", provider.Requests[1].PageToken);
        Assert.Single(articleStore.Articles);
        Assert.Equal(NewsRelevanceLevel.VeryHigh, articleStore.Articles[0].Relevance);
        Assert.NotNull(state.CompletedMetrics);
        Assert.Equal(2, state.CompletedMetrics.RequestsMade);
    }

    [Fact]
    public async Task Final_rate_limit_failure_records_every_attempt_and_bounded_retries()
    {
        var provider = new FakeProvider([
            RateLimited(),
            RateLimited(),
            RateLimited()
        ]);
        var state = new FakeStateStore();
        var delay = new FakeRetryDelay();
        var service = CreateService(provider, state, new FakeArticleStore(), delay);

        var exception = await Assert.ThrowsAsync<NewsException>(() =>
            service.CollectAsync(new NewsCollectionRequest(Now.AddHours(-1), Now)));

        Assert.Equal(NewsErrorCodes.RateLimited, exception.Code);
        Assert.Equal(3, provider.Requests.Count);
        Assert.Equal(2, delay.Delays.Count);
        Assert.Equal([TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(7)], delay.Delays);
        Assert.NotNull(state.FailedMetrics);
        Assert.Equal(3, state.FailedMetrics.RequestsMade);
        Assert.Equal(3, state.FailedMetrics.RateLimitResponses);
        Assert.Equal(NewsErrorCodes.RateLimited, state.FailureCode);
    }

    [Fact]
    public async Task Rejects_archive_window_when_plan_access_is_not_enabled()
    {
        var service = CreateService(
            new FakeProvider([]),
            new FakeStateStore(),
            new FakeArticleStore(),
            new FakeRetryDelay());

        var exception = await Assert.ThrowsAsync<NewsException>(() => service.CollectAsync(
            new NewsCollectionRequest(Now.AddDays(-3), Now)));

        Assert.Equal(NewsErrorCodes.InvalidRequest, exception.Code);
    }

    private static INewsCollectionService CreateService(
        INewsProvider provider,
        INewsCollectionStateStore state,
        INewsArticleStore articleStore,
        INewsRetryDelay delay)
    {
        var settings = new NewsSettings
        {
            Enabled = true,
            CollectionOverlapMinutes = 10,
            InitialLookbackHours = 24,
            MaximumPagesPerCollection = 3,
            MaxRetries = 2,
            RetryBaseDelaySeconds = 1,
            MinimumRelevance = NewsRelevanceLevel.Medium,
            ArchiveEnabled = false
        };
        var classifier = new NewsRelevanceClassifier(settings);
        return new NewsCollectionService(
            provider,
            new NewsArticleNormalizer(classifier),
            articleStore,
            state,
            delay,
            settings,
            new FixedTimeProvider(Now),
            NullLogger<NewsCollectionService>.Instance);
    }

    private static ProviderNewsArticle Article(
        string id,
        string? title,
        DateTimeOffset publishedAt) => new(
        id,
        title,
        null,
        null,
        $"https://example.test/{id}",
        "Example Wire",
        "https://example.test",
        [],
        publishedAt,
        "en",
        ["us"],
        [],
        [],
        null);

    private static NewsCollectionStateResult State(DateTimeOffset lastSuccess) => new(
        "newsdata", "Completed", lastSuccess, lastSuccess, lastSuccess.AddHours(-1), lastSuccess,
        0, 1, 0, 1, 1, 0, 0, 10, null, null);

    private static NewsException RateLimited() => new(
        NewsErrorCodes.RateLimited,
        "Rate limited.",
        transient: true,
        retryAfter: TimeSpan.FromSeconds(7));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeProvider(IReadOnlyList<object> outcomes) : INewsProvider
    {
        private int _index;

        public List<NewsProviderRequest> Requests { get; } = [];

        public Task<NewsProviderPage> GetNewsAsync(
            NewsProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            var outcome = outcomes[_index++];
            return outcome is Exception exception
                ? Task.FromException<NewsProviderPage>(exception)
                : Task.FromResult((NewsProviderPage)outcome);
        }

        public Task<NewsProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsProviderStatus(
                "NewsData", NewsProviderState.Available, true, "Test", Now));
    }

    private sealed class FakeArticleStore : INewsArticleStore
    {
        public IReadOnlyList<NormalizedNewsArticle> Articles { get; private set; } = [];

        public Task<NewsPersistenceResult> PersistAsync(
            IReadOnlyList<NormalizedNewsArticle> articles,
            CancellationToken cancellationToken = default)
        {
            Articles = articles;
            return Task.FromResult(new NewsPersistenceResult(articles.Count, 0));
        }
    }

    private sealed class FakeStateStore : INewsCollectionStateStore
    {
        public NewsCollectionStateResult? Current { get; init; }

        public NewsRunMetrics? CompletedMetrics { get; private set; }

        public NewsRunMetrics? FailedMetrics { get; private set; }

        public string? FailureCode { get; private set; }

        public Task<NewsCollectionStateResult?> GetAsync(
            string providerKey,
            CancellationToken cancellationToken = default) => Task.FromResult(Current);

        public Task<Guid> StartAsync(
            string providerKey,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            DateTimeOffset startedAtUtc,
            CancellationToken cancellationToken = default) => Task.FromResult(Guid.NewGuid());

        public Task CompleteAsync(
            Guid runId,
            string providerKey,
            DateTimeOffset completedAtUtc,
            NewsRunMetrics metrics,
            CancellationToken cancellationToken = default)
        {
            CompletedMetrics = metrics;
            return Task.CompletedTask;
        }

        public Task FailAsync(
            Guid runId,
            string providerKey,
            DateTimeOffset failedAtUtc,
            NewsRunMetrics metrics,
            string errorCode,
            string safeMessage,
            CancellationToken cancellationToken = default)
        {
            FailedMetrics = metrics;
            FailureCode = errorCode;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRetryDelay : INewsRetryDelay
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }
}
