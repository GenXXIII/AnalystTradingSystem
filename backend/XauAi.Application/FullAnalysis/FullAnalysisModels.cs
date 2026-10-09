using System.Text.Json.Serialization;
using XauAi.Application.MarketData;

namespace XauAi.Application.FullAnalysis;

public enum FullWorkspace
{
    Structure,
    Liquidity,
    Candle,
    Flow,
    Ktr,
    News,
    Risk,
    Master
}

public enum FullDecision
{
    Buy,
    Sell,
    Wait
}

public enum FullAnalysisStatus
{
    Analyzing,
    Active,
    Wait,
    Cancelled,
    Expired,
    Invalidated,
    Completed
}

public enum FullWorkspaceExecutionStatus
{
    Completed,
    Failed,
    Disabled,
    Cached
}

public enum FullInvalidationCondition
{
    None,
    AtOrBelow,
    AtOrAbove
}

public sealed record CreateFullAnalysisRequest(
    string Symbol,
    string Timeframe,
    DateTimeOffset? AnalysisTimeUtc = null);

public sealed record FullMarketFrameSnapshot(
    MarketTimeframe Timeframe,
    string Provider,
    DateTimeOffset LastCandleCloseTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal? TickVolume,
    string Trend,
    string Structure,
    string Momentum,
    string Volatility,
    decimal? Atr,
    IReadOnlyList<decimal> ImportantLevels,
    IReadOnlyList<string> CandleSignals,
    IReadOnlyList<string> Conflicts,
    int CandlesUsed)
{
    public IReadOnlyList<string> StrategySignals { get; init; } = [];
    public IReadOnlyList<string> FlowSignals { get; init; } = [];
    public IReadOnlyList<string> KtrLevels { get; init; } = [];
    public IReadOnlyList<string> IndicatorSignals { get; init; } = [];
}

public sealed record FullAnalysisSnapshot(
    string Symbol,
    decimal CurrentPrice,
    DateTimeOffset AnalysisTimeUtc,
    MarketTimeframe RequestedTimeframe,
    IReadOnlyList<MarketTimeframe> AvailableTimeframes,
    string CurrentCandleState,
    string MarketState,
    IReadOnlyDictionary<string, string> DataVersions,
    IReadOnlyList<Guid> EvidenceIds,
    IReadOnlyDictionary<string, string> ProviderInformation,
    IReadOnlyDictionary<FullWorkspace, string> AiConfigurationVersions,
    IReadOnlyDictionary<FullWorkspace, string> PromptVersions,
    IReadOnlyList<FullMarketFrameSnapshot> MarketFrames,
    IReadOnlyList<string> Conflicts);

public sealed record FullInvalidation(
    string Summary,
    decimal? Price,
    FullInvalidationCondition Condition);

public sealed record FullSpecialistOutput
{
    public required FullWorkspace Workspace { get; init; }
    public required FullDecision Direction { get; init; }
    public required IReadOnlyList<Guid> EvidenceIds { get; init; }
    public required IReadOnlyList<string> KeyFindings { get; init; }
    public required string Impact { get; init; }
    public required decimal Confidence { get; init; }
    public required string Uncertainty { get; init; }
    public required string Invalidation { get; init; }
    public required string Summary { get; init; }
    public required bool InsufficientEvidence { get; init; }
}

public sealed record FullMasterOutput
{
    public required FullDecision Decision { get; init; }
    public required decimal Confidence { get; init; }
    public required decimal Agreement { get; init; }
    public required IReadOnlyList<string> Conflicts { get; init; }
    public required IReadOnlyList<Guid> KeyEvidenceIds { get; init; }
    public required string Reasoning { get; init; }
    public required FullInvalidation Invalidation { get; init; }
    public required string Uncertainty { get; init; }
    public DateTimeOffset? ValidUntilUtc { get; init; }
    public required IReadOnlyList<FullWorkspace> SupportingWorkspaces { get; init; }
}

public sealed record FullAiRequest(
    FullWorkspace Workspace,
    string Instrument,
    string Timeframe,
    DateTimeOffset AnalysisTimeUtc,
    string PromptVersion,
    string StateHash,
    string PayloadJson,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record FullAiCompletion(
    string Json,
    int? InputTokens,
    int? OutputTokens,
    int LatencyMilliseconds,
    string? ProviderRequestId,
    string? Model = null);

public sealed record FullWorkspaceConfiguration(
    FullWorkspace Workspace,
    bool Enabled,
    string Provider,
    string Adapter,
    bool RequiresApiKey,
    [property: JsonIgnore] string ApiKey,
    string Model,
    IReadOnlyList<string> FallbackModels,
    string BaseUrl,
    double Temperature,
    int TimeoutSeconds,
    int MaxOutputTokens,
    bool DisableReasoning,
    int MaxRetries,
    int RequestsPerMinute,
    string PromptVersion,
    string ConfigurationVersion);

public sealed record FullWorkspaceConfigurationView(
    FullWorkspace Workspace,
    bool Enabled,
    string Provider,
    string Adapter,
    string Model,
    IReadOnlyList<string> FallbackModels,
    string BaseUrl,
    double Temperature,
    int TimeoutSeconds,
    int MaxOutputTokens,
    bool DisableReasoning,
    int MaxRetries,
    int RequestsPerMinute,
    string PromptVersion,
    string ConfigurationVersion,
    bool HasApiKey);

public sealed record FullWorkspaceRunResult(
    Guid Id,
    FullWorkspace Workspace,
    FullWorkspaceExecutionStatus Status,
    FullSpecialistOutput? SpecialistOutput,
    FullMasterOutput? MasterOutput,
    string OutputJson,
    FullWorkspaceConfiguration Configuration,
    string InputHash,
    bool CacheHit,
    int? InputTokens,
    int? OutputTokens,
    int? LatencyMilliseconds,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CompletedAtUtc);

public sealed record FullAnalysisResult(
    Guid Id,
    string Symbol,
    DateTimeOffset AnalysisTimeUtc,
    decimal? CurrentPrice,
    MarketTimeframe Timeframe,
    FullDecision Decision,
    decimal? Confidence,
    decimal? Agreement,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<Guid> KeyEvidenceIds,
    string Reasoning,
    FullInvalidation? Invalidation,
    string Uncertainty,
    DateTimeOffset? ValidUntilUtc,
    bool FutureAvailable,
    IReadOnlyList<FullWorkspaceRunResult> WorkspaceResults,
    Guid? MasterResultId,
    FullAnalysisStatus Status,
    string Provider,
    string Model,
    string PromptVersion,
    string ConfigurationVersion,
    string SnapshotHash,
    FullAnalysisSnapshot? Snapshot,
    int InputTokens,
    int OutputTokens,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? EndedAtUtc);

public sealed record FullLifecycleItem(
    Guid Id,
    string EventType,
    FullAnalysisStatus? PreviousStatus,
    FullAnalysisStatus Status,
    string? Reason,
    decimal? Price,
    DateTimeOffset OccurredAtUtc);

public sealed record PagedFullAnalyses(
    IReadOnlyList<FullAnalysisResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record FullAnalysisQuery(
    string Symbol,
    MarketTimeframe? Timeframe,
    FullAnalysisStatus? Status,
    int Page,
    int PageSize);

public sealed record FullAnalysisJobWriteModel(
    Guid Id,
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset AnalysisTimeUtc,
    string PromptVersion,
    string ConfigurationVersion,
    DateTimeOffset CreatedAtUtc);

public sealed record FullSnapshotWriteModel(
    Guid AnalysisId,
    decimal CurrentPrice,
    string SnapshotHash,
    string SnapshotJson,
    DateTimeOffset UpdatedAtUtc);

public sealed record FullAnalysisCompletionWriteModel(
    Guid AnalysisId,
    FullDecision Decision,
    decimal Confidence,
    decimal Agreement,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<Guid> KeyEvidenceIds,
    string Reasoning,
    FullInvalidation Invalidation,
    string Uncertainty,
    DateTimeOffset? ValidUntilUtc,
    IReadOnlyList<FullWorkspaceRunResult> WorkspaceResults,
    Guid? MasterResultId,
    string Provider,
    string Model,
    string PromptVersion,
    string ConfigurationVersion,
    DateTimeOffset CompletedAtUtc);

public sealed record FullLifecycleTransition(
    Guid AnalysisId,
    FullAnalysisStatus ExpectedStatus,
    FullAnalysisStatus NewStatus,
    string EventType,
    string? Reason,
    decimal? Price,
    DateTimeOffset OccurredAtUtc);

public sealed class FullAnalystSettings
{
    public bool Enabled { get; init; }
    public string Symbol { get; init; } = "XAUUSD";
    public IReadOnlyList<MarketTimeframe> Timeframes { get; init; } = Enum.GetValues<MarketTimeframe>();
    public int EvidenceLookbackHours { get; init; } = 720;
    public int MaximumEvidenceItemsPerWorkspace { get; init; } = 8;
    public int MaximumCompressedCharacters { get; init; } = 2_500;
    public int MaximumTotalTokens { get; init; } = 15_000;
    public int MinimumMarketTimeframes { get; init; } = 3;
    public int StaleAfterIntervals { get; init; } = 3;
    public decimal MinimumConfidence { get; init; } = 0.65m;
    public int DefaultValidityMinutes { get; init; } = 120;
    public int MaximumValidityMinutes { get; init; } = 1_440;
    public int CacheMinutes { get; init; } = 5;
    public int MonitorIntervalSeconds { get; init; } = 30;
    public int MaximumPageSize { get; init; } = 200;
    public string ConfigurationVersion { get; init; } = "phase14-v1";
}

public sealed class FullWorkspaceCatalog(IEnumerable<FullWorkspaceConfiguration> configurations)
{
    private readonly IReadOnlyDictionary<FullWorkspace, FullWorkspaceConfiguration> configurations =
        configurations.ToDictionary(configuration => configuration.Workspace);

    public FullWorkspaceConfiguration Get(FullWorkspace workspace) =>
        configurations.TryGetValue(workspace, out var configuration)
            ? configuration
            : throw new FullAnalysisException(
                FullAnalysisErrorCodes.WorkspaceNotConfigured,
                $"The {workspace} Full AI workspace is not configured.");

    internal IReadOnlyList<FullWorkspaceConfiguration> Configurations => [.. configurations.Values];

    public IReadOnlyList<FullWorkspaceConfigurationView> GetSafeViews() =>
        [.. configurations.Values.OrderBy(configuration => configuration.Workspace)
            .Select(configuration => new FullWorkspaceConfigurationView(
                configuration.Workspace,
                configuration.Enabled,
                configuration.Provider,
                configuration.Adapter,
                configuration.Model,
                configuration.FallbackModels,
                configuration.BaseUrl,
                configuration.Temperature,
                configuration.TimeoutSeconds,
                configuration.MaxOutputTokens,
                configuration.DisableReasoning,
                configuration.MaxRetries,
                configuration.RequestsPerMinute,
                configuration.PromptVersion,
                configuration.ConfigurationVersion,
                !string.IsNullOrWhiteSpace(configuration.ApiKey)))];
}
