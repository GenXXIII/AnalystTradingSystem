using System.Text.Json.Serialization;
using XauAi.Application.AI;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;

namespace XauAi.Application.TargetAnalysis;

public enum TargetWorkspace
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

public enum TargetAnalysisStatus
{
    Analyzing,
    Success,
    Active,
    NoValidTarget,
    TargetHit,
    Invalidated,
    Expired,
    Cancelled
}

public enum TargetDirectionContext
{
    Unknown,
    Upward,
    Downward,
    TwoSided
}

public enum TargetWorkspaceExecutionStatus
{
    Completed,
    Failed,
    Disabled
}

public sealed record CreateTargetAnalysisRequest(
    string Symbol,
    string Timeframe,
    DateTimeOffset? AnalysisTimeUtc = null);

public sealed record TargetMarketFrameSnapshot(
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
    int CandlesUsed);

public sealed record TargetAnalysisSnapshot(
    string Symbol,
    decimal CurrentPrice,
    DateTimeOffset AnalysisTimeUtc,
    MarketTimeframe RequestedTimeframe,
    IReadOnlyList<MarketTimeframe> AvailableTimeframes,
    string CurrentMarketState,
    string CandleState,
    IReadOnlyDictionary<string, string> DataVersions,
    IReadOnlyList<Guid> EvidenceIds,
    IReadOnlyDictionary<string, string> ProviderInformation,
    IReadOnlyDictionary<TargetWorkspace, string> AiConfigurationVersions,
    IReadOnlyDictionary<TargetWorkspace, string> PromptVersions,
    IReadOnlyList<TargetMarketFrameSnapshot> MarketFrames,
    IReadOnlyList<string> Conflicts,
    LocalSignalSnapshot? LocalSignal = null);

public sealed record TargetCitedStatement(
    string Text,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record TargetSpecialistOutput
{
    public required TargetWorkspace Workspace { get; init; }

    public required bool HasCandidate { get; init; }

    public decimal? CandidateTargetPrice { get; init; }

    public decimal? CandidateInvalidationPrice { get; init; }

    public required TargetDirectionContext DirectionContext { get; init; }

    public required decimal Confidence { get; init; }

    public required bool RiskAcceptable { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<TargetCitedStatement> Reasoning { get; init; }

    public required IReadOnlyList<string> Obstacles { get; init; }

    public required string Uncertainty { get; init; }

    public required IReadOnlyList<Guid> EvidenceIds { get; init; }
}

public sealed record TargetMasterOutput
{
    public required bool ValidTarget { get; init; }

    public decimal? TargetPrice { get; init; }

    public decimal? InvalidationPrice { get; init; }

    public required TargetDirectionContext DirectionContext { get; init; }

    public required decimal Confidence { get; init; }

    public DateTimeOffset? ValidUntilUtc { get; init; }

    public required string ReasoningSummary { get; init; }

    public required string Uncertainty { get; init; }

    public required IReadOnlyList<Guid> EvidenceIds { get; init; }

    public required IReadOnlyList<string> Conflicts { get; init; }

    public required IReadOnlyList<TargetWorkspace> SupportingWorkspaces { get; init; }

    public string? NoTargetReason { get; init; }
}

public sealed record TargetAiRequest(
    TargetWorkspace Workspace,
    string Instrument,
    string Timeframe,
    DateTimeOffset AnalysisTimeUtc,
    string PromptVersion,
    string PayloadJson,
    IReadOnlyList<Guid> EvidenceIds);

public sealed record TargetAiCompletion(
    string Json,
    int? InputTokens,
    int? OutputTokens,
    int LatencyMilliseconds,
    string? ProviderRequestId);

public sealed record TargetWorkspaceConfiguration(
    TargetWorkspace Workspace,
    bool Enabled,
    string Provider,
    string Adapter,
    bool RequiresApiKey,
    string ApiKey,
    string Model,
    string BaseUrl,
    double Temperature,
    int TimeoutSeconds,
    int MaxOutputTokens,
    int MaxRetries,
    int RequestsPerMinute,
    string PromptVersion,
    string ConfigurationVersion);

public sealed record TargetWorkspaceConfigurationView(
    TargetWorkspace Workspace,
    bool Enabled,
    string Provider,
    string Adapter,
    string Model,
    string BaseUrl,
    double Temperature,
    int TimeoutSeconds,
    int MaxOutputTokens,
    int MaxRetries,
    int RequestsPerMinute,
    string PromptVersion,
    string ConfigurationVersion,
    bool HasApiKey);

public sealed record TargetWorkspaceRunResult(
    Guid Id,
    TargetWorkspace Workspace,
    TargetWorkspaceExecutionStatus Status,
    TargetSpecialistOutput? SpecialistOutput,
    TargetMasterOutput? MasterOutput,
    string OutputJson,
    TargetWorkspaceConfiguration Configuration,
    int? InputTokens,
    int? OutputTokens,
    int? LatencyMilliseconds,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CompletedAtUtc);

public sealed record TargetSpecialistResultView(
    Guid Id,
    TargetWorkspace Workspace,
    TargetWorkspaceExecutionStatus Status,
    bool HasCandidate,
    decimal? CandidateTargetPrice,
    decimal? CandidateInvalidationPrice,
    TargetDirectionContext DirectionContext,
    decimal? Confidence,
    bool? RiskAcceptable,
    string Summary,
    string Uncertainty,
    IReadOnlyList<Guid> EvidenceIds,
    string Provider,
    string Model,
    string PromptVersion,
    string ConfigurationVersion,
    int? InputTokens,
    int? OutputTokens,
    int? LatencyMilliseconds,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record TargetAnalysisResult(
    Guid Id,
    string Symbol,
    DateTimeOffset AnalysisTimeUtc,
    decimal? CurrentPrice,
    MarketTimeframe Timeframe,
    decimal? TargetPrice,
    decimal? InvalidationPrice,
    TargetDirectionContext DirectionContext,
    decimal? Confidence,
    DateTimeOffset? ValidUntilUtc,
    IReadOnlyList<Guid> EvidenceIds,
    IReadOnlyList<TargetSpecialistResultView> SpecialistResults,
    Guid? MasterResultId,
    string ReasoningSummary,
    string Uncertainty,
    string? NoTargetReason,
    TargetAnalysisStatus Status,
    string Provider,
    string Model,
    string PromptVersion,
    string ConfigurationVersion,
    TargetAnalysisSnapshot? Snapshot,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? EndedAtUtc);

public sealed record TargetLifecycleItem(
    Guid Id,
    string EventType,
    TargetAnalysisStatus? PreviousStatus,
    TargetAnalysisStatus Status,
    string? Reason,
    decimal? Price,
    DateTimeOffset OccurredAtUtc);

public sealed record PagedTargetAnalyses(
    IReadOnlyList<TargetAnalysisResult> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record TargetAnalysisQuery(
    string Symbol,
    MarketTimeframe? Timeframe,
    TargetAnalysisStatus? Status,
    int Page,
    int PageSize);

public sealed record TargetAnalysisJobWriteModel(
    Guid Id,
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset AnalysisTimeUtc,
    string PromptVersion,
    string ConfigurationVersion,
    DateTimeOffset CreatedAtUtc);

public sealed record TargetSnapshotWriteModel(
    Guid AnalysisId,
    decimal CurrentPrice,
    string SnapshotJson,
    IReadOnlyList<Guid> EvidenceIds,
    DateTimeOffset UpdatedAtUtc);

public sealed record TargetAnalysisCompletionWriteModel(
    Guid AnalysisId,
    TargetMasterOutput Master,
    IReadOnlyList<TargetWorkspaceRunResult> WorkspaceResults,
    string Provider,
    string Model,
    string PromptVersion,
    string ConfigurationVersion,
    DateTimeOffset CompletedAtUtc);

public sealed record TargetNoValidTargetWriteModel(
    Guid AnalysisId,
    string Reason,
    string ReasoningSummary,
    string Uncertainty,
    IReadOnlyList<TargetWorkspaceRunResult> WorkspaceResults,
    string Provider,
    string Model,
    string PromptVersion,
    string ConfigurationVersion,
    DateTimeOffset CompletedAtUtc);

public sealed record TargetLifecycleTransition(
    Guid AnalysisId,
    TargetAnalysisStatus ExpectedStatus,
    TargetAnalysisStatus NewStatus,
    string EventType,
    string? Reason,
    decimal? Price,
    DateTimeOffset OccurredAtUtc);

public sealed class TargetAnalystSettings
{
    public bool Enabled { get; init; }

    public string Symbol { get; init; } = "XAUUSD";

    public IReadOnlyList<MarketTimeframe> Timeframes { get; init; } = Enum.GetValues<MarketTimeframe>();

    public int EvidenceLookbackHours { get; init; } = 720;

    public int MaximumEvidenceItemsPerWorkspace { get; init; } = 50;

    public int MaximumCompressedCharacters { get; init; } = 32_000;

    public int MinimumMarketTimeframes { get; init; } = 3;

    public int StaleAfterIntervals { get; init; } = 3;

    public decimal MinimumConfidence { get; init; } = 0.60m;

    public decimal MinimumTargetDistanceAtr { get; init; } = 0.25m;

    public int DefaultValidityMinutes { get; init; } = 240;

    public int MaximumValidityMinutes { get; init; } = 1_440;

    public int MonitorIntervalSeconds { get; init; } = 30;

    public int MaximumPageSize { get; init; } = 200;

    public string ConfigurationVersion { get; init; } = "phase13-v1";
}

public sealed class TargetWorkspaceCatalog(IEnumerable<TargetWorkspaceConfiguration> configurations)
{
    private readonly IReadOnlyDictionary<TargetWorkspace, TargetWorkspaceConfiguration> configurations =
        configurations.ToDictionary(configuration => configuration.Workspace);

    public TargetWorkspaceConfiguration Get(TargetWorkspace workspace) =>
        configurations.TryGetValue(workspace, out var configuration)
            ? configuration
            : throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.WorkspaceNotConfigured,
                $"The {workspace} target AI workspace is not configured.");

    internal IReadOnlyList<TargetWorkspaceConfiguration> Configurations => [.. configurations.Values];

    public IReadOnlyList<TargetWorkspaceConfigurationView> GetSafeViews() =>
        [.. configurations.Values.OrderBy(configuration => configuration.Workspace)
            .Select(configuration => new TargetWorkspaceConfigurationView(
                configuration.Workspace,
                configuration.Enabled,
                configuration.Provider,
                configuration.Adapter,
                configuration.Model,
                configuration.BaseUrl,
                configuration.Temperature,
                configuration.TimeoutSeconds,
                configuration.MaxOutputTokens,
                configuration.MaxRetries,
                configuration.RequestsPerMinute,
                configuration.PromptVersion,
                configuration.ConfigurationVersion,
                !string.IsNullOrWhiteSpace(configuration.ApiKey)))];
}
