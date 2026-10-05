namespace XauAi.Domain.Analysts;

public sealed class AnalystStatement
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public Guid? AnalystSourceId { get; set; }

    public Guid? AnalystId { get; set; }

    public Guid? PublicationId { get; set; }

    public Guid? InstrumentId { get; set; }

    public string? ExternalId { get; set; }

    public string? AnalystName { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? PermittedContent { get; set; }

    public string? SourceUrl { get; set; }

    public string? SourceUrlHash { get; set; }

    public string InstrumentCode { get; set; } = "Unknown";

    public string AssetClass { get; set; } = "Other";

    public string? Direction { get; set; }

    public decimal? TargetPrice { get; set; }

    public decimal? TargetRangeLow { get; set; }

    public decimal? TargetRangeHigh { get; set; }

    public string? TargetCurrency { get; set; }

    public int? HorizonValue { get; set; }

    public string HorizonUnit { get; set; } = "Unknown";

    public string? TimeHorizon { get; set; }

    public decimal? Confidence { get; set; }

    public string? Reason { get; set; }

    public string Language { get; set; } = "und";

    public string Category { get; set; } = "Other";

    public string Status { get; set; } = "Published";

    public string? ClaimHash { get; set; }

    public DateTimeOffset PublishedAtUtc { get; set; }

    public DateTimeOffset FetchedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
