using XauAi.Application.MarketData;

namespace XauAi.Application.LocalAnalysis;

public interface ILocalAnalystService
{
    Task<LocalSignalSnapshot> EvaluateAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);

    Task<LocalSignalSnapshot> GetCurrentAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);

    Task<LocalSignalHistoryResult> GetHistoryAsync(
        string symbol,
        MarketTimeframe? timeframe,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalSignalLifecycleItem>> GetLifecycleAsync(
        string signalId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalSignalChartMarker>> GetChartMarkersAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken = default);

    Task<LocalAnalystStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface ILocalSignalEngine
{
    LocalSignalDecision Evaluate(
        IReadOnlyList<StoredMarketCandle> candles,
        TechnicalAnalysis.TechnicalAnalysisResult analysis,
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset evaluatedAtUtc);
}

public interface ILocalSignalStore
{
    Task<LocalAnalystCheckpoint?> GetCheckpointAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);

    Task<LocalSignalSnapshot?> GetActiveAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default);

    Task<LocalSignalSnapshot> SaveAsync(
        LocalSignalPersistenceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalSignalSnapshot>> GetHistoryAsync(
        string symbol,
        MarketTimeframe? timeframe,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalSignalLifecycleItem>> GetLifecycleAsync(
        string signalId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalSignalChartMarker>> GetChartMarkersAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalAnalystCheckpoint>> GetCheckpointsAsync(
        string symbol,
        IReadOnlyList<MarketTimeframe> timeframes,
        CancellationToken cancellationToken = default);
}
