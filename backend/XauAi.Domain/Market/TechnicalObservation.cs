namespace XauAi.Domain.Market;

public sealed class TechnicalObservation
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid TimeframeId { get; set; }

    public string ObservationType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string CalculationVersion { get; set; } = string.Empty;

    public string ParametersHash { get; set; } = string.Empty;

    public string ParametersJson { get; set; } = "{}";

    public decimal? NumericValue { get; set; }

    public string? ValueText { get; set; }

    public string? ValuesJson { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
