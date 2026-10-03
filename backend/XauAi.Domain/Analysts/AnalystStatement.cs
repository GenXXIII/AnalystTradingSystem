namespace XauAi.Domain.Analysts;

public sealed class AnalystStatement
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public Guid? InstrumentId { get; set; }

    public string? ExternalId { get; set; }

    public string? AnalystName { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? PermittedContent { get; set; }

    public string? SourceUrl { get; set; }

    public string? SourceUrlHash { get; set; }

    public string? Direction { get; set; }

    public decimal? TargetPrice { get; set; }

    public string? TimeHorizon { get; set; }

    public DateTimeOffset PublishedAtUtc { get; set; }

    public DateTimeOffset FetchedAtUtc { get; set; }
}
