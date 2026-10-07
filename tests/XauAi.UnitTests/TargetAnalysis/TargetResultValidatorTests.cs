using XauAi.Application.AI;
using XauAi.Application.MarketData;
using XauAi.Application.TargetAnalysis;

namespace XauAi.UnitTests.TargetAnalysis;

public sealed class TargetResultValidatorTests
{
    [Fact]
    public void Defensible_single_target_passes_validation()
    {
        var now = new DateTimeOffset(2026, 10, 7, 6, 0, 0, TimeSpan.Zero);
        var evidence = Candidate(Guid.NewGuid(), now.AddMinutes(-10));
        var snapshot = Snapshot(now, evidence.Id);
        var master = Master(now, evidence.Id);
        var risk = Risk(evidence.Id);

        var result = Validator().Validate(
            master,
            risk,
            snapshot,
            new Dictionary<Guid, AiEvidenceCandidate> { [evidence.Id] = evidence },
            10m,
            Runs(evidence.Id, now));

        Assert.Null(result);
    }

    [Fact]
    public void Future_evidence_is_rejected_by_look_ahead_gate()
    {
        var now = new DateTimeOffset(2026, 10, 7, 6, 0, 0, TimeSpan.Zero);
        var evidence = Candidate(Guid.NewGuid(), now.AddMinutes(1));

        var result = Validator().Validate(
            Master(now, evidence.Id),
            Risk(evidence.Id),
            Snapshot(now, evidence.Id),
            new Dictionary<Guid, AiEvidenceCandidate> { [evidence.Id] = evidence },
            10m,
            Runs(evidence.Id, now));

        Assert.Equal("STALE_OR_LOOK_AHEAD_EVIDENCE", result);
    }

    [Fact]
    public void Risk_workspace_can_reject_a_master_target()
    {
        var now = new DateTimeOffset(2026, 10, 7, 6, 0, 0, TimeSpan.Zero);
        var evidence = Candidate(Guid.NewGuid(), now.AddMinutes(-1));
        var risk = Risk(evidence.Id) with { RiskAcceptable = false };

        var result = Validator().Validate(
            Master(now, evidence.Id),
            risk,
            Snapshot(now, evidence.Id),
            new Dictionary<Guid, AiEvidenceCandidate> { [evidence.Id] = evidence },
            10m,
            Runs(evidence.Id, now));

        Assert.Equal("RISK_REJECTED", result);
    }

    [Fact]
    public void Invalidation_must_be_on_the_protective_side_of_current_price()
    {
        var now = new DateTimeOffset(2026, 10, 7, 6, 0, 0, TimeSpan.Zero);
        var evidence = Candidate(Guid.NewGuid(), now.AddMinutes(-1));
        var master = Master(now, evidence.Id) with { InvalidationPrice = 2405m };

        var result = Validator().Validate(
            master,
            Risk(evidence.Id),
            Snapshot(now, evidence.Id),
            new Dictionary<Guid, AiEvidenceCandidate> { [evidence.Id] = evidence },
            10m,
            Runs(evidence.Id, now));

        Assert.Equal("INVALID_INVALIDATION", result);
    }

    private static TargetResultValidator Validator() => new(new TargetAnalystSettings
    {
        MinimumConfidence = 0.6m,
        MinimumTargetDistanceAtr = 0.25m,
        MaximumValidityMinutes = 1_440
    });

    private static TargetMasterOutput Master(DateTimeOffset now, Guid evidenceId) => new()
    {
        ValidTarget = true,
        TargetPrice = 2420m,
        InvalidationPrice = 2380m,
        DirectionContext = TargetDirectionContext.Upward,
        Confidence = 0.8m,
        ValidUntilUtc = now.AddHours(4),
        ReasoningSummary = "Structure and liquidity independently align.",
        Uncertainty = "Moderate event risk.",
        EvidenceIds = [evidenceId],
        Conflicts = [],
        SupportingWorkspaces = [TargetWorkspace.Structure, TargetWorkspace.Liquidity],
        NoTargetReason = null
    };

    private static TargetSpecialistOutput Risk(Guid evidenceId) => new()
    {
        Workspace = TargetWorkspace.Risk,
        HasCandidate = true,
        CandidateTargetPrice = 2420m,
        CandidateInvalidationPrice = 2380m,
        DirectionContext = TargetDirectionContext.Upward,
        Confidence = 0.72m,
        RiskAcceptable = true,
        Summary = "The proposed target is reachable without a critical obstacle.",
        Reasoning = [new TargetCitedStatement("Risk is acceptable.", [evidenceId])],
        Obstacles = [],
        Uncertainty = "Volatility can expand.",
        EvidenceIds = [evidenceId]
    };

    private static IReadOnlyList<TargetWorkspaceRunResult> Runs(Guid evidenceId, DateTimeOffset now) =>
    [
        Run(TargetWorkspace.Structure, evidenceId, now),
        Run(TargetWorkspace.Liquidity, evidenceId, now)
    ];

    private static TargetWorkspaceRunResult Run(
        TargetWorkspace workspace,
        Guid evidenceId,
        DateTimeOffset now)
    {
        var output = Risk(evidenceId) with { Workspace = workspace };
        var configuration = new TargetWorkspaceConfiguration(
            workspace,
            true,
            "test",
            "OpenAiCompatible",
            false,
            string.Empty,
            "test-model",
            "https://example.test/v1/",
            0.1,
            30,
            1000,
            0,
            0,
            "phase13-test-v1",
            "phase13-v1");
        return new TargetWorkspaceRunResult(
            Guid.NewGuid(),
            workspace,
            TargetWorkspaceExecutionStatus.Completed,
            output,
            null,
            "{}",
            configuration,
            null,
            null,
            1,
            null,
            null,
            now,
            now);
    }

    private static TargetAnalysisSnapshot Snapshot(DateTimeOffset now, Guid evidenceId) => new(
        "XAUUSD",
        2400m,
        now,
        MarketTimeframe.M5,
        [MarketTimeframe.M5, MarketTimeframe.H1, MarketTimeframe.H4],
        "BullishAligned",
        "MomentumCandle",
        new Dictionary<string, string>(),
        [evidenceId],
        new Dictionary<string, string>(),
        new Dictionary<TargetWorkspace, string>(),
        new Dictionary<TargetWorkspace, string>(),
        [],
        []);

    private static AiEvidenceCandidate Candidate(Guid id, DateTimeOffset availableAt) => new(
        id,
        "Technical",
        "Market",
        "test",
        null,
        id.ToString("N"),
        "XAUUSD",
        "M5",
        availableAt,
        availableAt,
        null,
        null,
        availableAt.AddHours(2),
        "Test evidence",
        "Test evidence summary",
        2400m,
        null,
        "USD",
        "Bullish",
        "High",
        "Structure",
        "High",
        "Complete",
        true,
        null,
        null,
        availableAt,
        null,
        []);
}
