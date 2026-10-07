using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.AI;
using XauAi.Application.Evidence;
using XauAi.Infrastructure.AI.Persistence;
using XauAi.Infrastructure.Evidence.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class AiInterpretationSqlTests
{
    [SqlServerFact]
    public async Task Interpretations_persist_trace_evidence_and_preserve_superseded_and_stale_history()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var databaseName = $"XauAiInterpretationTests_{Guid.NewGuid():N}";
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection) { InitialCatalog = databaseName };
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;
        var analysisTime = DateTimeOffset.Parse("2026-10-04T08:00:00Z");
        var evidenceSettings = new EvidenceSettings();

        try
        {
            Guid evidenceId;
            await using (var context = new XauAiDbContext(options))
            {
                await context.Database.MigrateAsync();
                var ingestion = new EvidenceIngestionService(
                    new EvidenceNormalizer(),
                    new EfEvidenceStore(context, evidenceSettings),
                    evidenceSettings,
                    new FixedTimeProvider(analysisTime),
                    NullLogger<EvidenceIngestionService>.Instance);
                var result = await ingestion.IngestAsync([new EvidenceInput
                {
                    EvidenceType = EvidenceType.News,
                    SourceType = EvidenceSourceType.Manual,
                    SourceKey = "phase11-test",
                    ExternalId = "news-1",
                    Instrument = "XAUUSD",
                    EventTime = analysisTime.AddMinutes(-5),
                    AvailableAt = analysisTime.AddMinutes(-4),
                    PublishedAt = analysisTime.AddMinutes(-5),
                    Title = "Test event",
                    Summary = "Test-only evidence.",
                    Category = "Fed",
                    SourceReliability = EvidenceSourceReliability.Known
                }]);
                Assert.Equal(1, result.Inserted);
                evidenceId = await context.EvidenceRecords.Select(item => item.Id).SingleAsync();
            }

            Guid firstId;
            Guid secondId;
            await using (var context = new XauAiDbContext(options))
            {
                var store = new EfAiInterpretationStore(context);
                firstId = Guid.NewGuid();
                var first = await store.SaveCompletedAsync(Write(
                    firstId, evidenceId, analysisTime, "cache-1", "evidence-v1"));
                Assert.Equal(AiInterpretationLifecycle.Current, first.Lifecycle);
                Assert.Equal(new string('c', 64), first.ConfigurationVersion);
                Assert.Equal([evidenceId], first.EvidenceIds);

                secondId = Guid.NewGuid();
                var second = await store.SaveCompletedAsync(Write(
                    secondId, evidenceId, analysisTime.AddMinutes(1), "cache-2", "evidence-v2"));
                Assert.Equal(AiInterpretationLifecycle.Current, second.Lifecycle);
            }

            await using (var restarted = new XauAiDbContext(options))
            {
                var store = new EfAiInterpretationStore(restarted);
                var first = await store.GetAsync(firstId);
                var second = await store.GetAsync(secondId);
                Assert.Equal(AiInterpretationLifecycle.Superseded, first?.Lifecycle);
                Assert.Equal(AiInterpretationLifecycle.Current, second?.Lifecycle);
                Assert.Equal(new string('c', 64), second?.ConfigurationVersion);
                Assert.Equal([evidenceId], second?.EvidenceIds);

                var evidence = await restarted.EvidenceRecords.SingleAsync(item => item.Id == evidenceId);
                evidence.UpdatedAtUtc = analysisTime.AddMinutes(2);
                await restarted.SaveChangesAsync();
                await store.MarkChangedInterpretationsStaleAsync("XAUUSD", analysisTime.AddMinutes(3));
            }

            await using (var finalContext = new XauAiDbContext(options))
            {
                var store = new EfAiInterpretationStore(finalContext);
                var second = await store.GetAsync(secondId);
                Assert.Equal(AiInterpretationLifecycle.Stale, second?.Lifecycle);
                var history = await store.QueryAsync(new AiInterpretationQuery(
                    "XAUUSD", null, null, null, null, null, null, true, 1, 20));
                Assert.Equal(2, history.TotalItems);
                Assert.Contains(history.Items, item => item.Lifecycle == AiInterpretationLifecycle.Superseded);
                Assert.Contains(history.Items, item => item.Lifecycle == AiInterpretationLifecycle.Stale);
            }
        }
        finally
        {
            await using var cleanup = new XauAiDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static AiInterpretationWriteModel Write(
        Guid id,
        Guid evidenceId,
        DateTimeOffset analysisTime,
        string cacheSeed,
        string evidenceSeed)
    {
        var output = new StructuredAiInterpretation
        {
            InterpretationType = AiInterpretationType.NewsEvent,
            Direction = AiInterpretationDirection.Mixed,
            Impact = AiInterpretationImpact.High,
            AffectedAssets = ["XAUUSD", "USD"],
            Mechanism = "The event may affect USD demand.",
            ExpectedEffect = "A stronger USD may pressure gold.",
            ObservedReaction = "The observed reaction is mixed.",
            ReactionAlignment = AiReactionAlignment.Mixed,
            CurrentRelevance = AiCurrentRelevance.High,
            Confidence = 0.7m,
            Uncertainty = "Persistence remains unknown.",
            Summary = "Mixed evidence requires confirmation.",
            EvidenceIds = [evidenceId],
            Facts = [new AiInterpretationStatement("The event was published.", [evidenceId])],
            Interpretations = [new AiInterpretationStatement("The effect may be temporary.", [evidenceId])],
            Unknowns = ["Persistence is unknown."],
            Conflicts = [],
            Measurements = []
        };
        return new AiInterpretationWriteModel(
            id,
            "XAUUSD",
            null,
            AiSpecialist.News,
            output,
            "ProviderA",
            "model-a",
            "phase11-v1",
            new string('c', 64),
            evidenceSeed.PadRight(64, '0'),
            cacheSeed.PadRight(64, '0'),
            "input".PadRight(64, '0'),
            JsonSerializer.Serialize(output, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            }),
            analysisTime,
            analysisTime,
            analysisTime,
            analysisTime.AddSeconds(1),
            100,
            50,
            20,
            [evidenceId]);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
