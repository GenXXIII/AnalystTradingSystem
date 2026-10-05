using Microsoft.EntityFrameworkCore;
using XauAi.Application.Analysts;
using XauAi.Domain.Analysts;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.Analysts.Persistence;

internal sealed class EfAnalystSyncStateStore(
    XauAiDbContext context,
    AnalystReferenceResolver references) : IAnalystSyncStateStore
{
    public async Task<AnalystSyncStateResult?> GetAsync(
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var providerId = await references.ProviderIdAsync(providerKey, cancellationToken);
        var state = await context.AnalystSyncStates.AsNoTracking()
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
        var interrupted = await context.AnalystSyncRuns
            .Where(run => run.DataProviderId == providerId && run.Status == "Running")
            .ToArrayAsync(cancellationToken);
        foreach (var run in interrupted)
        {
            run.Status = "Interrupted";
            run.CompletedAtUtc = startedAtUtc;
            run.ErrorCode = "SYNCHRONIZATION_INTERRUPTED";
            run.ErrorMessage = "The previous analyst synchronization ended before completion and was safely superseded.";
        }

        var state = await context.AnalystSyncStates
            .SingleOrDefaultAsync(value => value.DataProviderId == providerId, cancellationToken);
        if (state is null)
        {
            state = new AnalystSyncState { DataProviderId = providerId };
            context.AnalystSyncStates.Add(state);
        }

        Reset(state, startedAtUtc);
        var runId = Guid.NewGuid();
        context.AnalystSyncRuns.Add(new AnalystSyncRun
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
        DateTimeOffset? lastPublishedAtUtc,
        string? lastExternalId,
        AnalystRunMetrics metrics,
        CancellationToken cancellationToken = default) =>
        FinishAsync(
            runId,
            providerKey,
            completedAtUtc,
            lastPublishedAtUtc,
            lastExternalId,
            metrics,
            "Healthy",
            null,
            null,
            cancellationToken);

    public Task FailAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset failedAtUtc,
        AnalystRunMetrics metrics,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken = default) =>
        FinishAsync(
            runId,
            providerKey,
            failedAtUtc,
            null,
            null,
            metrics,
            "Failed",
            errorCode,
            safeMessage,
            cancellationToken);

    private async Task FinishAsync(
        Guid runId,
        string providerKey,
        DateTimeOffset completedAtUtc,
        DateTimeOffset? lastPublishedAtUtc,
        string? lastExternalId,
        AnalystRunMetrics metrics,
        string status,
        string? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var providerId = await references.ProviderIdAsync(providerKey, cancellationToken);
        var run = await context.AnalystSyncRuns
            .SingleAsync(value => value.Id == runId && value.DataProviderId == providerId, cancellationToken);
        var state = await context.AnalystSyncStates
            .SingleAsync(value => value.DataProviderId == providerId, cancellationToken);
        Apply(run, metrics);
        run.Status = status == "Healthy" ? "Completed" : status;
        run.CompletedAtUtc = completedAtUtc;
        run.ErrorCode = errorCode;
        run.ErrorMessage = errorMessage;
        Apply(state, metrics);
        state.Status = status;
        state.LastSuccessfulSyncAtUtc = status == "Healthy"
            ? completedAtUtc
            : state.LastSuccessfulSyncAtUtc;
        state.LastPublishedAtUtc = status == "Healthy" && lastPublishedAtUtc.HasValue
            ? Max(state.LastPublishedAtUtc, lastPublishedAtUtc)
            : state.LastPublishedAtUtc;
        state.LastExternalId = status == "Healthy" && !string.IsNullOrWhiteSpace(lastExternalId)
            ? lastExternalId
            : state.LastExternalId;
        state.ConsecutiveFailures = status == "Healthy" ? 0 : state.ConsecutiveFailures + 1;
        state.LastErrorCode = errorCode;
        state.LastErrorMessage = errorMessage;
        state.UpdatedAtUtc = completedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static DateTimeOffset? Max(DateTimeOffset? left, DateTimeOffset? right) =>
        !left.HasValue ? right
        : !right.HasValue ? left
        : left >= right ? left : right;

    private static void Reset(AnalystSyncState state, DateTimeOffset startedAtUtc)
    {
        state.Status = "Running";
        state.LastAttemptAtUtc = startedAtUtc;
        state.RequestsMade = 0;
        state.RateLimitResponses = 0;
        state.ItemsReceived = 0;
        state.PublicationsInserted = 0;
        state.PredictionsInserted = 0;
        state.ItemsSkipped = 0;
        state.ItemsRejected = 0;
        state.DurationMilliseconds = 0;
        state.LastErrorCode = null;
        state.LastErrorMessage = null;
        state.UpdatedAtUtc = startedAtUtc;
    }

    private static void Apply(AnalystSyncRun run, AnalystRunMetrics metrics)
    {
        run.RequestsMade = metrics.RequestsMade;
        run.RateLimitResponses = metrics.RateLimitResponses;
        run.ItemsReceived = metrics.ItemsReceived;
        run.PublicationsInserted = metrics.PublicationsInserted;
        run.PredictionsInserted = metrics.PredictionsInserted;
        run.ItemsSkipped = metrics.ItemsSkipped;
        run.ItemsRejected = metrics.ItemsRejected;
        run.DurationMilliseconds = metrics.DurationMilliseconds;
    }

    private static void Apply(AnalystSyncState state, AnalystRunMetrics metrics)
    {
        state.RequestsMade = metrics.RequestsMade;
        state.RateLimitResponses = metrics.RateLimitResponses;
        state.ItemsReceived = metrics.ItemsReceived;
        state.PublicationsInserted = metrics.PublicationsInserted;
        state.PredictionsInserted = metrics.PredictionsInserted;
        state.ItemsSkipped = metrics.ItemsSkipped;
        state.ItemsRejected = metrics.ItemsRejected;
        state.DurationMilliseconds = metrics.DurationMilliseconds;
    }

    private static AnalystSyncStateResult Map(AnalystSyncState state, string providerKey) => new(
        providerKey,
        state.Status,
        state.LastAttemptAtUtc,
        state.LastSuccessfulSyncAtUtc,
        state.LastPublishedAtUtc,
        state.LastExternalId,
        state.ConsecutiveFailures,
        new AnalystRunMetrics(
            state.RequestsMade,
            state.RateLimitResponses,
            state.ItemsReceived,
            state.PublicationsInserted,
            state.PredictionsInserted,
            state.ItemsSkipped,
            state.ItemsRejected,
            state.DurationMilliseconds),
        state.LastErrorCode,
        state.LastErrorMessage);
}
