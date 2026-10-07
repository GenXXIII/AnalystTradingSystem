using XauAi.Application.TargetAnalysis;

namespace XauAi.Infrastructure.TargetAnalysis.Persistence;

internal sealed class DisabledTargetAnalysisStore : ITargetAnalysisStore
{
    public Task<TargetAnalysisResult> CreateJobAsync(TargetAnalysisJobWriteModel job, CancellationToken cancellationToken = default) => throw Disabled();
    public Task UpdateSnapshotAsync(TargetSnapshotWriteModel snapshot, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<TargetAnalysisResult> CompleteAsync(TargetAnalysisCompletionWriteModel completion, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<TargetAnalysisResult> CompleteNoValidTargetAsync(TargetNoValidTargetWriteModel completion, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<TargetAnalysisResult?> GetAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<IReadOnlyList<TargetAnalysisResult>> GetActiveAsync(string symbol, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<PagedTargetAnalyses> QueryAsync(TargetAnalysisQuery query, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<IReadOnlyList<TargetLifecycleItem>> GetLifecycleAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<bool> TryTransitionAsync(TargetLifecycleTransition transition, CancellationToken cancellationToken = default) => throw Disabled();

    private static TargetAnalysisException Disabled() => new(
        TargetAnalysisErrorCodes.DatabaseDisabled,
        "Target-analysis SQL persistence is disabled.");
}
