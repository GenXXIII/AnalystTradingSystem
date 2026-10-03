namespace XauAi.Domain.Signals;

public sealed class TradingSignal
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid? TimeframeId { get; set; }

    public Guid StrategyVersionId { get; set; }

    public Guid? StrategyEvaluationId { get; set; }

    public Guid? AiAnalysisId { get; set; }

    public string Direction { get; set; } = string.Empty;

    public decimal? EntryPrice { get; set; }

    public decimal? StopLossPrice { get; set; }

    public decimal? TakeProfitPrice { get; set; }

    public int? TimeHorizonSeconds { get; set; }

    public decimal? ModelConfidence { get; set; }

    public string? RiskConditionsJson { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset SignalAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
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
