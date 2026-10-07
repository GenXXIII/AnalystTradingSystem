using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.MarketData;
using XauAi.Application.TargetAnalysis;
using XauAi.Infrastructure.Persistence;
using XauAi.Infrastructure.TargetAnalysis.Persistence;

namespace XauAi.IntegrationTests;

public sealed class TargetAnalystSqlTests
{
    [SqlServerFact]
    public async Task Active_target_can_be_cancelled_without_deleting_snapshot_specialists_or_history()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var builder = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = $"XauAiTargetTests_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .Options;

        try
        {
            await using var context = new XauAiDbContext(options);
            await context.Database.MigrateAsync();
            var store = new EfTargetAnalysisStore(context);
            var now = new DateTimeOffset(2026, 10, 7, 6, 0, 0, TimeSpan.Zero);
            var id = Guid.NewGuid();
            await store.CreateJobAsync(new TargetAnalysisJobWriteModel(
                id,
                "XAUUSD",
                MarketTimeframe.M5,
                now,
                "phase13-master-v1",
                "phase13-v1",
                now));
            await store.UpdateSnapshotAsync(new TargetSnapshotWriteModel(
                id,
                2400m,
                JsonSerializer.Serialize(new TargetAnalysisSnapshot(
                    "XAUUSD",
                    2400m,
                    now,
                    MarketTimeframe.M5,
                    [MarketTimeframe.M5, MarketTimeframe.H1, MarketTimeframe.H4],
                    "BullishAligned",
                    "MomentumCandle",
                    new Dictionary<string, string>(),
                    [],
                    new Dictionary<string, string>(),
                    new Dictionary<TargetWorkspace, string>(),
                    new Dictionary<TargetWorkspace, string>(),
                    [],
                    [])),
                [],
                now));

            var master = new TargetMasterOutput
            {
                ValidTarget = true,
                TargetPrice = 2420m,
                InvalidationPrice = 2380m,
                DirectionContext = TargetDirectionContext.Upward,
                Confidence = 0.8m,
                ValidUntilUtc = now.AddHours(4),
                ReasoningSummary = "Independent evidence supports one target.",
                Uncertainty = "Moderate.",
                EvidenceIds = [],
                Conflicts = [],
                SupportingWorkspaces = [TargetWorkspace.Structure, TargetWorkspace.Liquidity],
                NoTargetReason = null
            };
            var configuration = Configuration(TargetWorkspace.Master);
            var masterRun = new TargetWorkspaceRunResult(
                Guid.NewGuid(),
                TargetWorkspace.Master,
                TargetWorkspaceExecutionStatus.Completed,
                null,
                master,
                JsonSerializer.Serialize(master),
                configuration,
                100,
                50,
                200,
                null,
                null,
                now,
                now.AddSeconds(1));
            var active = await store.CompleteAsync(new TargetAnalysisCompletionWriteModel(
                id,
                master,
                [masterRun],
                configuration.Provider,
                configuration.Model,
                configuration.PromptVersion,
                configuration.ConfigurationVersion,
                now.AddSeconds(1)));

            Assert.Equal(TargetAnalysisStatus.Active, active.Status);
            Assert.Single(active.SpecialistResults);
            Assert.NotNull(active.Snapshot);

            var changed = await store.TryTransitionAsync(new TargetLifecycleTransition(
                id,
                TargetAnalysisStatus.Active,
                TargetAnalysisStatus.Cancelled,
                "CANCELLED",
                "Cancelled by the user.",
                2401m,
                now.AddMinutes(1)));
            var cancelled = await store.GetAsync(id);
            var lifecycle = await store.GetLifecycleAsync(id);

            Assert.True(changed);
            Assert.NotNull(cancelled);
            Assert.Equal(TargetAnalysisStatus.Cancelled, cancelled.Status);
            Assert.Single(cancelled.SpecialistResults);
            Assert.NotNull(cancelled.Snapshot);
            Assert.Equal(
                [TargetAnalysisStatus.Analyzing, TargetAnalysisStatus.Success, TargetAnalysisStatus.Active, TargetAnalysisStatus.Cancelled],
                lifecycle.Select(item => item.Status).ToArray());
        }
        finally
        {
            await using var cleanup = new XauAiDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static TargetWorkspaceConfiguration Configuration(TargetWorkspace workspace) => new(
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
        1000,
        0,
        0,
        $"phase13-{workspace.ToString().ToLowerInvariant()}-v1",
        "phase13-v1");
}
