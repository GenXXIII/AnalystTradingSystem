using Microsoft.EntityFrameworkCore;
using XauAi.Application.EconomicData;
using XauAi.Domain.EconomicData;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.EconomicData.Persistence;

internal sealed class EfEconomicSyncStateStore(XauAiDbContext context) : IEconomicSyncStateStore
{
    public Task<EconomicSyncStateResult?> GetAsync(
        Guid economicSeriesId,
        CancellationToken cancellationToken = default) =>
        Project(context.EconomicSyncStates.AsNoTracking()
                .Where(state => state.EconomicSeriesId == economicSeriesId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<EconomicSyncStateResult>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await Project(context.EconomicSyncStates.AsNoTracking()
            .OrderBy(state => state.EconomicSeriesId))
            .ToArrayAsync(cancellationToken);

    public async Task<Guid> StartAsync(
        Guid economicSeriesId,
        DateOnly from,
        DateOnly to,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var interrupted = await context.EconomicSyncRuns
            .Where(run => run.EconomicSeriesId == economicSeriesId && run.Status == "Running")
            .ToArrayAsync(cancellationToken);
        foreach (var run in interrupted)
        {
            run.Status = "Interrupted";
            run.CompletedAtUtc = startedAtUtc;
            run.ErrorCode = "ECONOMIC_DATA_SYNC_INTERRUPTED";
            run.ErrorMessage = "The previous synchronization did not complete before restart or retry.";
        }

        var state = await context.EconomicSyncStates.FindAsync([economicSeriesId], cancellationToken);
        if (state is null)
        {
            state = new EconomicSyncState { EconomicSeriesId = economicSeriesId };
            context.EconomicSyncStates.Add(state);
        }

        state.Status = "Running";
        state.LastAttemptAtUtc = startedAtUtc;
        state.LastErrorCode = null;
        state.LastErrorMessage = null;
        state.UpdatedAtUtc = startedAtUtc;
        var runId = Guid.NewGuid();
        context.EconomicSyncRuns.Add(new EconomicSyncRun
        {
            Id = runId,
            EconomicSeriesId = economicSeriesId,
            Status = "Running",
            RequestedFromDate = from,
            RequestedToDate = to,
            StartedAtUtc = startedAtUtc
        });
        await context.SaveChangesAsync(cancellationToken);
        return runId;
    }

    public Task CompleteAsync(
        Guid runId,
        Guid economicSeriesId,
        DateTimeOffset completedAtUtc,
        DateOnly? lastObservationDate,
        EconomicSyncMetrics metrics,
        CancellationToken cancellationToken = default) =>
        FinishAsync(
            runId,
            economicSeriesId,
            completedAtUtc,
            lastObservationDate,
            metrics,
            "Healthy",
            null,
            null,
            cancellationToken);

    public Task FailAsync(
        Guid runId,
        Guid economicSeriesId,
        DateTimeOffset failedAtUtc,
        EconomicSyncMetrics metrics,
        string errorCode,
        string safeMessage,
        CancellationToken cancellationToken = default) =>
        FinishAsync(
            runId,
            economicSeriesId,
            failedAtUtc,
            null,
            metrics,
            "Failed",
            errorCode,
            safeMessage,
            cancellationToken);

    private async Task FinishAsync(
        Guid runId,
        Guid economicSeriesId,
        DateTimeOffset completedAtUtc,
        DateOnly? lastObservationDate,
        EconomicSyncMetrics metrics,
        string status,
        string? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var run = await context.EconomicSyncRuns.FindAsync([runId], cancellationToken)
            ?? throw new InvalidOperationException("Economic synchronization run was not found.");
        var state = await context.EconomicSyncStates.FindAsync([economicSeriesId], cancellationToken)
            ?? throw new InvalidOperationException("Economic synchronization state was not found.");
        run.Status = status == "Healthy" ? "Succeeded" : status;
        run.CompletedAtUtc = completedAtUtc;
        run.RequestsMade = metrics.RequestsMade;
        run.RateLimitResponses = metrics.RateLimitResponses;
        run.RecordsReceived = metrics.RecordsReceived;
        run.RecordsInserted = metrics.RecordsInserted;
        run.RecordsUpdated = metrics.RecordsUpdated;
        run.RecordsSkipped = metrics.RecordsSkipped;
        run.DurationMilliseconds = metrics.DurationMilliseconds;
        run.ErrorCode = errorCode;
        run.ErrorMessage = errorMessage;
        state.Status = status;
        state.RequestsMade = metrics.RequestsMade;
        state.RateLimitResponses = metrics.RateLimitResponses;
        state.RecordsReceived = metrics.RecordsReceived;
        state.RecordsInserted = metrics.RecordsInserted;
        state.RecordsUpdated = metrics.RecordsUpdated;
        state.RecordsSkipped = metrics.RecordsSkipped;
        state.DurationMilliseconds = metrics.DurationMilliseconds;
        state.LastErrorCode = errorCode;
        state.LastErrorMessage = errorMessage;
        state.UpdatedAtUtc = completedAtUtc;
        if (status == "Healthy")
        {
            state.LastSuccessfulSyncAtUtc = completedAtUtc;
            state.LastObservationDate = lastObservationDate ?? state.LastObservationDate;
            state.ConsecutiveFailures = 0;
        }
        else
        {
            state.ConsecutiveFailures++;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<EconomicSyncStateResult> Project(IQueryable<EconomicSyncState> source) =>
        source.Join(
            context.EconomicSeries.AsNoTracking(),
            state => state.EconomicSeriesId,
            series => series.Id,
            (state, series) => new EconomicSyncStateResult(
                state.EconomicSeriesId,
                series.ExternalSeriesId,
                series.Name,
                state.Status,
                state.LastAttemptAtUtc,
                state.LastSuccessfulSyncAtUtc,
                state.LastObservationDate,
                state.ConsecutiveFailures,
                new EconomicSyncMetrics(
                    state.RequestsMade,
                    state.RateLimitResponses,
                    state.RecordsReceived,
                    state.RecordsInserted,
                    state.RecordsUpdated,
                    state.RecordsSkipped,
                    state.DurationMilliseconds),
                state.LastErrorCode,
                state.LastErrorMessage));
}
