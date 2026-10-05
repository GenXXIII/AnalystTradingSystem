using System.Text.Json;
using System.Text.Json.Serialization;

namespace XauAi.Application.AI;

internal sealed class AiEvidenceCompressor : IAiEvidenceCompressor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public CompressedAiEvidence Compress(
        SelectedAiEvidence selection,
        AiEvidenceSelectionQuery query,
        int maximumCharacters)
    {
        var items = new List<object>(selection.Items.Count);
        foreach (var item in selection.Items)
        {
            var relatedIds = item.Relations
                .Where(relation => relation.EvidenceId == item.Id || relation.RelatedEvidenceId == item.Id)
                .Select(relation => relation.EvidenceId == item.Id
                    ? relation.RelatedEvidenceId
                    : relation.EvidenceId)
                .Distinct()
                .Order()
                .ToArray();
            var compressed = new
            {
                item.Id,
                item.EvidenceType,
                item.SourceType,
                item.SourceKey,
                item.ExternalId,
                item.Instrument,
                item.Timeframe,
                TimeframeRole = AiTimeframeContext.Role(query.Timeframe, item.Timeframe),
                item.EventTimeUtc,
                item.AvailableAtUtc,
                item.PublishedAtUtc,
                Title = Limit(item.Title, 500),
                Summary = Limit(item.Summary, 1_000),
                item.NumericValue,
                item.OriginalValue,
                item.Unit,
                item.Direction,
                item.Importance,
                item.Category,
                item.Quality,
                item.Completeness,
                item.OriginalSourceUrl,
                Metadata = ParseMetadata(item.MetadataJson),
                item.ClusterId,
                RelatedEvidenceIds = relatedIds
            };

            items.Add(compressed);
            var candidate = Serialize(query, selection, items);
            if (candidate.Length > maximumCharacters)
            {
                items.RemoveAt(items.Count - 1);
                break;
            }
        }

        if (items.Count == 0 && selection.Items.Count > 0)
        {
            var item = selection.Items[0];
            items.Add(new
            {
                item.Id,
                item.EvidenceType,
                item.SourceType,
                item.SourceKey,
                item.Instrument,
                item.Timeframe,
                item.EventTimeUtc,
                item.AvailableAtUtc,
                Title = Limit(item.Title, 200),
                Summary = Limit(item.Summary, Math.Max(100, maximumCharacters / 4)),
                item.NumericValue,
                item.Unit,
                item.Direction,
                item.Importance,
                item.Category
            });
        }

        var json = Serialize(query, selection, items);
        var includedIds = selection.Items.Take(items.Count).Select(item => item.Id).ToArray();
        return new CompressedAiEvidence(
            json,
            includedIds,
            selection.Items.Count,
            items.Count,
            json.Length);
    }

    private static string Serialize(
        AiEvidenceSelectionQuery query,
        SelectedAiEvidence selection,
        IReadOnlyList<object> items) =>
        JsonSerializer.Serialize(new
        {
            context = new
            {
                query.Instrument,
                query.Timeframe,
                query.AnalysisTimeUtc,
                query.Specialist,
                query.InterpretationType,
                selection.EvidenceVersion
            },
            evidence = items,
            conflicts = selection.Conflicts.Where(conflict =>
                items.Any(item => HasEvidenceId(item, conflict.FirstEvidenceId))
                && items.Any(item => HasEvidenceId(item, conflict.SecondEvidenceId)))
        }, SerializerOptions);

    private static bool HasEvidenceId(object item, Guid id)
    {
        var property = item.GetType().GetProperty("Id");
        return property?.GetValue(item) is Guid value && value == id;
    }

    private static JsonElement? ParseMetadata(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static string? Limit(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) || value.Length <= maximumLength
            ? value
            : value[..maximumLength];
}
