namespace XauAi.Application.FullAnalysis;

using XauAi.Application.AI;

public interface IFullAnalystService
{
    Task<FullAnalysisResult> AnalyzeAsync(CreateFullAnalysisRequest request, CancellationToken cancellationToken = default);
    Task<FullAnalysisResult> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FullAnalysisResult>> GetActiveAsync(string symbol, CancellationToken cancellationToken = default);
    Task<PagedFullAnalyses> GetHistoryAsync(FullAnalysisQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FullLifecycleItem>> GetLifecycleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FullAnalysisResult> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task MonitorActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiProviderAccountStatus>> GetProviderStatusesAsync(CancellationToken cancellationToken = default);
}

public interface IFullAnalysisStore
{
    Task<FullAnalysisResult> CreateJobAsync(FullAnalysisJobWriteModel job, CancellationToken cancellationToken = default);
    Task UpdateSnapshotAsync(FullSnapshotWriteModel snapshot, CancellationToken cancellationToken = default);
    Task<FullAnalysisResult> CompleteAsync(FullAnalysisCompletionWriteModel completion, CancellationToken cancellationToken = default);
    Task<FullAnalysisResult?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FullAnalysisResult>> GetActiveAsync(string symbol, CancellationToken cancellationToken = default);
    Task<PagedFullAnalyses> QueryAsync(FullAnalysisQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FullLifecycleItem>> GetLifecycleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TryTransitionAsync(FullLifecycleTransition transition, CancellationToken cancellationToken = default);
}

public interface IFullAiProvider
{
    string Adapter { get; }
    Task<FullAiCompletion> AnalyzeAsync(
        FullAiRequest request,
        FullWorkspaceConfiguration configuration,
        CancellationToken cancellationToken = default);
}

public interface IFullAiProviderFactory
{
    IFullAiProvider Create(string adapter);
}

public interface IFullWorkspaceRunner
{
    Task<FullWorkspaceRunResult> RunSpecialistAsync(FullAiRequest request, CancellationToken cancellationToken = default);
    Task<FullWorkspaceRunResult> RunMasterAsync(FullAiRequest request, CancellationToken cancellationToken = default);
}

public interface IFullAiResponseValidator
{
    FullSpecialistOutput ValidateSpecialist(
        string json,
        FullWorkspace expectedWorkspace,
        IReadOnlyCollection<Guid> allowedEvidenceIds);

    FullMasterOutput ValidateMaster(string json, IReadOnlyCollection<Guid> allowedEvidenceIds);
}

public interface IFullResultValidator
{
    string? Validate(
        FullMasterOutput master,
        FullSpecialistOutput risk,
        FullAnalysisSnapshot snapshot,
        IReadOnlyList<FullWorkspaceRunResult> workspaceResults);
}
