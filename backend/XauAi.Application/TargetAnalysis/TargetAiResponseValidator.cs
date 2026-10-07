using System.Text.Json;
using System.Text.Json.Serialization;

namespace XauAi.Application.TargetAnalysis;

internal sealed class TargetAiResponseValidator : ITargetAiResponseValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public TargetSpecialistOutput ValidateSpecialist(
        string json,
        TargetWorkspace expectedWorkspace,
        IReadOnlyCollection<Guid> allowedEvidenceIds)
    {
        if (expectedWorkspace == TargetWorkspace.Master)
        {
            throw Invalid("The Master workspace must use the master response contract.");
        }

        TargetSpecialistOutput output;
        try
        {
            output = JsonSerializer.Deserialize<TargetSpecialistOutput>(json, SerializerOptions)
                ?? throw Invalid("The target AI response was empty.");
        }
        catch (JsonException exception)
        {
            throw Invalid("The target AI response did not match the required specialist schema.", exception);
        }

        if (output.Workspace != expectedWorkspace)
        {
            throw Invalid($"The target AI response identified the wrong workspace for {expectedWorkspace}.");
        }

        if (output.Reasoning is null || output.Obstacles is null || output.EvidenceIds is null)
        {
            throw Invalid("The target AI specialist response omitted a required collection.");
        }

        ValidateConfidence(output.Confidence);
        ValidateText(output.Summary, "summary");
        ValidateText(output.Uncertainty, "uncertainty");
        ValidateEvidence(output.EvidenceIds, allowedEvidenceIds);
        foreach (var statement in output.Reasoning)
        {
            ValidateText(statement.Text, "reasoning statement");
            ValidateEvidence(statement.EvidenceIds, allowedEvidenceIds);
        }

        if (output.HasCandidate)
        {
            if (!IsPrice(output.CandidateTargetPrice) || !IsPrice(output.CandidateInvalidationPrice))
            {
                throw Invalid("A target workspace candidate must include valid target and invalidation prices.");
            }

            if (output.EvidenceIds.Count == 0)
            {
                throw Invalid("A target workspace candidate must cite evidence.");
            }
        }
        else if (output.CandidateTargetPrice.HasValue || output.CandidateInvalidationPrice.HasValue)
        {
            throw Invalid("A workspace without a candidate cannot include target prices.");
        }

        return output;
    }

    public TargetMasterOutput ValidateMaster(
        string json,
        IReadOnlyCollection<Guid> allowedEvidenceIds)
    {
        TargetMasterOutput output;
        try
        {
            output = JsonSerializer.Deserialize<TargetMasterOutput>(json, SerializerOptions)
                ?? throw Invalid("The Target Master response was empty.");
        }
        catch (JsonException exception)
        {
            throw Invalid("The Target Master response did not match the required schema.", exception);
        }

        if (output.EvidenceIds is null || output.Conflicts is null || output.SupportingWorkspaces is null)
        {
            throw Invalid("The Target Master response omitted a required collection.");
        }

        ValidateConfidence(output.Confidence);
        ValidateText(output.ReasoningSummary, "reasoning summary");
        ValidateText(output.Uncertainty, "uncertainty");
        ValidateEvidence(output.EvidenceIds, allowedEvidenceIds);

        if (output.ValidTarget)
        {
            if (!IsPrice(output.TargetPrice) || !IsPrice(output.InvalidationPrice))
            {
                throw Invalid("A valid Target Master result must include one target and one invalidation price.");
            }

            if (!output.ValidUntilUtc.HasValue)
            {
                throw Invalid("A valid Target Master result must include a validity deadline.");
            }

            if (output.EvidenceIds.Count == 0)
            {
                throw Invalid("A valid Target Master result must cite evidence.");
            }

            if (!string.IsNullOrWhiteSpace(output.NoTargetReason))
            {
                throw Invalid("A valid target cannot also include a no-target reason.");
            }
        }
        else
        {
            if (output.TargetPrice.HasValue || output.InvalidationPrice.HasValue || output.ValidUntilUtc.HasValue)
            {
                throw Invalid("A no-valid-target response cannot include target lifecycle prices or dates.");
            }

            if (string.IsNullOrWhiteSpace(output.NoTargetReason))
            {
                throw Invalid("A no-valid-target response must explain why no target is defensible.");
            }
        }

        if (output.SupportingWorkspaces.Any(workspace => workspace is TargetWorkspace.Master or TargetWorkspace.Risk))
        {
            throw Invalid("Master and Risk cannot be listed as independent supporting specialist workspaces.");
        }

        return output;
    }

    private static void ValidateConfidence(decimal confidence)
    {
        if (confidence is < 0m or > 1m)
        {
            throw Invalid("Target AI confidence must be between zero and one.");
        }
    }

    private static void ValidateText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4_000)
        {
            throw Invalid($"The target AI {field} is missing or too long.");
        }
    }

    private static void ValidateEvidence(
        IEnumerable<Guid> evidenceIds,
        IReadOnlyCollection<Guid> allowedEvidenceIds)
    {
        var allowed = allowedEvidenceIds.ToHashSet();
        if (evidenceIds.Any(id => id == Guid.Empty || !allowed.Contains(id)))
        {
            throw Invalid("The target AI response cited evidence outside the immutable analysis snapshot.");
        }
    }

    private static bool IsPrice(decimal? value) => value is > 0m;

    private static TargetAnalysisException Invalid(string message, Exception? inner = null) => new(
        TargetAnalysisErrorCodes.InvalidResponse,
        message,
        inner);
}
