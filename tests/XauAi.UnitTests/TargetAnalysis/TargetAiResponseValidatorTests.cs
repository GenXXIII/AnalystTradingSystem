using System.Text.Json;
using System.Text.Json.Serialization;
using XauAi.Application.TargetAnalysis;

namespace XauAi.UnitTests.TargetAnalysis;

public sealed class TargetAiResponseValidatorTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void Specialist_accepts_one_evidence_cited_candidate()
    {
        var evidenceId = Guid.NewGuid();
        var output = Specialist(evidenceId);

        var result = new TargetAiResponseValidator().ValidateSpecialist(
            JsonSerializer.Serialize(output, Options),
            TargetWorkspace.Structure,
            [evidenceId]);

        Assert.True(result.HasCandidate);
        Assert.Equal(2415m, result.CandidateTargetPrice);
        Assert.Single(result.EvidenceIds);
    }

    [Fact]
    public void Specialist_rejects_unknown_fields_and_out_of_snapshot_citations()
    {
        var output = Specialist(Guid.NewGuid());
        var json = JsonSerializer.Serialize(output, Options)
            .Replace("\"summary\":", "\"decision\":\"BUY\",\"summary\":", StringComparison.Ordinal);

        var exception = Assert.Throws<TargetAnalysisException>(() =>
            new TargetAiResponseValidator().ValidateSpecialist(
                json,
                TargetWorkspace.Structure,
                [Guid.NewGuid()]));

        Assert.Equal(TargetAnalysisErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public void Master_rejects_target_values_in_no_valid_target_result()
    {
        var output = new TargetMasterOutput
        {
            ValidTarget = false,
            TargetPrice = 2415m,
            InvalidationPrice = 2380m,
            DirectionContext = TargetDirectionContext.Unknown,
            Confidence = 0.2m,
            ValidUntilUtc = null,
            ReasoningSummary = "Evidence conflicts.",
            Uncertainty = "High.",
            EvidenceIds = [],
            Conflicts = ["StructureFlowConflict"],
            SupportingWorkspaces = [],
            NoTargetReason = "CONFLICTING_EVIDENCE"
        };

        var exception = Assert.Throws<TargetAnalysisException>(() =>
            new TargetAiResponseValidator().ValidateMaster(
                JsonSerializer.Serialize(output, Options),
                []));

        Assert.Equal(TargetAnalysisErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public void Master_accepts_exactly_one_validated_target_contract()
    {
        var evidenceId = Guid.NewGuid();
        var output = new TargetMasterOutput
        {
            ValidTarget = true,
            TargetPrice = 2415m,
            InvalidationPrice = 2380m,
            DirectionContext = TargetDirectionContext.Upward,
            Confidence = 0.78m,
            ValidUntilUtc = DateTimeOffset.UtcNow.AddHours(4),
            ReasoningSummary = "Independent structure and liquidity evidence align.",
            Uncertainty = "News may increase volatility.",
            EvidenceIds = [evidenceId],
            Conflicts = [],
            SupportingWorkspaces = [TargetWorkspace.Structure, TargetWorkspace.Liquidity],
            NoTargetReason = null
        };

        var result = new TargetAiResponseValidator().ValidateMaster(
            JsonSerializer.Serialize(output, Options),
            [evidenceId]);

        Assert.True(result.ValidTarget);
        Assert.Equal(2415m, result.TargetPrice);
        Assert.Equal(2, result.SupportingWorkspaces.Count);
    }

    private static TargetSpecialistOutput Specialist(Guid evidenceId) => new()
    {
        Workspace = TargetWorkspace.Structure,
        HasCandidate = true,
        CandidateTargetPrice = 2415m,
        CandidateInvalidationPrice = 2380m,
        DirectionContext = TargetDirectionContext.Upward,
        Confidence = 0.75m,
        RiskAcceptable = true,
        Summary = "Higher-timeframe structure supports one forward area.",
        Reasoning = [new TargetCitedStatement("A confirmed structural level is present.", [evidenceId])],
        Obstacles = [],
        Uncertainty = "The shortest timeframe is transitional.",
        EvidenceIds = [evidenceId]
    };
}
