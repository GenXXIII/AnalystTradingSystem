using Microsoft.EntityFrameworkCore;
using XauAi.Application.MarketData;
using XauAi.Domain.Market;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed class EfMarketDataSyncStateStore(
    XauAiDbContext dbContext,
    MarketDataReferenceResolver referenceResolver) : IMarketDataSyncStateStore
{
    public async Task<Guid> StartRunAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var references = await referenceResolver.RequireAsync(symbol, timeframe, cancellationToken);
        var abandonedRuns = await dbContext.MarketDataSyncRuns
            .Where(run =>
                run.InstrumentId == references.InstrumentId
                && run.TimeframeId == references.TimeframeId
                && run.DataProviderId == references.ProviderId
                && run.Status == "Running")
            .ToListAsync(cancellationToken);
        foreach (var abandoned in abandonedRuns)
        {
            abandoned.Status = "Interrupted";
            abandoned.CompletedAtUtc = startedAtUtc;
            abandoned.DurationMilliseconds = Math.Max(0, (long)(startedAtUtc - abandoned.StartedAtUtc).TotalMilliseconds);
            abandoned.ErrorCode = "MARKET_DATA_SYNC_INTERRUPTED";
            abandoned.ErrorMessage = "A previous synchronization was interrupted before completion.";
        }

        var state = await FindStateAsync(references, cancellationToken);
        if (state is null)
        {
            state = new MarketDataSyncState
            {
                Id = Guid.NewGuid(),
                InstrumentId = references.InstrumentId,
                TimeframeId = references.TimeframeId,
                DataProviderId = references.ProviderId
            };
            dbContext.MarketDataSyncStates.Add(state);
        }

        state.Status = "Running";
        state.LastAttemptAtUtc = startedAtUtc;
        state.LastRequestedFromUtc = fromUtc;
        state.LastRequestedToUtc = toUtc;
        state.LastErrorCode = null;
        state.LastErrorMessage = null;
        state.UpdatedAtUtc = startedAtUtc;

        var runId = Guid.NewGuid();
        dbContext.MarketDataSyncRuns.Add(new MarketDataSyncRun
        {
            Id = runId,
            InstrumentId = references.InstrumentId,
            TimeframeId = references.TimeframeId,
            DataProviderId = references.ProviderId,
            StartedAtUtc = startedAtUtc,
            RequestedFromUtc = fromUtc,
            RequestedToUtc = toUtc,
            Status = "Running"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return runId;
    }

    public async Task CompleteRunAsync(
        Guid runId,
        MarketDataPipelineResult result,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var run = await dbContext.MarketDataSyncRuns.SingleAsync(value => value.Id == runId, cancellationToken);
        ApplyResult(run, result);
        run.Status = "Succeeded";
        run.CompletedAtUtc = completedAtUtc;

        var state = await dbContext.MarketDataSyncStates.SingleAsync(value =>
            value.InstrumentId == run.InstrumentId
            && value.TimeframeId == run.TimeframeId
            && value.DataProviderId == run.DataProviderId,
            cancellationToken);
        state.Status = "Healthy";
        state.LastSuccessfulSyncAtUtc = completedAtUtc;
        state.LastStoredCandleOpenTimeUtc = result.LastStoredCandleOpenTimeUtc;
        state.ConsecutiveFailures = 0;
        state.DetectedGapCount = result.DetectedGaps;
        state.LastErrorCode = null;
        state.LastErrorMessage = null;
        state.UpdatedAtUtc = completedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task FailRunAsync(
        Guid runId,
        MarketDataSyncProgress progress,
        string errorCode,
        string safeMessage,
        DateTimeOffset failedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var run = await dbContext.MarketDataSyncRuns.SingleAsync(value => value.Id == runId, cancellationToken);
        run.Status = "Failed";
        run.CompletedAtUtc = failedAtUtc;
        run.BatchesProcessed = progress.BatchesProcessed;
        run.RecordsReceived = progress.Received;
        run.RecordsAccepted = progress.Accepted;
        run.RecordsInserted = progress.Inserted;
        run.RecordsUpdated = progress.Updated;
        run.RecordsSkipped = progress.Skipped;
        run.RecordsRejected = progress.Rejected;
        run.DurationMilliseconds = Math.Max(0, (long)(failedAtUtc - run.StartedAtUtc).TotalMilliseconds);
        run.ErrorCode = errorCode;
        run.ErrorMessage = safeMessage;

        var state = await dbContext.MarketDataSyncStates.SingleAsync(value =>
            value.InstrumentId == run.InstrumentId
            && value.TimeframeId == run.TimeframeId
            && value.DataProviderId == run.DataProviderId,
            cancellationToken);
        state.Status = "Failed";
        state.ConsecutiveFailures++;
        state.LastErrorCode = errorCode;
        state.LastErrorMessage = safeMessage;
        state.UpdatedAtUtc = failedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MarketDataSyncStatus?> GetStatusAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        var references = await referenceResolver.RequireAsync(symbol, timeframe, cancellationToken);
        var state = await FindStateAsync(references, cancellationToken);
        return state is null
            ? null
            : new MarketDataSyncStatus(
                symbol,
                timeframe,
                state.Status,
                state.LastAttemptAtUtc,
                state.LastSuccessfulSyncAtUtc,
                state.LastRequestedFromUtc,
                state.LastRequestedToUtc,
                state.LastStoredCandleOpenTimeUtc,
                state.ConsecutiveFailures,
                state.DetectedGapCount,
                state.LastErrorCode,
                state.LastErrorMessage);
    }

    private Task<MarketDataSyncState?> FindStateAsync(
        MarketDataReferences references,
        CancellationToken cancellationToken) =>
        dbContext.MarketDataSyncStates.SingleOrDefaultAsync(state =>
            state.InstrumentId == references.InstrumentId
            && state.TimeframeId == references.TimeframeId
            && state.DataProviderId == references.ProviderId,
            cancellationToken);

    private static void ApplyResult(MarketDataSyncRun run, MarketDataPipelineResult result)
    {
        run.BatchesProcessed = result.BatchesProcessed;
        run.RecordsReceived = result.Received;
        run.RecordsAccepted = result.Accepted;
        run.RecordsInserted = result.Inserted;
        run.RecordsUpdated = result.Updated;
        run.RecordsSkipped = result.Skipped;
        run.RecordsRejected = result.Rejected;
        run.DetectedGapCount = result.DetectedGaps;
        run.DurationMilliseconds = result.DurationMilliseconds;
    }
}
