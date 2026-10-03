namespace XauAi.Domain.ReferenceData;

public sealed class Instrument
{
    public Guid Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string AssetClass { get; set; } = string.Empty;

    public string? BaseAsset { get; set; }

    public string? QuoteAsset { get; set; }

    public short PriceScale { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
