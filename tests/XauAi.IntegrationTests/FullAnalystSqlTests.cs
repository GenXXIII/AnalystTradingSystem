using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.FullAnalysis;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.FullAnalysis.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class FullAnalystSqlTests
{
    [SqlServerFact]
    public async Task Active_analysis_can_be_cancelled_without_deleting_snapshot_specialists_or_history()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var builder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = $"XauAiFullTests_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(builder.ConnectionString, sql => sql.EnableRetryOnFailure())
            .Options;

        try
        {
            await using var context = new XauAiDbContext(options);
            await context.Database.MigrateAsync();
            var store = new EfFullAnalysisStore(context);
            var now = new DateTimeOffset(2026, 10, 8, 1, 0, 0, TimeSpan.Zero);
            var id = Guid.NewGuid();
            await store.CreateJobAsync(new FullAnalysisJobWriteModel(
                id,
                "XAUUSD",
                MarketTimeframe.M15,
                now,
                "phase14-master-v1",
                "phase14-v1",
                now));
            var snapshot = new FullAnalysisSnapshot(
                "XAUUSD",
                2400m,
                now,
                MarketTimeframe.M15,
                [MarketTimeframe.M15, MarketTimeframe.H1, MarketTimeframe.H4],
                "Closed",
                "BullishAligned",
                new Dictionary<string, string>(),
                [],
                new Dictionary<string, string>(),
                new Dictionary<FullWorkspace, string>(),
                new Dictionary<FullWorkspace, string>(),
                [],
                []);
            await store.UpdateSnapshotAsync(new FullSnapshotWriteModel(
                id,
                2400m,
                "snapshot-hash",
                JsonSerializer.Serialize(snapshot),
                now));

            var master = new FullMasterOutput
            {
                Decision = FullDecision.Buy,
                Confidence = 0.8m,
                Agreement = 0.75m,
                Conflicts = [],
                KeyEvidenceIds = [],
                Reasoning = "Independent structure and liquidity evidence support BUY.",
                Invalidation = new FullInvalidation("Break below support.", 2380m, FullInvalidationCondition.AtOrBelow),
                Uncertainty = "News can alter relevance.",
                ValidUntilUtc = now.AddHours(2),
                SupportingWorkspaces = [FullWorkspace.Structure, FullWorkspace.Liquidity]
            };
            var configuration = Configuration(FullWorkspace.Master);
            var run = new FullWorkspaceRunResult(
                Guid.NewGuid(),
                FullWorkspace.Master,
                FullWorkspaceExecutionStatus.Completed,
                null,
                master,
                JsonSerializer.Serialize(master),
                configuration,
                "input-hash",
                false,
                100,
                50,
                200,
                null,
                null,
                now,
                now.AddSeconds(1));
            var active = await store.CompleteAsync(new FullAnalysisCompletionWriteModel(
                id,
                master.Decision,
                master.Confidence,
                master.Agreement,
                master.Conflicts,
                master.KeyEvidenceIds,
                master.Reasoning,
                master.Invalidation,
                master.Uncertainty,
                master.ValidUntilUtc,
                [run],
                run.Id,
                configuration.Provider,
                configuration.Model,
                configuration.PromptVersion,
                configuration.ConfigurationVersion,
                now.AddSeconds(1)));

            Assert.Equal(FullAnalysisStatus.Active, active.Status);
            Assert.True(active.FutureAvailable);
            Assert.Single(active.WorkspaceResults);
            Assert.NotNull(active.Snapshot);

            var changed = await store.TryTransitionAsync(new FullLifecycleTransition(
                id,
                FullAnalysisStatus.Active,
                FullAnalysisStatus.Cancelled,
                "CANCELLED",
                "Cancelled by the user.",
                2401m,
                now.AddMinutes(1)));
            var cancelled = await store.GetAsync(id);
            var lifecycle = await store.GetLifecycleAsync(id);

            Assert.True(changed);
            Assert.NotNull(cancelled);
            Assert.Equal(FullAnalysisStatus.Cancelled, cancelled.Status);
            Assert.False(cancelled.FutureAvailable);
            Assert.Single(cancelled.WorkspaceResults);
            Assert.NotNull(cancelled.Snapshot);
            Assert.Equal(
                [FullAnalysisStatus.Analyzing, FullAnalysisStatus.Active, FullAnalysisStatus.Cancelled],
                lifecycle.Select(item => item.Status).ToArray());
        }
        finally
        {
            await using var cleanup = new XauAiDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static FullWorkspaceConfiguration Configuration(FullWorkspace workspace) => new(
        workspace,
        true,
        "test-provider",
        "OpenAiCompatible",
        false,
        string.Empty,
        "test-model",
        "https://example.test/v1/",
        0.1,
        30,
        1_000,
        0,
        0,
        $"phase14-{workspace.ToString().ToLowerInvariant()}-v1",
        "phase14-v1");
}
