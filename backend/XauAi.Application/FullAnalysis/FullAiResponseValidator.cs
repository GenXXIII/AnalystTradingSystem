using System.Text.Json;
using System.Text.Json.Serialization;

namespace XauAi.Application.FullAnalysis;

internal sealed class FullAiResponseValidator : IFullAiResponseValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public FullSpecialistOutput ValidateSpecialist(
        string json,
        FullWorkspace expectedWorkspace,
        IReadOnlyCollection<Guid> allowedEvidenceIds)
    {
        if (expectedWorkspace == FullWorkspace.Master)
        {
            throw Invalid("Full Master must use the master response contract.");
        }

        FullSpecialistOutput output;
        try
        {
            output = JsonSerializer.Deserialize<FullSpecialistOutput>(json, SerializerOptions)
                ?? throw Invalid("The Full AI specialist response was empty.");
        }
        catch (JsonException exception)
        {
            throw Invalid("The Full AI response did not match the required specialist schema.", exception);
        }

        if (output.Workspace != expectedWorkspace)
        {
            throw Invalid($"The Full AI response identified the wrong workspace for {expectedWorkspace}.");
        }

        if (output.EvidenceIds is null || output.KeyFindings is null)
        {
            throw Invalid("The Full AI specialist response omitted a required collection.");
        }

        ValidateRatio(output.Confidence, "confidence");
        ValidateText(output.Summary, "summary");
        ValidateText(output.Uncertainty, "uncertainty");
        ValidateText(output.Invalidation, "invalidation");
        ValidateText(output.Impact, "impact");
        ValidateEvidence(output.EvidenceIds, allowedEvidenceIds);
        if (output.KeyFindings.Any(string.IsNullOrWhiteSpace))
        {
            throw Invalid("Full AI key findings cannot contain empty values.");
        }

        if (output.Direction != FullDecision.Wait && output.EvidenceIds.Count == 0)
        {
            throw Invalid("A directional specialist interpretation must cite immutable evidence.");
        }

        if (output.InsufficientEvidence && output.Direction != FullDecision.Wait)
        {
            throw Invalid("An insufficient-evidence specialist result must return WAIT.");
        }

        return output;
    }

    public FullMasterOutput ValidateMaster(string json, IReadOnlyCollection<Guid> allowedEvidenceIds)
    {
        FullMasterOutput output;
        try
        {
            output = JsonSerializer.Deserialize<FullMasterOutput>(json, SerializerOptions)
                ?? throw Invalid("The Full Master response was empty.");
        }
        catch (JsonException exception)
        {
            throw Invalid("The Full Master response did not match the required schema.", exception);
        }

        if (output.Conflicts is null
            || output.KeyEvidenceIds is null
            || output.SupportingWorkspaces is null
            || output.Invalidation is null)
        {
            throw Invalid("The Full Master response omitted a required field.");
        }

        ValidateRatio(output.Confidence, "confidence");
        ValidateRatio(output.Agreement, "agreement");
        ValidateText(output.Reasoning, "reasoning");
        ValidateText(output.Uncertainty, "uncertainty");
        ValidateText(output.Invalidation.Summary, "invalidation summary");
        ValidateEvidence(output.KeyEvidenceIds, allowedEvidenceIds);

        if (output.SupportingWorkspaces.Any(workspace => workspace is FullWorkspace.Master or FullWorkspace.Risk))
        {
            throw Invalid("Master and Risk are not independent supporting specialist workspaces.");
        }

        if (output.Decision == FullDecision.Wait)
        {
            if (output.ValidUntilUtc.HasValue
                || output.Invalidation.Price.HasValue
                || output.Invalidation.Condition != FullInvalidationCondition.None)
            {
                throw Invalid("WAIT cannot include an active validity window or price invalidation trigger.");
            }

            return output;
        }

        if (output.KeyEvidenceIds.Count == 0 || output.SupportingWorkspaces.Distinct().Count() < 2)
        {
            throw Invalid("BUY or SELL requires cited evidence and at least two independent supporting workspaces.");
        }

        if (!output.ValidUntilUtc.HasValue || output.Invalidation.Price is not > 0m)
        {
            throw Invalid("BUY or SELL requires a validity deadline and a positive invalidation price.");
        }

        var expectedCondition = output.Decision == FullDecision.Buy
            ? FullInvalidationCondition.AtOrBelow
            : FullInvalidationCondition.AtOrAbove;
        if (output.Invalidation.Condition != expectedCondition)
        {
            throw Invalid("The invalidation condition does not match the Full Master direction.");
        }

        return output;
    }

    private static void ValidateRatio(decimal value, string field)
    {
        if (value is < 0m or > 1m)
        {
            throw Invalid($"Full AI {field} must be between zero and one.");
        }
    }

    private static void ValidateText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4_000)
        {
            throw Invalid($"The Full AI {field} is missing or too long.");
        }
    }

    private static void ValidateEvidence(
        IEnumerable<Guid> evidenceIds,
        IReadOnlyCollection<Guid> allowedEvidenceIds)
    {
        var allowed = allowedEvidenceIds.ToHashSet();
        if (evidenceIds.Any(id => id == Guid.Empty || !allowed.Contains(id)))
        {
            throw Invalid("The Full AI response cited evidence outside the immutable snapshot.");
        }
    }

    private static FullAnalysisException Invalid(string message, Exception? inner = null) => new(
        FullAnalysisErrorCodes.InvalidResponse,
        message,
        inner);
}
