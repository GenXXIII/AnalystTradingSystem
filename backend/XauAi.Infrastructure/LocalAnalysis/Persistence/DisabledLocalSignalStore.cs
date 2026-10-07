using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.LocalAnalysis.Persistence;

internal sealed class DisabledLocalSignalStore : ILocalSignalStore
{
    public Task<LocalAnalystCheckpoint?> GetCheckpointAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<LocalSignalSnapshot?> GetActiveAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<LocalSignalSnapshot> SaveAsync(LocalSignalPersistenceRequest request, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<IReadOnlyList<LocalSignalSnapshot>> GetHistoryAsync(string symbol, MarketTimeframe? timeframe, int limit, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<IReadOnlyList<LocalSignalLifecycleItem>> GetLifecycleAsync(string signalId, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<IReadOnlyList<LocalSignalChartMarker>> GetChartMarkersAsync(string symbol, MarketTimeframe timeframe, int limit, CancellationToken cancellationToken = default) =>
        throw Disabled();

    public Task<IReadOnlyList<LocalAnalystCheckpoint>> GetCheckpointsAsync(string symbol, IReadOnlyList<MarketTimeframe> timeframes, CancellationToken cancellationToken = default) =>
        throw Disabled();

    private static LocalAnalystException Disabled() =>
        new(
            LocalAnalystErrorCodes.DatabaseDisabled,
            "The database is disabled; local signal persistence is unavailable.");
}
