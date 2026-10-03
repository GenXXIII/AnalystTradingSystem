namespace XauAi.Domain.EconomicData;

public sealed class EconomicSeries
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public string ExternalSeriesId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Units { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public string SeasonalAdjustment { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public DateOnly? ObservationStartDate { get; set; }

    public DateOnly? ObservationEndDate { get; set; }

    public DateTimeOffset? ProviderUpdatedAtUtc { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class EconomicObservation
{
    public Guid Id { get; set; }

    public Guid EconomicSeriesId { get; set; }

    public DateOnly ObservationDate { get; set; }

    public decimal? Value { get; set; }

    public string OriginalValue { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly? RealtimeStartDate { get; set; }

    public DateOnly? RealtimeEndDate { get; set; }

    public DateTimeOffset FetchedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class EconomicObservationRevision
{
    public Guid Id { get; set; }

    public Guid EconomicObservationId { get; set; }

    public decimal? Value { get; set; }

    public string OriginalValue { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly? RealtimeStartDate { get; set; }

    public DateOnly? RealtimeEndDate { get; set; }

    public DateTimeOffset FetchedAtUtc { get; set; }

    public DateTimeOffset SupersededAtUtc { get; set; }
}
