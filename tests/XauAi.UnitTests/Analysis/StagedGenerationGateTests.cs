using XauAi.Application.AI;
using XauAi.Application.FullAnalysis;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;
using XauAi.Application.TargetAnalysis;

namespace XauAi.UnitTests.Analysis;

public sealed class StagedGenerationGateTests
{
    [Fact]
    public void Target_market_gate_requires_one_aligned_timeframe()
    {
        var weak = TargetSnapshot([TargetFrame(MarketTimeframe.M15, "Bullish", "Bearish:Mixed")]);
        var strong = TargetSnapshot([
            TargetFrame(MarketTimeframe.M15, "Bullish", "Bullish:HigherHighs")
        ]);

        Assert.False(TargetGenerationGate.HasDeterministicCandidate(weak));
        Assert.True(TargetGenerationGate.HasDeterministicCandidate(strong));
    }

    [Fact]
    public void Target_market_gate_allows_mixed_but_meaningful_directional_evidence()
    {
        var mixed = TargetSnapshot([
            TargetFrame(MarketTimeframe.M1, "Bullish", "Bullish:HigherHighs"),
            TargetFrame(MarketTimeframe.M5, "Bullish", "Bullish:HigherHighs"),
            TargetFrame(MarketTimeframe.H4, "Bearish", "Bearish:LowerLows"),
            TargetFrame(MarketTimeframe.D1, "Bearish", "Bearish:LowerLows")
        ]);

        Assert.True(TargetGenerationGate.HasDeterministicCandidate(mixed));
    }

    [Fact]
    public void Target_market_gate_keeps_an_active_local_signal_from_crossing_a_dominant_direction()
    {
        var sell = new LocalSignalSnapshot(
            null, "XAUUSD", MarketTimeframe.M15, null, null, LocalSignalState.Sell, LocalSignalState.Sell,
            2_400m, 4, 6, 0.67m, "Bearish", "Bearish", "Bearish", "Bearish", "Bearish", "Normal",
            2_405m, 2_390m, null, DateTimeOffset.UtcNow.AddHours(1), "ACTIVE", "test", [],
            DateTimeOffset.UtcNow, null, null, null, false);
        var snapshot = TargetSnapshot([
            TargetFrame(MarketTimeframe.M1, "Bullish", "Bullish:HigherHighs"),
            TargetFrame(MarketTimeframe.M5, "Bullish", "Bullish:HigherHighs"),
            TargetFrame(MarketTimeframe.M15, "Bullish", "Bullish:HigherHighs"),
            TargetFrame(MarketTimeframe.H1, "Bearish", "Bearish:LowerLows")
        ]) with { LocalSignal = sell };

        Assert.False(TargetGenerationGate.HasDeterministicCandidate(snapshot));
    }

    [Fact]
    public void Target_scout_gate_requires_independent_directional_support()
    {
        var one = TargetRun(TargetWorkspace.Structure, TargetDirectionContext.Upward);
        var two = TargetRun(TargetWorkspace.Flow, TargetDirectionContext.Upward);

        Assert.Null(TargetGenerationGate.CandidateDirection([one], 0.6m, 2));
        Assert.Equal(TargetDirectionContext.Upward, TargetGenerationGate.CandidateDirection([one, two], 0.6m, 2));
    }

    [Fact]
    public void Target_total_evidence_gate_accepts_two_possible_candidates_without_perfect_confluence()
    {
        var upwardStructure = TargetRun(TargetWorkspace.Structure, TargetDirectionContext.Upward, 0.55m);
        var upwardCandle = TargetRun(TargetWorkspace.Candle, TargetDirectionContext.Upward, 0.55m);
        var downwardFlow = TargetRun(TargetWorkspace.Flow, TargetDirectionContext.Downward, 0.75m);

        Assert.Equal(
            TargetDirectionContext.Upward,
            TargetGenerationGate.CandidateDirection([upwardStructure, upwardCandle, downwardFlow], 0.50m, 2));
    }

    [Fact]
    public void Future_market_gate_requires_one_aligned_timeframe()
    {
        var weak = FullSnapshot([FullFrame(MarketTimeframe.M15, "Neutral", "Neutral:Range")]);
        var strong = FullSnapshot([
            FullFrame(MarketTimeframe.M15, "Bearish", "Bearish:LowerLows")
        ]);

        Assert.False(FullGenerationGate.HasDeterministicCandidate(weak));
        Assert.True(FullGenerationGate.HasDeterministicCandidate(strong));
    }

    [Fact]
    public void Future_market_gate_allows_specialists_to_resolve_a_directional_tie()
    {
        var mixed = FullSnapshot([
            FullFrame(MarketTimeframe.M1, "Bullish", "Bullish:HigherHighs"),
            FullFrame(MarketTimeframe.M5, "Bullish", "Bullish:HigherHighs"),
            FullFrame(MarketTimeframe.H4, "Bearish", "Bearish:LowerLows"),
            FullFrame(MarketTimeframe.D1, "Bearish", "Bearish:LowerLows")
        ]);

        Assert.True(FullGenerationGate.HasDeterministicCandidate(mixed));
    }

    [Fact]
    public void Provider_request_gate_keeps_target_and_future_independent()
    {
        var target = ScopedAiProviderRequestGate.ScopeKey("Target", "Groq", "https://api.groq.com/openai/v1/", "key");
        var future = ScopedAiProviderRequestGate.ScopeKey("Future", "groq", "https://api.groq.com/openai/v1", "key");
        var otherAccount = ScopedAiProviderRequestGate.ScopeKey("Future", "Groq", "https://api.groq.com/openai/v1", "other-key");

        Assert.NotEqual(target, future);
        Assert.NotEqual(target, otherAccount);
    }

    [Fact]
    public void Future_scout_gate_stops_on_conflict_and_accepts_two_matching_scouts()
    {
        var buy = FullRun(FullWorkspace.Structure, FullDecision.Buy);
        var sell = FullRun(FullWorkspace.Flow, FullDecision.Sell);
        var secondBuy = FullRun(FullWorkspace.Ktr, FullDecision.Buy);

        Assert.Null(FullGenerationGate.CandidateDirection([buy, sell], 0.65m, 2));
        Assert.Equal(FullDecision.Buy, FullGenerationGate.CandidateDirection([buy, secondBuy], 0.65m, 2));
    }

    [Fact]
    public void Future_total_evidence_gate_accepts_two_possible_signals_without_perfect_confluence()
    {
        var buyStructure = FullRun(FullWorkspace.Structure, FullDecision.Buy, 0.60m);
        var buyCandle = FullRun(FullWorkspace.Candle, FullDecision.Buy, 0.60m);
        var sellFlow = FullRun(FullWorkspace.Flow, FullDecision.Sell, 0.80m);

        Assert.Equal(
            FullDecision.Buy,
            FullGenerationGate.CandidateDirection([buyStructure, buyCandle, sellFlow], 0.55m, 2));
    }

    private static TargetAnalysisSnapshot TargetSnapshot(IReadOnlyList<TargetMarketFrameSnapshot> frames) => new(
        "XAUUSD", 2_400m, DateTimeOffset.UtcNow, MarketTimeframe.M15,
        [.. frames.Select(frame => frame.Timeframe)], "Aligned", "Closed",
        new Dictionary<string, string>(), [], new Dictionary<string, string>(),
        new Dictionary<TargetWorkspace, string>(), new Dictionary<TargetWorkspace, string>(), frames, []);

    private static TargetMarketFrameSnapshot TargetFrame(MarketTimeframe timeframe, string trend, string structure) => new(
        timeframe, "AllTick", DateTimeOffset.UtcNow, 2_399m, 2_402m, 2_398m, 2_400m, 100m,
        trend, structure, "RSI:Neutral;MACD:Neutral", "Normal", 5m, [], [], [], 205);

    private static TargetWorkspaceRunResult TargetRun(
        TargetWorkspace workspace,
        TargetDirectionContext direction,
        decimal confidence = 0.75m) => new(
        Guid.NewGuid(), workspace, TargetWorkspaceExecutionStatus.Completed,
        new TargetSpecialistOutput
        {
            Workspace = workspace,
            HasCandidate = true,
            CandidateTargetPrice = direction == TargetDirectionContext.Upward ? 2_410m : 2_390m,
            CandidateInvalidationPrice = direction == TargetDirectionContext.Upward ? 2_395m : 2_405m,
            DirectionContext = direction,
            Confidence = confidence,
            RiskAcceptable = true,
            Summary = "Bounded candidate.",
            Reasoning = [],
            Obstacles = [],
            Uncertainty = "May change.",
            EvidenceIds = [Guid.NewGuid()]
        },
        null, "{}", TargetConfiguration(workspace), 10, 10, 1, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static TargetWorkspaceConfiguration TargetConfiguration(TargetWorkspace workspace) => new(
        workspace, true, "Test", "Stub", false, string.Empty, "model", [], "https://example.test/v1",
        0.1, 30, 450, true, 0, 30, "test", "test");

    private static FullAnalysisSnapshot FullSnapshot(IReadOnlyList<FullMarketFrameSnapshot> frames) => new(
        "XAUUSD", 2_400m, DateTimeOffset.UtcNow, MarketTimeframe.M15,
        [.. frames.Select(frame => frame.Timeframe)], "Closed", "Aligned",
        new Dictionary<string, string>(), [], new Dictionary<string, string>(),
        new Dictionary<FullWorkspace, string>(), new Dictionary<FullWorkspace, string>(), frames, []);

    private static FullMarketFrameSnapshot FullFrame(MarketTimeframe timeframe, string trend, string structure) => new(
        timeframe, "AllTick", DateTimeOffset.UtcNow, 2_399m, 2_402m, 2_398m, 2_400m, 100m,
        trend, structure, "RSI:Neutral;MACD:Neutral", "Normal", 5m, [], [], [], 205);

    private static FullWorkspaceRunResult FullRun(
        FullWorkspace workspace,
        FullDecision direction,
        decimal confidence = 0.75m) => new(
        Guid.NewGuid(), workspace, FullWorkspaceExecutionStatus.Completed,
        new FullSpecialistOutput
        {
            Workspace = workspace,
            Direction = direction,
            EvidenceIds = [Guid.NewGuid()],
            KeyFindings = ["Bounded evidence."],
            Impact = "Medium",
            Confidence = confidence,
            Uncertainty = "May change.",
            Invalidation = "Opposing structure.",
            Summary = "Scoped result.",
            InsufficientEvidence = false
        },
        null, "{}", FullConfiguration(workspace), "hash", false, 10, 10, 1, null, null,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static FullWorkspaceConfiguration FullConfiguration(FullWorkspace workspace) => new(
        workspace, true, "Test", "Stub", false, string.Empty, "model", [], "https://example.test/v1",
        0.1, 30, 400, true, 0, 30, "test", "test");
}
