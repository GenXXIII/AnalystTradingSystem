namespace XauAi.Domain.Statistics;

public sealed class PerformanceStatistic
{
    public Guid Id { get; set; }

    public Guid StrategyVersionId { get; set; }

    public Guid? InstrumentId { get; set; }

    public Guid? TimeframeId { get; set; }

    public Guid? BacktestRunId { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string? MarketRegime { get; set; }

    public DateTimeOffset RangeStartUtc { get; set; }

    public DateTimeOffset RangeEndUtc { get; set; }

    public int SampleSize { get; set; }

    public decimal? WinRate { get; set; }

    public decimal? LossRate { get; set; }

    public decimal? Expectancy { get; set; }

    public decimal? ProfitFactor { get; set; }

    public decimal? MaximumDrawdown { get; set; }

    public decimal? AverageR { get; set; }

    public decimal? AverageFavorableExcursion { get; set; }

    public decimal? AverageAdverseExcursion { get; set; }

    public string CalculationVersion { get; set; } = string.Empty;

    public DateTimeOffset CalculatedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
