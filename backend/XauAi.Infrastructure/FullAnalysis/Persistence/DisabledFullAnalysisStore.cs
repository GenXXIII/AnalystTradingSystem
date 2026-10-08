using XauAi.Application.FullAnalysis;

namespace XauAi.Infrastructure.FullAnalysis.Persistence;

internal sealed class DisabledFullAnalysisStore : IFullAnalysisStore
{
    public Task<FullAnalysisResult> CreateJobAsync(FullAnalysisJobWriteModel job, CancellationToken cancellationToken = default) => throw Disabled();
    public Task UpdateSnapshotAsync(FullSnapshotWriteModel snapshot, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<FullAnalysisResult> CompleteAsync(FullAnalysisCompletionWriteModel completion, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<FullAnalysisResult?> GetAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<IReadOnlyList<FullAnalysisResult>> GetActiveAsync(string symbol, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<PagedFullAnalyses> QueryAsync(FullAnalysisQuery query, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<IReadOnlyList<FullLifecycleItem>> GetLifecycleAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<bool> TryTransitionAsync(FullLifecycleTransition transition, CancellationToken cancellationToken = default) => throw Disabled();

    private static FullAnalysisException Disabled() => new(
        FullAnalysisErrorCodes.DatabaseDisabled,
        "Full Analyst SQL persistence is disabled.");
}
