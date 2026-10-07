using System.Text.Json.Serialization;
using XauAi.Application.MarketData;

namespace XauAi.Application.LocalAnalysis;

public enum LocalSignalState
{
    Nothing,
    Buy,
    Sell,
    Stop
}

public enum LocalSignalMutation
{
    None,
    Open,
    Update,
    Stop
}

public sealed record LocalSignalCondition(
    string Component,
    string State,
    bool LongMatched,
    bool ShortMatched,
    decimal Weight,
    IReadOnlyList<string> Evidence);

public sealed record LocalSignalDecision(
    string Symbol,
    MarketTimeframe Timeframe,
    string SignalCandleId,
    DateTimeOffset SignalCandleTimeUtc,
    DateTimeOffset CandleCloseTimeUtc,
    LocalSignalState State,
    decimal SignalPrice,
    decimal Score,
    decimal MaxScore,
    decimal Confidence,
    string StructureState,
    string LiquidityState,
    string CandleState,
    string MomentumState,
    string KtrState,
    string VolatilityState,
    decimal? InvalidationPrice,
    decimal? TargetPrice,
    string? Reason,
    DateTimeOffset ValidUntilUtc,
    IReadOnlyList<LocalSignalCondition> Conditions);

public sealed record LocalSignalSnapshot(
    string? SignalId,
    string Symbol,
    MarketTimeframe Timeframe,
    string? SignalCandleId,
    DateTimeOffset? SignalCandleTimeUtc,
    LocalSignalState State,
    LocalSignalState? OriginDirection,
    decimal? SignalPrice,
    decimal Score,
    decimal MaxScore,
    decimal Confidence,
    string StructureState,
    string LiquidityState,
    string CandleState,
    string MomentumState,
    string KtrState,
    string VolatilityState,
    decimal? InvalidationPrice,
    decimal? TargetPrice,
    string? Reason,
    DateTimeOffset? ValidUntilUtc,
    string Status,
    string ConfigurationVersion,
    IReadOnlyList<LocalSignalCondition> Conditions,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? EndedAtUtc,
    bool IsCached,
    [property: JsonIgnore] Guid? PersistenceId = null);

public sealed record LocalSignalLifecycleItem(
    string SignalId,
    string EventType,
    string? PreviousStatus,
    string Status,
    LocalSignalState Direction,
    string? Reason,
    DateTimeOffset CandleTimeUtc,
    decimal? Price,
    decimal? Score,
    decimal? Confidence,
    DateTimeOffset OccurredAtUtc);

public sealed record LocalSignalChartMarker(
    string SignalId,
    LocalSignalState State,
    DateTimeOffset CandleTimeUtc,
    decimal? Price,
    string? Reason);

public sealed record LocalAnalystCheckpoint(
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset? LastProcessedCandleTimeUtc,
    LocalSignalState LastResult,
    string? LastReason,
    DateTimeOffset UpdatedAtUtc,
    LocalSignalSnapshot? Snapshot);

public sealed record LocalAnalystStatus(
    bool Enabled,
    string Symbol,
    string ConfigurationVersion,
    int EvaluationIntervalSeconds,
    IReadOnlyList<LocalAnalystCheckpoint> Timeframes);

public sealed record LocalSignalHistoryResult(
    string Symbol,
    MarketTimeframe? Timeframe,
    int Limit,
    IReadOnlyList<LocalSignalSnapshot> Signals);

public sealed record LocalSignalPersistenceRequest(
    LocalSignalMutation Mutation,
    Guid? ExistingSignalId,
    string? NewSignalId,
    LocalSignalDecision Decision,
    DateTimeOffset EvaluatedAtUtc);
