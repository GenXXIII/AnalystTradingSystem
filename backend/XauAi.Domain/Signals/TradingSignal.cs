namespace XauAi.Domain.Signals;

public sealed class TradingSignal
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid? TimeframeId { get; set; }

    public Guid StrategyVersionId { get; set; }

    public Guid? StrategyEvaluationId { get; set; }

    public Guid? AiAnalysisId { get; set; }

    public string? SignalKey { get; set; }

    public string? Source { get; set; }

    public string? SignalCandleId { get; set; }

    public DateTimeOffset? SignalCandleTimeUtc { get; set; }

    public string Direction { get; set; } = string.Empty;

    public decimal? EntryPrice { get; set; }

    public decimal? StopLossPrice { get; set; }

    public decimal? TakeProfitPrice { get; set; }

    public int? TimeHorizonSeconds { get; set; }

    public decimal? ModelConfidence { get; set; }

    public decimal? Score { get; set; }

    public decimal? MaxScore { get; set; }

    public string? StructureState { get; set; }

    public string? LiquidityState { get; set; }

    public string? CandleState { get; set; }

    public string? MomentumState { get; set; }

    public string? KtrState { get; set; }

    public string? VolatilityState { get; set; }

    public string? ExplanationJson { get; set; }

    public string? InvalidationReason { get; set; }

    public DateTimeOffset? ValidUntilUtc { get; set; }

    public DateTimeOffset? LastEvaluatedCandleTimeUtc { get; set; }

    public string? ConfigurationVersion { get; set; }

    public string? RiskConditionsJson { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset SignalAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? EndedAtUtc { get; set; }
}

public sealed class TradingSignalLifecycleEvent
{
    public Guid Id { get; set; }

    public Guid TradingSignalId { get; set; }

    public Guid? StrategyEvaluationId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? PreviousStatus { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Direction { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public DateTimeOffset CandleTimeUtc { get; set; }

    public decimal? Price { get; set; }

    public decimal? Score { get; set; }

    public decimal? Confidence { get; set; }

    public string? DetailsJson { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class LocalAnalystProcessingState
{
    public Guid InstrumentId { get; set; }

    public Guid TimeframeId { get; set; }

    public DateTimeOffset? LastProcessedCandleTimeUtc { get; set; }

    public Guid? LastStrategyEvaluationId { get; set; }

    public Guid? CurrentTradingSignalId { get; set; }

    public string LastResult { get; set; } = string.Empty;

    public string? LastReason { get; set; }

    public string? LastSnapshotJson { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class TradingSignalEvidence
{
    public Guid TradingSignalId { get; set; }

    public Guid EvidenceRecordId { get; set; }

    public string Role { get; set; } = string.Empty;
}

public sealed class SignalOutcome
{
    public Guid Id { get; set; }

    public Guid TradingSignalId { get; set; }

    public string OutcomeStatus { get; set; } = string.Empty;

    public decimal? ExitPrice { get; set; }

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public decimal? ProfitLossAmount { get; set; }

    public decimal? ProfitLossR { get; set; }

    public decimal? MaximumFavorableExcursion { get; set; }

    public decimal? MaximumAdverseExcursion { get; set; }

    public string? MeasurementDetailsJson { get; set; }

    public DateTimeOffset MeasuredAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
