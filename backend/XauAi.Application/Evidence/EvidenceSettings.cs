namespace XauAi.Application.Evidence;

public sealed class EvidenceSettings
{
    public int MaximumPageSize { get; init; } = 200;

    public int MaximumQueryRangeDays { get; init; } = 365;

    public int DefaultPackLookbackDays { get; init; } = 30;

    public int MaximumPackLookbackDays { get; init; } = 730;

    public int MaximumPackItemsPerType { get; init; } = 100;

    public int IngestionBatchSize { get; init; } = 250;

    public int ConflictWindowHours { get; init; } = 24;

    public int MaximumConflicts { get; init; } = 200;
}
