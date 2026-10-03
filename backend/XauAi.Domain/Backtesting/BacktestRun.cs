namespace XauAi.Domain.Backtesting;

public sealed class BacktestRun
{
    public Guid Id { get; set; }

    public Guid StrategyVersionId { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid TimeframeId { get; set; }

    public string EngineVersion { get; set; } = string.Empty;

    public string ParametersJson { get; set; } = "{}";

    public string ParametersHash { get; set; } = string.Empty;

    public DateTimeOffset RangeStartUtc { get; set; }

    public DateTimeOffset RangeEndUtc { get; set; }

    public decimal StartingCapital { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public int? TradeCount { get; set; }

    public int? WinningTrades { get; set; }

    public int? LosingTrades { get; set; }

    public decimal? GrossProfit { get; set; }

    public decimal? GrossLoss { get; set; }

    public decimal? MaximumDrawdown { get; set; }

    public decimal? WinRate { get; set; }

    public decimal? Expectancy { get; set; }

    public string? ResultsJson { get; set; }

    public string? ErrorMessage { get; set; }
}

public sealed class BacktestTrade
{
    public Guid Id { get; set; }

    public Guid BacktestRunId { get; set; }

    public int SequenceNumber { get; set; }

    public string Direction { get; set; } = string.Empty;

    public DateTimeOffset EnteredAtUtc { get; set; }

    public DateTimeOffset? ExitedAtUtc { get; set; }

    public decimal EntryPrice { get; set; }

    public decimal? ExitPrice { get; set; }

    public decimal? ProfitLossAmount { get; set; }

    public decimal? ProfitLossR { get; set; }

    public string? AuditJson { get; set; }
}
