using Microsoft.EntityFrameworkCore;
using XauAi.Application.News;
using XauAi.Domain.News;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.News.Persistence;

internal sealed class EfNewsCollectionStateStore(
    XauAiDbContext context,
    NewsReferenceResolver references) : INewsCollectionStateStore
{
    public async Task<NewsCollectionStateResult?> GetAsync(
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var providerId = await references.ProviderIdAsync(providerKey, cancellationToken);
        var state = await context.NewsCollectionStates.AsNoTracking()
            .SingleOrDefaultAsync(value => value.DataProviderId == providerId, cancellationToken);
        return state is null ? null : Map(state, providerKey);
    }

    public async Task<Guid> StartAsync(
        string providerKey,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var providerId = await references.ProviderIdAsync(providerKey, cancellationToken);
        var interrupted = await context.NewsCollectionRuns
            .Where(run => run.DataProviderId == providerId && run.Status == "Running")
            .ToArrayAsync(cancellationToken);
        foreach (var run in interrupted)
        {
            run.Status = "Interrupted";
            run.CompletedAtUtc = startedAtUtc;
            run.ErrorCode = "COLLECTION_INTERRUPTED";
            run.ErrorMessage = "The previous collection ended before completion and was safely superseded.";
        }

        var state = await context.NewsCollectionStates
            .SingleOrDefaultAsync(value => value.DataProviderId == providerId, cancellationToken);
        if (state is null)
        {
            state = new NewsCollectionState { DataProviderId = providerId };
            context.NewsCollectionStates.Add(state);
        }

        ResetState(state, fromUtc, toUtc, startedAtUtc);
        var runId = Guid.NewGuid();
        context.NewsCollectionRuns.Add(new NewsCollectionRun
        {
            Id = runId,
            DataProviderId = providerId,
            Status = "Running",
            RequestedFromUtc = fromUtc,
            RequestedToUtc = toUtc,
            StartedAtUtc = startedAtUtc
        });
        await context.SaveChangesAsync(cancellationToken);
        return runId;
    }

    public Task CompleteAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset completedAtUtc,
        NewsRunMetrics metrics,
        CancellationToken cancellationToken = default) =>
        FinishAsync(runId, providerKey, completedAtUtc, metrics, "Healthy", null, null, cancellationToken);

    public Task FailAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset failedAtUtc,
        NewsRunMetrics metrics,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken = default) =>
        FinishAsync(runId, providerKey, failedAtUtc, metrics, "Failed", errorCode, safeMessage, cancellationToken);

    private async Task FinishAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset completedAtUtc,
        NewsRunMetrics metrics,
        string status,
        string? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var providerId = await references.ProviderIdAsync(providerKey, cancellationToken);
        var run = await context.NewsCollectionRuns
            .SingleAsync(value => value.Id == runId && value.DataProviderId == providerId, cancellationToken);
        var state = await context.NewsCollectionStates
            .SingleAsync(value => value.DataProviderId == providerId, cancellationToken);
        ApplyMetrics(run, metrics);
        run.Status = status == "Healthy" ? "Completed" : status;
        run.CompletedAtUtc = completedAtUtc;
        run.ErrorCode = errorCode;
        run.ErrorMessage = errorMessage;
        ApplyMetrics(state, metrics);
        state.Status = status;
        state.LastSuccessfulCollectionAtUtc = status == "Healthy"
            ? completedAtUtc
            : state.LastSuccessfulCollectionAtUtc;
        state.ConsecutiveFailures = status == "Healthy" ? 0 : state.ConsecutiveFailures + 1;
        state.LastErrorCode = errorCode;
        state.LastErrorMessage = errorMessage;
        state.UpdatedAtUtc = completedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ResetState(
        NewsCollectionState state,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset startedAtUtc)
    {
        state.Status = "Running";
        state.LastAttemptAtUtc = startedAtUtc;
        state.LastRequestedFromUtc = fromUtc;
        state.LastRequestedToUtc = toUtc;
        state.RequestsMade = 0;
        state.RateLimitResponses = 0;
        state.ArticlesReceived = 0;
        state.ArticlesInserted = 0;
        state.ArticlesSkipped = 0;
        state.ArticlesRejected = 0;
        state.DurationMilliseconds = 0;
        state.LastErrorCode = null;
        state.LastErrorMessage = null;
        state.UpdatedAtUtc = startedAtUtc;
    }

    private static void ApplyMetrics(NewsCollectionRun run, NewsRunMetrics metrics)
    {
        run.RequestsMade = metrics.RequestsMade;
        run.RateLimitResponses = metrics.RateLimitResponses;
        run.ArticlesReceived = metrics.ArticlesReceived;
        run.ArticlesInserted = metrics.ArticlesInserted;
        run.ArticlesSkipped = metrics.ArticlesSkipped;
        run.ArticlesRejected = metrics.ArticlesRejected;
        run.DurationMilliseconds = metrics.DurationMilliseconds;
    }

    private static void ApplyMetrics(NewsCollectionState state, NewsRunMetrics metrics)
    {
        state.RequestsMade = metrics.RequestsMade;
        state.RateLimitResponses = metrics.RateLimitResponses;
        state.ArticlesReceived = metrics.ArticlesReceived;
        state.ArticlesInserted = metrics.ArticlesInserted;
        state.ArticlesSkipped = metrics.ArticlesSkipped;
        state.ArticlesRejected = metrics.ArticlesRejected;
        state.DurationMilliseconds = metrics.DurationMilliseconds;
    }

    private static NewsCollectionStateResult Map(NewsCollectionState state, string provider) => new(
        provider,
        state.Status,
        state.LastAttemptAtUtc,
        state.LastSuccessfulCollectionAtUtc,
        state.LastRequestedFromUtc,
        state.LastRequestedToUtc,
        state.ConsecutiveFailures,
        state.RequestsMade,
        state.RateLimitResponses,
        state.ArticlesReceived,
        state.ArticlesInserted,
        state.ArticlesSkipped,
        state.ArticlesRejected,
        state.DurationMilliseconds,
        state.LastErrorCode,
        state.LastErrorMessage);
}
