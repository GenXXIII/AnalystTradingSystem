using System.Text.Json;
using System.Text.Json.Serialization;
using XauAi.Application.FullAnalysis;

namespace XauAi.UnitTests.FullAnalysis;

public sealed class FullAnalysisTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void Master_accepts_evidence_supported_buy()
    {
        var evidenceId = Guid.NewGuid();
        var output = Master(FullDecision.Buy, evidenceId);

        var result = new FullAiResponseValidator().ValidateMaster(
            JsonSerializer.Serialize(output, Options),
            [evidenceId]);

        Assert.Equal(FullDecision.Buy, result.Decision);
        Assert.Equal(FullInvalidationCondition.AtOrBelow, result.Invalidation.Condition);
        Assert.Equal(2, result.SupportingWorkspaces.Count);
    }

    [Fact]
    public void Master_rejects_future_context_for_wait()
    {
        var output = Master(FullDecision.Wait, Guid.NewGuid()) with
        {
            ValidUntilUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        var exception = Assert.Throws<FullAnalysisException>(() =>
            new FullAiResponseValidator().ValidateMaster(JsonSerializer.Serialize(output, Options), []));

        Assert.Equal(FullAnalysisErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public void Specialist_rejects_direction_without_snapshot_evidence()
    {
        var output = new FullSpecialistOutput
        {
            Workspace = FullWorkspace.Structure,
            Direction = FullDecision.Sell,
            EvidenceIds = [],
            KeyFindings = ["Lower high confirmed."],
            Impact = "High",
            Confidence = 0.74m,
            Uncertainty = "The lowest timeframe is mixed.",
            Invalidation = "A higher high would invalidate this interpretation.",
            Summary = "Structure is currently bearish.",
            InsufficientEvidence = false
        };

        Assert.Throws<FullAnalysisException>(() =>
            new FullAiResponseValidator().ValidateSpecialist(
                JsonSerializer.Serialize(output, Options),
                FullWorkspace.Structure,
                []));
    }

    [Fact]
    public void Result_validator_accepts_wait_without_forcing_signal()
    {
        var validator = new FullResultValidator(new FullAnalystSettings { MinimumConfidence = 0.65m });
        var wait = Master(FullDecision.Wait, Guid.NewGuid());

        var failure = validator.Validate(wait, Specialist(FullWorkspace.Risk, FullDecision.Wait, []), Snapshot(), []);

        Assert.Null(failure);
    }

    [Fact]
    public void Result_validator_rejects_low_confidence_direction()
    {
        var evidenceId = Guid.NewGuid();
        var master = Master(FullDecision.Buy, evidenceId) with { Confidence = 0.4m };
        var validator = new FullResultValidator(new FullAnalystSettings { MinimumConfidence = 0.65m });

        var failure = validator.Validate(master, Specialist(FullWorkspace.Risk, FullDecision.Wait, [evidenceId]), Snapshot(), []);

        Assert.Equal("CONFIDENCE_BELOW_THRESHOLD", failure);
    }

    [Fact]
    public async Task Workspace_runner_reuses_same_state_hash_without_new_provider_call()
    {
        var evidenceId = Guid.NewGuid();
        var provider = new StubProvider(JsonSerializer.Serialize(
            Specialist(FullWorkspace.Structure, FullDecision.Buy, [evidenceId]),
            Options));
        var configuration = Configuration(FullWorkspace.Structure);
        var runner = new FullWorkspaceRunner(
            new FullWorkspaceCatalog([configuration]),
            new StubFactory(provider),
            new FullAiResponseValidator(),
            new XauAi.Application.AI.ScopedAiProviderRequestGate(TimeProvider.System),
            new FullAnalystSettings { CacheMinutes = 5 },
            TimeProvider.System);
        var request = new FullAiRequest(
            FullWorkspace.Structure,
            "XAUUSD",
            "M15",
            DateTimeOffset.UtcNow,
            configuration.PromptVersion,
            "stable-state",
            "{}",
            [evidenceId]);

        var first = await runner.RunSpecialistAsync(request);
        var second = await runner.RunSpecialistAsync(request with { AnalysisTimeUtc = request.AnalysisTimeUtc.AddSeconds(1) });

        Assert.Equal(FullWorkspaceExecutionStatus.Completed, first.Status);
        Assert.Equal(FullWorkspaceExecutionStatus.Cached, second.Status);
        Assert.True(second.CacheHit);
        Assert.Equal(1, provider.Calls);
    }

    private static FullMasterOutput Master(FullDecision decision, Guid evidenceId) => new()
    {
        Decision = decision,
        Confidence = decision == FullDecision.Wait ? 0.3m : 0.78m,
        Agreement = decision == FullDecision.Wait ? 0.2m : 0.76m,
        Conflicts = decision == FullDecision.Wait ? ["Evidence conflicts."] : [],
        KeyEvidenceIds = decision == FullDecision.Wait ? [] : [evidenceId],
        Reasoning = decision == FullDecision.Wait ? "No defensible direction." : "Independent evidence supports the current direction.",
        Invalidation = decision switch
        {
            FullDecision.Buy => new FullInvalidation("Break below support.", 2380m, FullInvalidationCondition.AtOrBelow),
            FullDecision.Sell => new FullInvalidation("Break above resistance.", 2420m, FullInvalidationCondition.AtOrAbove),
            _ => new FullInvalidation("No active direction to invalidate.", null, FullInvalidationCondition.None)
        },
        Uncertainty = "News can change current relevance.",
        ValidUntilUtc = decision == FullDecision.Wait ? null : DateTimeOffset.UtcNow.AddHours(2),
        SupportingWorkspaces = decision == FullDecision.Wait ? [] : [FullWorkspace.Structure, FullWorkspace.Liquidity]
    };

    private static FullSpecialistOutput Specialist(
        FullWorkspace workspace,
        FullDecision direction,
        IReadOnlyList<Guid> evidenceIds) => new()
        {
            Workspace = workspace,
            Direction = direction,
            EvidenceIds = evidenceIds,
            KeyFindings = ["Evidence is bounded to the snapshot."],
            Impact = "Medium",
            Confidence = 0.72m,
            Uncertainty = "Short-term reaction may change.",
            Invalidation = "A material opposing structure change invalidates this interpretation.",
            Summary = "The workspace completed its scoped interpretation.",
            InsufficientEvidence = false
        };

    private static FullAnalysisSnapshot Snapshot() => new(
        "XAUUSD",
        2400m,
        DateTimeOffset.UtcNow,
        XauAi.Application.MarketData.MarketTimeframe.M15,
        [XauAi.Application.MarketData.MarketTimeframe.M15],
        "Closed",
        "Aligned",
        new Dictionary<string, string>(),
        [],
        new Dictionary<string, string>(),
        new Dictionary<FullWorkspace, string>(),
        new Dictionary<FullWorkspace, string>(),
        [],
        []);

    private static FullWorkspaceConfiguration Configuration(FullWorkspace workspace) => new(
        workspace,
        true,
        "Test",
        "Stub",
        false,
        string.Empty,
        "test-model",
        [],
        "https://example.test/v1",
        0.1,
        30,
        1_000,
        true,
        0,
        0,
        "phase14-test",
        "phase14-test");

    private sealed class StubFactory(IFullAiProvider provider) : IFullAiProviderFactory
    {
        public IFullAiProvider Create(string adapter) => provider;
    }

    private sealed class StubProvider(string json) : IFullAiProvider
    {
        public string Adapter => "Stub";
        public int Calls { get; private set; }

        public Task<FullAiCompletion> AnalyzeAsync(
            FullAiRequest request,
            FullWorkspaceConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new FullAiCompletion(json, 10, 20, 5, "test"));
        }
    }
}
