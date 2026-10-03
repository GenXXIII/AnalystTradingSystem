namespace XauAi.Application.EconomicData;

public sealed class EconomicDataSettings
{
    public bool Enabled { get; init; }

    public string Provider { get; init; } = "FRED";

    public string ProviderKey { get; init; } = "fred";

    public int InitialHistoryYears { get; init; } = 20;

    public int RevisionLookbackDays { get; init; } = 370;

    public int SyncIntervalMinutes { get; init; } = 360;

    public int ProviderPageSize { get; init; } = 1000;

    public int MaximumPagesPerSeries { get; init; } = 100;

    public int MaximumPageSize { get; init; } = 500;

    public int MaximumQueryRangeYears { get; init; } = 100;

    public int MaxRetries { get; init; } = 2;

    public int RetryBaseDelaySeconds { get; init; } = 2;

    public IReadOnlyList<EconomicSeriesDefinition> Series { get; init; } = [];
}
