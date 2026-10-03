namespace XauAi.Domain.Evidence;

public sealed class EvidenceRecord
{
    public Guid Id { get; set; }

    public string Kind { get; set; } = string.Empty;

    public DateTimeOffset ObservedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
