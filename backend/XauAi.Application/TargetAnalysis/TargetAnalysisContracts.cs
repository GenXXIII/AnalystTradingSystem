namespace XauAi.Application.TargetAnalysis;

public interface ITargetAnalystService
{
    Task<TargetAnalysisResult> AnalyzeAsync(
        CreateTargetAnalysisRequest request,
        CancellationToken cancellationToken = default);

    Task<TargetAnalysisResult> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TargetAnalysisResult>> GetActiveAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<PagedTargetAnalyses> GetHistoryAsync(
        TargetAnalysisQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TargetLifecycleItem>> GetLifecycleAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TargetAnalysisResult> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    Task MonitorActiveAsync(CancellationToken cancellationToken = default);
}

public interface ITargetAnalysisStore
{
    Task<TargetAnalysisResult> CreateJobAsync(
        TargetAnalysisJobWriteModel job,
        CancellationToken cancellationToken = default);

    Task UpdateSnapshotAsync(
        TargetSnapshotWriteModel snapshot,
        CancellationToken cancellationToken = default);

    Task<TargetAnalysisResult> CompleteAsync(
        TargetAnalysisCompletionWriteModel completion,
        CancellationToken cancellationToken = default);

    Task<TargetAnalysisResult> CompleteNoValidTargetAsync(
        TargetNoValidTargetWriteModel completion,
        CancellationToken cancellationToken = default);

    Task<TargetAnalysisResult?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TargetAnalysisResult>> GetActiveAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<PagedTargetAnalyses> QueryAsync(
        TargetAnalysisQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TargetLifecycleItem>> GetLifecycleAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> TryTransitionAsync(
        TargetLifecycleTransition transition,
        CancellationToken cancellationToken = default);
}

public interface ITargetAiProvider
{
    string Adapter { get; }

    Task<TargetAiCompletion> AnalyzeAsync(
        TargetAiRequest request,
        TargetWorkspaceConfiguration configuration,
        CancellationToken cancellationToken = default);
}

public interface ITargetAiProviderFactory
{
    ITargetAiProvider Create(string adapter);
}

public interface ITargetWorkspaceRunner
{
    Task<TargetWorkspaceRunResult> RunSpecialistAsync(
        TargetAiRequest request,
        CancellationToken cancellationToken = default);

    Task<TargetWorkspaceRunResult> RunMasterAsync(
        TargetAiRequest request,
        CancellationToken cancellationToken = default);
}

public interface ITargetAiResponseValidator
{
    TargetSpecialistOutput ValidateSpecialist(
        string json,
        TargetWorkspace expectedWorkspace,
        IReadOnlyCollection<Guid> allowedEvidenceIds);

    TargetMasterOutput ValidateMaster(
        string json,
        IReadOnlyCollection<Guid> allowedEvidenceIds);
}

public interface ITargetResultValidator
{
    string? Validate(
        TargetMasterOutput master,
        TargetSpecialistOutput risk,
        TargetAnalysisSnapshot snapshot,
        IReadOnlyDictionary<Guid, XauAi.Application.AI.AiEvidenceCandidate> selectedEvidence,
        decimal? requestedTimeframeAtr,
        IReadOnlyList<TargetWorkspaceRunResult> workspaceResults);
}

public interface ITargetAiRequestGate
{
    Task WaitAsync(
        TargetWorkspace workspace,
        int requestsPerMinute,
        CancellationToken cancellationToken = default);
}
