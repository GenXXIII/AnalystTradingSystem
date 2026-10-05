namespace XauAi.Infrastructure.Configuration.Options;

public sealed class EvidenceOptions
{
    public const string SectionName = "Evidence";

    public int MaximumPageSize { get; set; } = 200;

    public int MaximumQueryRangeDays { get; set; } = 365;

    public int DefaultPackLookbackDays { get; set; } = 30;

    public int MaximumPackLookbackDays { get; set; } = 730;

    public int MaximumPackItemsPerType { get; set; } = 100;

    public int IngestionBatchSize { get; set; } = 250;

    public int ConflictWindowHours { get; set; } = 24;

    public int MaximumConflicts { get; set; } = 200;
}
