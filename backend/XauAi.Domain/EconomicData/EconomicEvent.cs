namespace XauAi.Domain.EconomicData;

public sealed class EconomicEvent
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public string? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Importance { get; set; } = string.Empty;

    public DateTimeOffset ScheduledAtUtc { get; set; }

    public decimal? PreviousValue { get; set; }

    public decimal? ForecastValue { get; set; }

    public decimal? ActualValue { get; set; }

    public string? ValueUnit { get; set; }

    public string? RawValuesJson { get; set; }

    public DateTimeOffset FetchedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
