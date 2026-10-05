using System.Security.Cryptography;
using System.Text;

namespace XauAi.Application.AI;

internal sealed class AiEvidenceSelector : IAiEvidenceSelector
{
    public SelectedAiEvidence Select(
        IReadOnlyList<AiEvidenceCandidate> candidates,
        AiEvidenceSelectionQuery query,
        int maximumItems)
    {
        var eligible = candidates
            .Where(item => item.IsRelevant
                && string.Equals(item.Instrument, query.Instrument, StringComparison.Ordinal)
                && item.AvailableAtUtc <= query.AnalysisTimeUtc
                && item.AvailableAtUtc >= query.FromUtc
                && (!item.ValidFromUtc.HasValue || item.ValidFromUtc <= query.AnalysisTimeUtc)
                && (!item.ValidToUtc.HasValue || item.ValidToUtc > query.AnalysisTimeUtc)
                && IsEvidenceTypeRelevant(item.EvidenceType, query.Specialist)
                && IsInterpretationTypeRelevant(item.EvidenceType, query.InterpretationType)
                && IsTimeframeRelevant(item, query))
            .OrderByDescending(item => ImportanceRank(item.Importance))
            .ThenByDescending(item => QualityRank(item.Quality))
            .ThenByDescending(item => item.AvailableAtUtc)
            .ThenBy(item => item.Id)
            .ToArray();

        var selected = new List<AiEvidenceCandidate>(Math.Min(maximumItems, eligible.Length));
        var deduplicationKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in eligible)
        {
            var key = item.ClusterId.HasValue
                ? $"cluster:{item.ClusterId.Value:D}"
                : !string.IsNullOrWhiteSpace(item.ContentHash)
                    ? $"content:{item.ContentHash}"
                    : $"evidence:{item.Id:D}";
            if (!deduplicationKeys.Add(key))
            {
                continue;
            }

            selected.Add(item);
            if (selected.Count >= maximumItems)
            {
                break;
            }
        }

        var conflicts = DetectConflicts(selected);
        var versionMaterial = string.Join('|', selected
            .OrderBy(item => item.Id)
            .Select(item => $"{item.Id:D}:{item.ContentHash}:{item.UpdatedAtUtc.ToUniversalTime():O}"));
        var evidenceVersion = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(versionMaterial))).ToLowerInvariant();
        var updatedAt = selected.Count == 0
            ? query.AnalysisTimeUtc
            : selected.Max(item => item.UpdatedAtUtc);

        return new SelectedAiEvidence(selected, conflicts, evidenceVersion, updatedAt);
    }

    private static bool IsEvidenceTypeRelevant(string evidenceType, AiSpecialist specialist) => specialist switch
    {
        AiSpecialist.News => evidenceType is "News" or "Economic" or "Analyst" or "Market",
        AiSpecialist.Candle => evidenceType is "Market" or "Candle" or "Technical",
        AiSpecialist.Structure => evidenceType is "Market" or "Technical" or "Session",
        AiSpecialist.Liquidity => evidenceType is "Market" or "Technical" or "Liquidity",
        AiSpecialist.Flow => evidenceType is "Market" or "CandleFlow" or "OrderFlow",
        AiSpecialist.Ktr => evidenceType is "Market" or "Technical",
        AiSpecialist.Risk => evidenceType is "News" or "Economic" or "Analyst" or "Market" or "Technical" or "Liquidity",
        AiSpecialist.Master => true,
        _ => false
    };

    private static bool IsTimeframeRelevant(AiEvidenceCandidate item, AiEvidenceSelectionQuery query)
    {
        if (query.Timeframe is null || item.Timeframe is null)
        {
            return true;
        }

        return query.Specialist is AiSpecialist.News or AiSpecialist.Risk or AiSpecialist.Master
            || AiTimeframeContext.IsRelevant(query.Timeframe, item.Timeframe);
    }

    private static bool IsInterpretationTypeRelevant(
        string evidenceType,
        AiInterpretationType interpretationType) => interpretationType switch
    {
        AiInterpretationType.NewsEvent => evidenceType is "News" or "Economic" or "Market" or "Technical",
        AiInterpretationType.MacroData => evidenceType is "Economic" or "News" or "Market" or "Technical",
        AiInterpretationType.AnalystClaim => evidenceType is "Analyst" or "Market" or "Technical",
        AiInterpretationType.TechnicalEvidence => evidenceType is "Market" or "Technical" or "Candle"
            or "CandleFlow" or "Liquidity" or "OrderFlow" or "Session",
        AiInterpretationType.GeopoliticalRisk => evidenceType is "News" or "Market" or "Technical",
        AiInterpretationType.EvidenceSynthesis => true,
        _ => false
    };

    private static IReadOnlyList<AiEvidenceConflict> DetectConflicts(IReadOnlyList<AiEvidenceCandidate> items)
    {
        var conflicts = new Dictionary<(Guid, Guid), AiEvidenceConflict>();
        var selectedIds = items.Select(item => item.Id).ToHashSet();
        foreach (var item in items)
        {
            foreach (var relation in item.Relations.Where(relation =>
                         string.Equals(relation.RelationType, "Contradicts", StringComparison.OrdinalIgnoreCase)))
            {
                var relatedId = relation.EvidenceId == item.Id
                    ? relation.RelatedEvidenceId
                    : relation.EvidenceId;
                if (!selectedIds.Contains(relatedId))
                {
                    continue;
                }

                var key = Order(item.Id, relatedId);
                conflicts.TryAdd(key, new AiEvidenceConflict(
                    key.Item1,
                    key.Item2,
                    "The normalized evidence relationship marks these records as contradictory."));
            }
        }

        foreach (var group in items
                     .Where(item => item.Direction is "Bullish" or "Bearish")
                     .GroupBy(item => new { item.Category, item.Timeframe }))
        {
            foreach (var bullish in group.Where(item => item.Direction == "Bullish"))
            {
                foreach (var bearish in group.Where(item => item.Direction == "Bearish"
                             && item.SourceKey != bullish.SourceKey
                             && Math.Abs((item.AvailableAtUtc - bullish.AvailableAtUtc).TotalHours) <= 24))
                {
                    var key = Order(bullish.Id, bearish.Id);
                    conflicts.TryAdd(key, new AiEvidenceConflict(
                        key.Item1,
                        key.Item2,
                        "Independent evidence has opposing normalized directions; no majority decision was applied."));
                }
            }
        }

        return [.. conflicts.Values.OrderBy(conflict => conflict.FirstEvidenceId).ThenBy(conflict => conflict.SecondEvidenceId)];
    }

    private static (Guid, Guid) Order(Guid first, Guid second) =>
        first.CompareTo(second) <= 0 ? (first, second) : (second, first);

    private static int ImportanceRank(string value) => value switch
    {
        "Critical" => 4,
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0
    };

    private static int QualityRank(string value) => value switch
    {
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0
    };
}
