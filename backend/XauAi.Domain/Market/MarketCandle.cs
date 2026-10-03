namespace XauAi.Domain.Market;

public sealed class MarketCandle
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid TimeframeId { get; set; }

    public Guid DataProviderId { get; set; }

    public string ProviderSymbol { get; set; } = string.Empty;

    public DateTimeOffset OpenTimeUtc { get; set; }

    public DateTimeOffset CloseTimeUtc { get; set; }

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public decimal? TickVolume { get; set; }

    public decimal? RealVolume { get; set; }

    public decimal? Spread { get; set; }

    public bool IsComplete { get; set; }

    public string? SourceTimeZone { get; set; }

    public DateTimeOffset FetchedAtUtc { get; set; }
}
