namespace XauAi.Domain.ReferenceData;

public sealed class TimeframeDefinition
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}
