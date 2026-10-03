using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.EconomicData;

namespace XauAi.UnitTests.EconomicData;

public sealed class EconomicDataSynchronizationServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-03T00:00:00Z");
    private static readonly Guid SeriesId = Guid.Parse("80000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Synchronization_uses_revision_overlap_retries_rate_limits_and_persists_metrics()
    {
        var provider = new RetryingProvider();
        var seriesStore = new FakeSeriesStore(["TEST_CPI"]);
        var observationStore = new FakeObservationStore();
        var stateStore = new FakeStateStore(new DateOnly(2026, 9, 1));
        var delay = new FakeRetryDelay();
        var settings = Settings([new EconomicSeriesDefinition("TEST_CPI", "Inflation")]);
        var service = new EconomicDataSynchronizationService(
            provider,
            seriesStore,
            observationStore,
            stateStore,
            delay,
            settings,
            new FixedTimeProvider(Now),
            NullLogger<EconomicDataSynchronizationService>.Instance);

        var result = await service.SynchronizeAsync(new EconomicSyncRequest("TEST_CPI"));

        var seriesResult = Assert.Single(result.Series);
        Assert.Equal("Succeeded", seriesResult.Status);
        Assert.Equal(3, seriesResult.RequestsMade); // metadata + rate-limited observation + retry
        Assert.Equal(1, seriesResult.RateLimitResponses);
        Assert.Equal(1, seriesResult.RecordsInserted);
        Assert.Equal(TimeSpan.FromSeconds(7), Assert.Single(delay.Delays));
        Assert.Equal(new DateOnly(2026, 9, 1).AddDays(-370), stateStore.StartedFrom);
        Assert.Equal(new DateOnly(2026, 10, 3), stateStore.StartedTo);
        Assert.NotNull(stateStore.CompletedMetrics);
        Assert.Equal(3, stateStore.CompletedMetrics!.RequestsMade);
        Assert.Equal(stateStore.StartedFrom, provider.LastRequest?.From);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task Synchronization_isolates_a_series_failure_and_continues_other_configured_series()
    {
        var provider = new PartiallyFailingProvider();
        var stateStore = new FakeStateStore(null);
        var settings = Settings(
        [
            new EconomicSeriesDefinition("TEST_OK", "Inflation"),
            new EconomicSeriesDefinition("TEST_FAIL", "Employment")
        ]);
        var service = new EconomicDataSynchronizationService(
            provider,
            new FakeSeriesStore(["TEST_FAIL"]),
            new FakeObservationStore(),
            stateStore,
            new FakeRetryDelay(),
            settings,
            new FixedTimeProvider(Now),
            NullLogger<EconomicDataSynchronizationService>.Instance);

        var result = await service.SynchronizeAsync(new EconomicSyncRequest());

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Series, item => item.ExternalSeriesId == "TEST_OK" && item.Status == "Succeeded");
        Assert.Contains(result.Series, item =>
            item.ExternalSeriesId == "TEST_FAIL"
            && item.ErrorCode == EconomicDataErrorCodes.AuthenticationFailed);
        Assert.Equal(1, stateStore.Failures);
    }

    private static EconomicDataSettings Settings(IReadOnlyList<EconomicSeriesDefinition> series) => new()
    {
        Enabled = true,
        Provider = "FRED",
        ProviderKey = "fred",
        InitialHistoryYears = 20,
        RevisionLookbackDays = 370,
        ProviderPageSize = 100,
        MaximumPagesPerSeries = 2,
        MaximumQueryRangeYears = 100,
        MaxRetries = 2,
        RetryBaseDelaySeconds = 1,
        Series = series
    };

    private sealed class RetryingProvider : BaseProvider
    {
        private int _observationCalls;
        public EconomicObservationProviderRequest? LastRequest { get; private set; }

        public override Task<EconomicObservationProviderPage> GetObservationsAsync(
            EconomicObservationProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            _observationCalls++;
            if (_observationCalls == 1)
            {
                throw new EconomicDataException(
                    EconomicDataErrorCodes.RateLimited,
                    "TEST_ONLY rate limit",
                    transient: true,
                    retryAfter: TimeSpan.FromSeconds(7));
            }

            return Task.FromResult(Page());
        }
    }

    private sealed class PartiallyFailingProvider : BaseProvider
    {
        public override Task<ProviderEconomicSeries> GetSeriesMetadataAsync(
            string externalSeriesId,
            CancellationToken cancellationToken = default) =>
            externalSeriesId == "TEST_FAIL"
                ? throw new EconomicDataException(
                    EconomicDataErrorCodes.AuthenticationFailed,
                    "TEST_ONLY authentication failure")
                : Task.FromResult(Metadata(externalSeriesId));
    }

    private abstract class BaseProvider : IEconomicDataProvider
    {
        public virtual Task<ProviderEconomicSeries> GetSeriesMetadataAsync(string externalSeriesId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Metadata(externalSeriesId));

        public virtual Task<EconomicObservationProviderPage> GetObservationsAsync(EconomicObservationProviderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Page());

        public Task<ProviderEconomicObservation?> GetLatestObservationAsync(string externalSeriesId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProviderEconomicObservation?>(Page().Observations[0]);

        public Task<EconomicObservationProviderPage> GetObservationsSinceAsync(string externalSeriesId, DateOnly sinceExclusive, int offset, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult(Page());

        public Task<EconomicProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new EconomicProviderStatus("FRED", EconomicProviderState.Available, true, "TEST_ONLY", Now));

        protected static ProviderEconomicSeries Metadata(string externalSeriesId) =>
            new(externalSeriesId, $"TEST_ONLY {externalSeriesId}", null, "Test units", "Monthly", "Test adjustment", null, null, Now);

        protected static EconomicObservationProviderPage Page() =>
            new(
                [new ProviderEconomicObservation(new DateOnly(2026, 9, 1), 3.2m, "3.2", "Available", null, null)],
                1,
                0,
                100);
    }

    private sealed class FakeSeriesStore(IReadOnlyList<string> existingSeries) : IEconomicSeriesStore
    {
        public Task<EconomicSeriesResult> UpsertAsync(ProviderEconomicSeries metadata, EconomicSeriesDefinition definition, string providerKey, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EconomicSeriesResult(
                SeriesId, providerKey, metadata.ExternalSeriesId, metadata.Name, metadata.Description,
                metadata.Units, metadata.Frequency, metadata.SeasonalAdjustment, definition.CountryCode,
                definition.CurrencyCode, definition.Category, metadata.ObservationStartDate,
                metadata.ObservationEndDate, metadata.ProviderUpdatedAtUtc, true, updatedAtUtc, updatedAtUtc));

        public Task<IReadOnlyList<EconomicSeriesResult>> QueryAsync(EconomicSeriesQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EconomicSeriesResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EconomicSeriesResult?> GetByExternalIdAsync(string providerKey, string externalSeriesId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EconomicSeriesResult?>(existingSeries.Contains(externalSeriesId, StringComparer.OrdinalIgnoreCase)
                ? new EconomicSeriesResult(
                    SeriesId, providerKey, externalSeriesId, $"TEST_ONLY {externalSeriesId}", null,
                    "Test units", "Monthly", "Test adjustment", "US", "USD", "Test", null, null,
                    Now, true, Now, Now)
                : null);
    }

    private sealed class FakeObservationStore : IEconomicObservationStore
    {
        public Task<EconomicObservationPersistenceResult> PersistAsync(Guid economicSeriesId, IReadOnlyList<ProviderEconomicObservation> observations, DateTimeOffset fetchedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EconomicObservationPersistenceResult(
                observations.Count, 0, 0, observations.Max(item => (DateOnly?)item.ObservationDate)));

        public Task<PagedEconomicObservations> QueryAsync(EconomicObservationQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EconomicObservationResult?> GetLatestAsync(Guid economicSeriesId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EconomicObservationResult>> GetLatestForAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeStateStore(DateOnly? lastObservationDate) : IEconomicSyncStateStore
    {
        public DateOnly? StartedFrom { get; private set; }
        public DateOnly? StartedTo { get; private set; }
        public EconomicSyncMetrics? CompletedMetrics { get; private set; }
        public int Failures { get; private set; }

        public Task<EconomicSyncStateResult?> GetAsync(Guid economicSeriesId, CancellationToken cancellationToken = default) =>
            Task.FromResult(lastObservationDate.HasValue
                ? new EconomicSyncStateResult(
                    economicSeriesId, "TEST", "TEST_ONLY", "Healthy", Now, Now,
                    lastObservationDate, 0, new EconomicSyncMetrics(0, 0, 0, 0, 0, 0, 0), null, null)
                : null);

        public Task<IReadOnlyList<EconomicSyncStateResult>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Guid> StartAsync(Guid economicSeriesId, DateOnly from, DateOnly to, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default)
        {
            StartedFrom = from;
            StartedTo = to;
            return Task.FromResult(Guid.NewGuid());
        }

        public Task CompleteAsync(Guid runId, Guid economicSeriesId, DateTimeOffset completedAtUtc, DateOnly? latestObservationDate, EconomicSyncMetrics metrics, CancellationToken cancellationToken = default)
        {
            CompletedMetrics = metrics;
            return Task.CompletedTask;
        }

        public Task FailAsync(Guid runId, Guid economicSeriesId, DateTimeOffset failedAtUtc, EconomicSyncMetrics metrics, string errorCode, string safeMessage, CancellationToken cancellationToken = default)
        {
            Failures++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRetryDelay : IEconomicRetryDelay
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
