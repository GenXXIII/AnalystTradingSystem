using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.AI;

namespace XauAi.UnitTests.AI;

public sealed class AiInterpretationServiceTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task Unchanged_evidence_reuses_persisted_interpretation_without_another_provider_call()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, now.AddMinutes(-1));
        var store = new FakeStore(evidence);
        var runner = new FakeRunner(Configuration(), Completion(evidence.Id));
        var service = CreateService(store, runner, Configuration());
        var request = new CreateAiInterpretationRequest(
            "XAUUSD", AiSpecialist.News, AiInterpretationType.NewsEvent, null, now, 24);

        var first = await service.InterpretAsync(request);
        var second = await service.InterpretAsync(request);

        Assert.False(first.CacheHit);
        Assert.True(second.CacheHit);
        Assert.Equal(1, runner.CallCount);
        Assert.Equal(1, store.CompletedWrites);
    }

    [Fact]
    public async Task Provider_failure_is_persisted_as_invalid_without_throwing_into_deterministic_pipeline()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, now.AddMinutes(-1));
        var store = new FakeStore(evidence);
        var runner = new FakeRunner(Configuration(), new AiProviderException(
            AiInterpretationErrorCodes.Timeout,
            "The AI provider request timed out."));
        var service = CreateService(store, runner, Configuration());

        var result = await service.InterpretAsync(new CreateAiInterpretationRequest(
            "XAUUSD", AiSpecialist.News, AiInterpretationType.NewsEvent, null, now, 24));

        Assert.Equal(AiInterpretationExecutionStatus.Failed, result.Status);
        Assert.Equal(AiInterpretationLifecycle.Invalid, result.Lifecycle);
        Assert.Equal(AiInterpretationErrorCodes.Timeout, result.ErrorCode);
        Assert.Equal(1, store.FailedWrites);
    }

    [Fact]
    public async Task Concurrent_identical_requests_are_coalesced_to_one_provider_call()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, now.AddMinutes(-1));
        var store = new FakeStore(evidence);
        var runner = new FakeRunner(Configuration(), Completion(evidence.Id), TimeSpan.FromMilliseconds(50));
        var service = CreateService(store, runner, Configuration());
        var request = new CreateAiInterpretationRequest(
            "XAUUSD", AiSpecialist.News, AiInterpretationType.NewsEvent, null, now, 24);

        var results = await Task.WhenAll(
            service.InterpretAsync(request),
            service.InterpretAsync(request));

        Assert.Equal(1, runner.CallCount);
        Assert.Equal(1, store.CompletedWrites);
        Assert.Single(results, result => result.CacheHit);
    }

    [Fact]
    public async Task Current_time_requests_reuse_the_same_context_window()
    {
        var evidenceTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, evidenceTime);
        var store = new FakeStore(evidence);
        var runner = new FakeRunner(Configuration(), Completion(evidence.Id));
        var service = CreateService(store, runner, Configuration());
        var request = new CreateAiInterpretationRequest(
            "XAUUSD", AiSpecialist.News, AiInterpretationType.NewsEvent, null, null, 24);

        await service.InterpretAsync(request);
        var second = await service.InterpretAsync(request);

        Assert.True(second.CacheHit);
        Assert.Equal(1, runner.CallCount);
    }

    [Fact]
    public async Task Specialist_rejects_an_interpretation_type_outside_its_boundary()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, now.AddMinutes(-1));
        var service = CreateService(
            new FakeStore(evidence),
            new FakeRunner(Configuration(), Completion(evidence.Id)),
            Configuration());

        var exception = await Assert.ThrowsAsync<AiInterpretationException>(() => service.InterpretAsync(
            new CreateAiInterpretationRequest(
                "XAUUSD", AiSpecialist.News, AiInterpretationType.TechnicalEvidence, null, now, 24)));

        Assert.Equal(AiInterpretationErrorCodes.InvalidRequest, exception.Code);
    }

    private static AiInterpretationService CreateService(
        FakeStore store,
        FakeRunner runner,
        AiSpecialistConfiguration configuration) => new(
        store,
        new AiEvidenceSelector(),
        new AiEvidenceCompressor(),
        new AiResponseValidator(),
        runner,
        new AiInterpretationExecutionGate(),
        new AiSpecialistCatalog([configuration]),
        new AiInterpretationSettings(),
        TimeProvider.System,
        NullLogger<AiInterpretationService>.Instance);

    private static AiSpecialistConfiguration Configuration() => new(
        AiSpecialist.News,
        true,
        "ProviderA",
        "OpenAiCompatible",
        true,
        "test-key",
        "model-a",
        "https://example.test/v1/",
        0.2,
        30,
        2000,
        0,
        0);

    private static AiProviderCompletion Completion(Guid evidenceId)
    {
        var output = new StructuredAiInterpretation
        {
            InterpretationType = AiInterpretationType.NewsEvent,
            Direction = AiInterpretationDirection.Mixed,
            Impact = AiInterpretationImpact.High,
            AffectedAssets = ["XAUUSD", "USD"],
            Mechanism = "The evidence may affect USD demand.",
            ExpectedEffect = "A stronger USD may pressure gold.",
            ObservedReaction = "The current reaction is mixed.",
            ReactionAlignment = AiReactionAlignment.Mixed,
            CurrentRelevance = AiCurrentRelevance.High,
            Confidence = 0.65m,
            Uncertainty = "Persistence remains unknown.",
            Summary = "The evidence is mixed.",
            EvidenceIds = [evidenceId],
            Facts = [new AiInterpretationStatement("The event was published.", [evidenceId])],
            Interpretations = [new AiInterpretationStatement("The effect may be temporary.", [evidenceId])],
            Unknowns = ["Persistence is unknown."],
            Conflicts = [],
            Measurements = []
        };
        return new AiProviderCompletion(JsonSerializer.Serialize(output, SerializerOptions), 100, 50, 25, "request-1");
    }

    private sealed class FakeRunner : IAiSpecialistRunner
    {
        private readonly AiSpecialistConfiguration _configuration;
        private readonly AiProviderCompletion? _completion;
        private readonly AiProviderException? _exception;
        private readonly TimeSpan _delay;

        public FakeRunner(
            AiSpecialistConfiguration configuration,
            AiProviderCompletion completion,
            TimeSpan delay = default)
        {
            _configuration = configuration;
            _completion = completion;
            _delay = delay;
        }

        public FakeRunner(AiSpecialistConfiguration configuration, AiProviderException exception)
        {
            _configuration = configuration;
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public async Task<(AiProviderCompletion Completion, AiSpecialistConfiguration Configuration)> RunAsync(
            AiProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (_exception is not null) throw _exception;
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            return (_completion!, _configuration);
        }
    }

    private sealed class FakeStore(AiEvidenceCandidate evidence) : IAiInterpretationStore
    {
        private AiInterpretationResult? _cached;
        private string? _cachedKey;

        public int CompletedWrites { get; private set; }
        public int FailedWrites { get; private set; }

        public Task<IReadOnlyList<AiEvidenceCandidate>> LoadEvidenceAsync(AiEvidenceSelectionQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiEvidenceCandidate>>([evidence]);

        public Task<AiInterpretationResult?> FindCurrentByCacheKeyAsync(string cacheKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Equals(_cachedKey, cacheKey, StringComparison.Ordinal) ? _cached : null);

        public Task<AiInterpretationResult> SaveCompletedAsync(AiInterpretationWriteModel value, CancellationToken cancellationToken = default)
        {
            CompletedWrites++;
            _cachedKey = value.CacheKey;
            _cached = Result(
                value.Id,
                value.Instrument,
                value.Specialist,
                value.Interpretation.InterpretationType,
                AiInterpretationExecutionStatus.Completed,
                AiInterpretationLifecycle.Current,
                value.Provider,
                value.Model,
                value.PromptVersion,
                value.EvidenceVersion,
                value.AnalysisTimeUtc,
                value.EvidenceUpdatedAtUtc,
                value.CreatedAtUtc,
                value.CompletedAtUtc,
                value.EvidenceIds,
                value.Interpretation,
                null,
                null);
            return Task.FromResult(_cached);
        }

        public Task<AiInterpretationResult> SaveFailureAsync(AiInterpretationFailureWriteModel value, CancellationToken cancellationToken = default)
        {
            FailedWrites++;
            return Task.FromResult(Result(
                value.Id,
                value.Instrument,
                value.Specialist,
                value.InterpretationType,
                AiInterpretationExecutionStatus.Failed,
                AiInterpretationLifecycle.Invalid,
                value.Provider,
                value.Model,
                value.PromptVersion,
                value.EvidenceVersion,
                value.AnalysisTimeUtc,
                value.EvidenceUpdatedAtUtc,
                value.CreatedAtUtc,
                value.CompletedAtUtc,
                value.EvidenceIds,
                null,
                value.ErrorCode,
                value.ErrorMessage));
        }

        public Task MarkChangedInterpretationsStaleAsync(string? instrument, DateTimeOffset checkedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AiInterpretationResult?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AiInterpretationResult?>(null);
        public Task<AiInterpretationResult?> GetLatestAsync(string instrument, AiSpecialist? specialist, AiInterpretationType? interpretationType, string? timeframe, CancellationToken cancellationToken = default) => Task.FromResult<AiInterpretationResult?>(null);
        public Task<PagedAiInterpretations> QueryAsync(AiInterpretationQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedAiInterpretations> GetByEvidenceAsync(Guid evidenceId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private static AiInterpretationResult Result(
            Guid id,
            string instrument,
            AiSpecialist specialist,
            AiInterpretationType type,
            AiInterpretationExecutionStatus status,
            AiInterpretationLifecycle lifecycle,
            string provider,
            string model,
            string promptVersion,
            string evidenceVersion,
            DateTimeOffset analysisTime,
            DateTimeOffset evidenceUpdatedAt,
            DateTimeOffset createdAt,
            DateTimeOffset completedAt,
            IReadOnlyList<Guid> evidenceIds,
            StructuredAiInterpretation? output,
            string? errorCode,
            string? errorMessage) => new(
                id, instrument, null, specialist, type,
                output?.Direction ?? AiInterpretationDirection.Unknown,
                output?.Impact ?? AiInterpretationImpact.Unknown,
                output?.AffectedAssets ?? [],
                output?.Mechanism ?? string.Empty,
                output?.ExpectedEffect ?? string.Empty,
                output?.ObservedReaction ?? string.Empty,
                output?.ReactionAlignment ?? AiReactionAlignment.Unknown,
                output?.CurrentRelevance ?? AiCurrentRelevance.Unknown,
                output?.Confidence,
                output?.Uncertainty ?? string.Empty,
                output?.Summary ?? string.Empty,
                provider, model, promptVersion, evidenceVersion,
                analysisTime, evidenceUpdatedAt, createdAt, completedAt,
                status, lifecycle, false, null, null, null,
                errorCode, errorMessage, evidenceIds, output);
    }
}
