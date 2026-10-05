namespace XauAi.Application.AI;

public interface IAiInterpretationService
{
    Task<AiInterpretationResult> InterpretAsync(
        CreateAiInterpretationRequest request,
        CancellationToken cancellationToken = default);

    Task<AiInterpretationResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AiInterpretationResult?> GetLatestAsync(
        string instrument,
        AiSpecialist? specialist,
        AiInterpretationType? interpretationType,
        string? timeframe,
        CancellationToken cancellationToken = default);

    Task<PagedAiInterpretations> QueryAsync(
        AiInterpretationQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedAiInterpretations> GetByEvidenceAsync(
        Guid evidenceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IAiInterpretationStore
{
    Task<IReadOnlyList<AiEvidenceCandidate>> LoadEvidenceAsync(
        AiEvidenceSelectionQuery query,
        CancellationToken cancellationToken = default);

    Task<AiInterpretationResult?> FindCurrentByCacheKeyAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);

    Task<AiInterpretationResult> SaveCompletedAsync(
        AiInterpretationWriteModel interpretation,
        CancellationToken cancellationToken = default);

    Task<AiInterpretationResult> SaveFailureAsync(
        AiInterpretationFailureWriteModel interpretation,
        CancellationToken cancellationToken = default);

    Task MarkChangedInterpretationsStaleAsync(
        string? instrument,
        DateTimeOffset checkedAtUtc,
        CancellationToken cancellationToken = default);

    Task<AiInterpretationResult?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AiInterpretationResult?> GetLatestAsync(
        string instrument,
        AiSpecialist? specialist,
        AiInterpretationType? interpretationType,
        string? timeframe,
        CancellationToken cancellationToken = default);

    Task<PagedAiInterpretations> QueryAsync(
        AiInterpretationQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedAiInterpretations> GetByEvidenceAsync(
        Guid evidenceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IAiEvidenceSelector
{
    SelectedAiEvidence Select(
        IReadOnlyList<AiEvidenceCandidate> candidates,
        AiEvidenceSelectionQuery query,
        int maximumItems);
}

public interface IAiEvidenceCompressor
{
    CompressedAiEvidence Compress(
        SelectedAiEvidence selection,
        AiEvidenceSelectionQuery query,
        int maximumCharacters);
}

public interface IAiResponseValidator
{
    StructuredAiInterpretation Validate(
        string json,
        AiInterpretationType expectedType,
        IReadOnlyList<AiEvidenceCandidate> selectedEvidence);
}

public interface IAiProvider
{
    string Adapter { get; }

    Task<AiProviderCompletion> InterpretAsync(
        AiProviderRequest request,
        AiSpecialistConfiguration configuration,
        CancellationToken cancellationToken = default);
}

public interface IAiProviderFactory
{
    IAiProvider Create(string adapter);
}

public interface IAiSpecialist
{
    AiSpecialist Name { get; }

    AiSpecialistConfiguration Configuration { get; }

    Task<AiProviderCompletion> AnalyzeAsync(
        AiProviderRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiSpecialistRunner
{
    Task<(AiProviderCompletion Completion, AiSpecialistConfiguration Configuration)> RunAsync(
        AiProviderRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiRequestGate
{
    Task WaitAsync(
        AiSpecialist specialist,
        int requestsPerMinute,
        CancellationToken cancellationToken = default);
}

public interface IAiInterpretationExecutionGate
{
    Task<IAsyncDisposable> AcquireAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);
}
