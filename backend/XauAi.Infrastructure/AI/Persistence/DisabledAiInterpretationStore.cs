using XauAi.Application.AI;

namespace XauAi.Infrastructure.AI.Persistence;

internal sealed class DisabledAiInterpretationStore : IAiInterpretationStore
{
    public Task<IReadOnlyList<AiEvidenceCandidate>> LoadEvidenceAsync(AiEvidenceSelectionQuery query, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<AiInterpretationResult?> FindCurrentByCacheKeyAsync(string cacheKey, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<AiInterpretationResult> SaveCompletedAsync(AiInterpretationWriteModel interpretation, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<AiInterpretationResult> SaveFailureAsync(AiInterpretationFailureWriteModel interpretation, CancellationToken cancellationToken = default) => throw Disabled();
    public Task MarkChangedInterpretationsStaleAsync(string? instrument, DateTimeOffset checkedAtUtc, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<AiInterpretationResult?> GetAsync(Guid id, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<AiInterpretationResult?> GetLatestAsync(string instrument, AiSpecialist? specialist, AiInterpretationType? interpretationType, string? timeframe, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<PagedAiInterpretations> QueryAsync(AiInterpretationQuery query, CancellationToken cancellationToken = default) => throw Disabled();
    public Task<PagedAiInterpretations> GetByEvidenceAsync(Guid evidenceId, int page, int pageSize, CancellationToken cancellationToken = default) => throw Disabled();

    private static AiInterpretationException Disabled() => new(
        AiInterpretationErrorCodes.DatabaseDisabled,
        "AI interpretation SQL persistence is disabled.");
}
