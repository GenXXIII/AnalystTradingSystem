using System.Text.Json;
using System.Text.Json.Serialization;

namespace XauAi.Application.AI;

internal sealed class AiResponseValidator : IAiResponseValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public StructuredAiInterpretation Validate(
        string json,
        AiInterpretationType expectedType,
        IReadOnlyList<AiEvidenceCandidate> selectedEvidence)
    {
        StructuredAiInterpretation output;
        try
        {
            output = JsonSerializer.Deserialize<StructuredAiInterpretation>(json, SerializerOptions)
                ?? throw Invalid("The AI provider returned an empty JSON object.");
        }
        catch (JsonException exception)
        {
            throw new AiProviderException(
                AiInterpretationErrorCodes.InvalidResponse,
                "The AI provider returned a response that does not match the required interpretation schema.",
                exception);
        }

        if (output.InterpretationType != expectedType)
        {
            throw Invalid("The AI response interpretation type does not match the requested type.");
        }

        if (output.Confidence is < 0 or > 1)
        {
            throw Invalid("AI interpretation confidence must be between zero and one.");
        }

        if (string.IsNullOrWhiteSpace(output.Summary)
            || string.IsNullOrWhiteSpace(output.Uncertainty)
            || string.IsNullOrWhiteSpace(output.Mechanism))
        {
            throw Invalid("The AI response is missing required interpretation or uncertainty fields.");
        }

        if (output.AffectedAssets is null
            || output.EvidenceIds is null
            || output.Facts is null
            || output.Interpretations is null
            || output.Unknowns is null
            || output.Conflicts is null
            || output.Measurements is null)
        {
            throw Invalid("The AI response contains null where a structured array is required.");
        }

        if (output.AffectedAssets.Count == 0 || output.AffectedAssets.Any(string.IsNullOrWhiteSpace))
        {
            throw Invalid("The AI response must identify at least one affected asset.");
        }

        var selectedById = selectedEvidence.ToDictionary(item => item.Id);
        ValidateReferences(output.EvidenceIds, selectedById, "interpretation");
        if (output.EvidenceIds.Count == 0)
        {
            throw Invalid("The AI response must cite at least one selected evidence record.");
        }

        foreach (var statement in output.Facts)
        {
            if (statement is null || string.IsNullOrWhiteSpace(statement.Text))
            {
                throw Invalid("AI fact statements cannot be empty.");
            }

            if (statement.EvidenceIds is null || statement.EvidenceIds.Count == 0)
            {
                throw Invalid("AI fact statements must cite at least one selected evidence record.");
            }

            ValidateReferences(statement.EvidenceIds, selectedById, "fact");
        }

        foreach (var statement in output.Interpretations)
        {
            if (statement is null || string.IsNullOrWhiteSpace(statement.Text))
            {
                throw Invalid("AI interpretation statements cannot be empty.");
            }

            if (statement.EvidenceIds is null || statement.EvidenceIds.Count == 0)
            {
                throw Invalid("AI interpretation statements must cite at least one selected evidence record.");
            }

            ValidateReferences(statement.EvidenceIds, selectedById, "interpretation statement");
        }

        foreach (var conflict in output.Conflicts)
        {
            if (conflict is null
                || string.IsNullOrWhiteSpace(conflict.Description)
                || conflict.EvidenceIds is null
                || conflict.EvidenceIds.Count < 2)
            {
                throw Invalid("AI conflicts must describe and reference at least two evidence records.");
            }

            ValidateReferences(conflict.EvidenceIds, selectedById, "conflict");
        }

        foreach (var measurement in output.Measurements)
        {
            if (measurement is null
                || string.IsNullOrWhiteSpace(measurement.Name)
                || !selectedById.TryGetValue(measurement.EvidenceId, out var evidence)
                || !evidence.NumericValue.HasValue
                || evidence.NumericValue.Value != measurement.Value
                || !string.Equals(evidence.Unit, measurement.Unit, StringComparison.OrdinalIgnoreCase))
            {
                throw Invalid("The AI response contains a measurement that was not supplied by deterministic evidence.");
            }
        }

        if (output.Unknowns.Any(string.IsNullOrWhiteSpace))
        {
            throw Invalid("AI unknown statements cannot be empty.");
        }

        return output;
    }

    private static void ValidateReferences(
        IReadOnlyList<Guid> evidenceIds,
        IReadOnlyDictionary<Guid, AiEvidenceCandidate> selectedById,
        string field)
    {
        if (evidenceIds.Any(id => !selectedById.ContainsKey(id)))
        {
            throw Invalid($"The AI {field} references evidence that was not selected for this analysis.");
        }
    }

    private static AiProviderException Invalid(string message) =>
        new(AiInterpretationErrorCodes.InvalidResponse, message);
}
