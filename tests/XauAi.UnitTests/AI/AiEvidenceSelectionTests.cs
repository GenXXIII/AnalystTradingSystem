using System.Text.Json;
using XauAi.Application.AI;

namespace XauAi.UnitTests.AI;

public sealed class AiEvidenceSelectionTests
{
    private static readonly DateTimeOffset AnalysisTime = new(2026, 10, 4, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Selection_rejects_future_irrelevant_wrong_symbol_and_wrong_timeframe_evidence()
    {
        var selector = new AiEvidenceSelector();
        var eligible = Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M15", AnalysisTime.AddMinutes(-1));
        var candidates = new[]
        {
            eligible,
            Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M15", AnalysisTime.AddMinutes(1)),
            Candidate(Guid.NewGuid(), "Technical", "EURUSD", "M15", AnalysisTime.AddMinutes(-1)),
            Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M1", AnalysisTime.AddMinutes(-1)),
            Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M15", AnalysisTime.AddMinutes(-1)) with { IsRelevant = false }
        };

        var result = selector.Select(candidates, Query(AiSpecialist.Structure, "M15"), 20);

        Assert.Equal([eligible.Id], result.Items.Select(item => item.Id));
        Assert.All(result.Items, item => Assert.True(item.AvailableAtUtc <= AnalysisTime));
    }

    [Fact]
    public void Technical_selection_keeps_higher_context_and_immediate_lower_confirmation()
    {
        var selector = new AiEvidenceSelector();
        var primary = Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M15", AnalysisTime.AddMinutes(-1));
        var confirmation = Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M5", AnalysisTime.AddMinutes(-2));
        var context = Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "H1", AnalysisTime.AddMinutes(-3));
        var fineTiming = Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "M1", AnalysisTime.AddMinutes(-4));
        var query = Query(AiSpecialist.Structure, "M15") with
        {
            InterpretationType = AiInterpretationType.TechnicalEvidence
        };

        var result = selector.Select([primary, confirmation, context, fineTiming], query, 20);
        var compressed = new AiEvidenceCompressor().Compress(result, query, 20_000);

        Assert.Equal(3, result.Items.Count);
        Assert.Contains(result.Items, item => item.Id == primary.Id);
        Assert.Contains(result.Items, item => item.Id == confirmation.Id);
        Assert.Contains(result.Items, item => item.Id == context.Id);
        Assert.DoesNotContain(result.Items, item => item.Id == fineTiming.Id);
        Assert.Contains("\"timeframeRole\":\"Primary\"", compressed.Json, StringComparison.Ordinal);
        Assert.Contains("\"timeframeRole\":\"Confirmation\"", compressed.Json, StringComparison.Ordinal);
        Assert.Contains("\"timeframeRole\":\"Context\"", compressed.Json, StringComparison.Ordinal);
    }

    [Fact]
    public void Selection_deduplicates_reposts_without_turning_repetition_into_confidence()
    {
        var selector = new AiEvidenceSelector();
        var contentHash = new string('a', 64);
        var original = Candidate(Guid.NewGuid(), "News", "XAUUSD", null, AnalysisTime.AddMinutes(-2)) with
        {
            ContentHash = contentHash,
            Importance = "High"
        };
        var repost = Candidate(Guid.NewGuid(), "News", "XAUUSD", null, AnalysisTime.AddMinutes(-1)) with
        {
            ContentHash = contentHash,
            SourceKey = "repost-provider"
        };

        var result = selector.Select([original, repost], Query(AiSpecialist.News, null), 20);

        Assert.Single(result.Items);
        Assert.Equal(original.Id, result.Items[0].Id);
    }

    [Fact]
    public void Selection_preserves_opposing_evidence_as_a_conflict()
    {
        var selector = new AiEvidenceSelector();
        var bullish = Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "H1", AnalysisTime.AddMinutes(-2)) with
        {
            Direction = "Bullish",
            Category = "MarketStructure",
            SourceKey = "technical-a"
        };
        var bearish = Candidate(Guid.NewGuid(), "Technical", "XAUUSD", "H1", AnalysisTime.AddMinutes(-1)) with
        {
            Direction = "Bearish",
            Category = "MarketStructure",
            SourceKey = "technical-b"
        };

        var result = selector.Select([bullish, bearish], Query(AiSpecialist.Master, null), 20);

        Assert.Equal(2, result.Items.Count);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Contains(conflict.FirstEvidenceId, new[] { bullish.Id, bearish.Id });
        Assert.Contains(conflict.SecondEvidenceId, new[] { bullish.Id, bearish.Id });
    }

    [Fact]
    public void Compression_preserves_timestamps_source_provenance_numbers_and_nulls()
    {
        var selector = new AiEvidenceSelector();
        var evidence = Candidate(Guid.NewGuid(), "Economic", "XAUUSD", null, AnalysisTime.AddMinutes(-1)) with
        {
            NumericValue = null,
            OriginalValue = null,
            MetadataJson = "{\"previous\":2.5,\"forecast\":null,\"actual\":null,\"revision\":null}",
            OriginalSourceUrl = "https://example.test/event"
        };
        var query = Query(AiSpecialist.News, null);
        var selection = selector.Select([evidence], query, 20);

        var compressed = new AiEvidenceCompressor().Compress(selection, query, 10_000);
        using var document = JsonDocument.Parse(compressed.Json);
        var item = document.RootElement.GetProperty("evidence")[0];

        Assert.Equal(evidence.Id, item.GetProperty("id").GetGuid());
        Assert.Equal("source-a", item.GetProperty("sourceKey").GetString());
        Assert.Equal(evidence.AvailableAtUtc, item.GetProperty("availableAtUtc").GetDateTimeOffset());
        Assert.Equal("https://example.test/event", item.GetProperty("originalSourceUrl").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("numericValue").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("metadata").GetProperty("forecast").ValueKind);
    }

    private static AiEvidenceSelectionQuery Query(AiSpecialist specialist, string? timeframe) => new(
        "XAUUSD",
        specialist,
        AiInterpretationType.EvidenceSynthesis,
        timeframe,
        AnalysisTime.AddDays(-1),
        AnalysisTime,
        100);

    internal static AiEvidenceCandidate Candidate(
        Guid id,
        string type,
        string instrument,
        string? timeframe,
        DateTimeOffset availableAt) => new(
        id,
        type,
        "InternalTechnicalEngine",
        "source-a",
        id.ToString("N"),
        id.ToString("N").PadRight(64, '0'),
        instrument,
        timeframe,
        availableAt.AddMinutes(-1),
        availableAt,
        availableAt.AddMinutes(-1),
        availableAt,
        null,
        "Evidence title",
        "Evidence summary",
        42.5m,
        "42.5",
        "Price",
        "Unknown",
        "Medium",
        "Technical",
        "High",
        "Complete",
        true,
        "https://example.test/source",
        "{}",
        availableAt,
        null,
        []);
}
