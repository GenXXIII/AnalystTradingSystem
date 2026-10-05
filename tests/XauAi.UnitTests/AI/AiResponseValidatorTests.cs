using System.Text.Json;
using System.Text.Json.Serialization;
using XauAi.Application.AI;

namespace XauAi.UnitTests.AI;

public sealed class AiResponseValidatorTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void Valid_response_keeps_facts_interpretations_unknowns_and_conflicts_separate()
    {
        var first = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, DateTimeOffset.UtcNow);
        var second = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, DateTimeOffset.UtcNow);
        var output = Output(first.Id, second.Id);

        var result = new AiResponseValidator().Validate(
            JsonSerializer.Serialize(output, Options),
            AiInterpretationType.NewsEvent,
            [first, second]);

        Assert.Single(result.Facts);
        Assert.Single(result.Interpretations);
        Assert.Single(result.Unknowns);
        Assert.Single(result.Conflicts);
    }

    [Fact]
    public void Malformed_or_unknown_schema_fields_are_rejected()
    {
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(Output(evidence.Id, evidence.Id), Options)
            .Replace("\"summary\":", "\"decision\":\"BUY\",\"summary\":", StringComparison.Ordinal);

        var exception = Assert.Throws<AiProviderException>(() =>
            new AiResponseValidator().Validate(json, AiInterpretationType.NewsEvent, [evidence]));

        Assert.Equal(AiInterpretationErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public void References_to_unselected_evidence_are_rejected()
    {
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, DateTimeOffset.UtcNow);
        var output = Output(Guid.NewGuid(), Guid.NewGuid());

        var exception = Assert.Throws<AiProviderException>(() =>
            new AiResponseValidator().Validate(
                JsonSerializer.Serialize(output, Options),
                AiInterpretationType.NewsEvent,
                [evidence]));

        Assert.Equal(AiInterpretationErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public void Fact_and_interpretation_statements_require_evidence_citations()
    {
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "News", "XAUUSD", null, DateTimeOffset.UtcNow);
        var output = Output(evidence.Id, evidence.Id) with
        {
            Facts = [new AiInterpretationStatement("An uncited fact.", [])]
        };

        var exception = Assert.Throws<AiProviderException>(() =>
            new AiResponseValidator().Validate(
                JsonSerializer.Serialize(output, Options),
                AiInterpretationType.NewsEvent,
                [evidence]));

        Assert.Equal(AiInterpretationErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public void Measurements_not_supplied_by_deterministic_evidence_are_rejected()
    {
        var evidence = AiEvidenceSelectionTests.Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M15", DateTimeOffset.UtcNow);
        var output = Output(evidence.Id, evidence.Id) with
        {
            Measurements = [new AiInterpretationMeasurement(evidence.Id, "RSI", 71.2m, "Index")]
        };

        var exception = Assert.Throws<AiProviderException>(() =>
            new AiResponseValidator().Validate(
                JsonSerializer.Serialize(output, Options),
                AiInterpretationType.NewsEvent,
                [evidence]));

        Assert.Equal(AiInterpretationErrorCodes.InvalidResponse, exception.Code);
    }

    private static StructuredOutputRecord Output(Guid first, Guid second) => new(
        AiInterpretationType.NewsEvent,
        AiInterpretationDirection.Mixed,
        AiInterpretationImpact.High,
        ["XAUUSD", "USD"],
        "A possible transmission mechanism.",
        "Gold may weaken if USD strengthens.",
        "The observed reaction is mixed.",
        AiReactionAlignment.Mixed,
        AiCurrentRelevance.High,
        0.7m,
        "The persistence of the reaction remains uncertain.",
        "Evidence is mixed and requires confirmation.",
        [first, second],
        [new AiInterpretationStatement("The event was published.", [first])],
        [new AiInterpretationStatement("The reaction may be temporary.", [second])],
        ["Future persistence is unknown."],
        [new AiInterpretationConflict("The evidence points in different directions.", [first, second])],
        []);

    private sealed record StructuredOutputRecord(
        AiInterpretationType InterpretationType,
        AiInterpretationDirection Direction,
        AiInterpretationImpact Impact,
        IReadOnlyList<string> AffectedAssets,
        string Mechanism,
        string ExpectedEffect,
        string ObservedReaction,
        AiReactionAlignment ReactionAlignment,
        AiCurrentRelevance CurrentRelevance,
        decimal Confidence,
        string Uncertainty,
        string Summary,
        IReadOnlyList<Guid> EvidenceIds,
        IReadOnlyList<AiInterpretationStatement> Facts,
        IReadOnlyList<AiInterpretationStatement> Interpretations,
        IReadOnlyList<string> Unknowns,
        IReadOnlyList<AiInterpretationConflict> Conflicts,
        IReadOnlyList<AiInterpretationMeasurement> Measurements);
}
