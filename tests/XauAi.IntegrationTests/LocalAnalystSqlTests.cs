using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.LocalAnalysis.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.IntegrationTests;

public sealed class LocalAnalystSqlTests
{
    [SqlServerFact]
    public async Task Local_signal_lifecycle_reuses_origin_signal_and_preserves_audit_history()
    {
        var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
        var databaseName = $"XauAiLocalAnalystTests_{Guid.NewGuid():N}";
        var connectionBuilder = new SqlConnectionStringBuilder(serverConnection) { InitialCatalog = databaseName };
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;
        var now = DateTimeOffset.Parse("2026-10-07T08:00:00Z");

        try
        {
            LocalSignalSnapshot opened;
            await using (var context = new XauAiDbContext(options))
            {
                await context.Database.MigrateAsync();
                var store = new EfLocalSignalStore(context, new LocalAnalystSettings
                {
                    Enabled = true,
                    Timeframes = [MarketTimeframe.M5]
                });
                opened = await store.SaveAsync(new LocalSignalPersistenceRequest(
                    LocalSignalMutation.Open,
                    null,
                    "LOCAL-XAUUSD-M5-20261007080000-BUY",
                    Decision(now, LocalSignalState.Buy, null),
                    now));
                Assert.Equal(LocalSignalState.Buy, opened.State);
                Assert.Equal("LOCAL-XAUUSD-M5-20261007080000-BUY", opened.SignalId);

                var duplicateOpen = await store.SaveAsync(new LocalSignalPersistenceRequest(
                    LocalSignalMutation.Open,
                    null,
                    "LOCAL-XAUUSD-M5-20261007080500-BUY",
                    Decision(now.AddMinutes(5), LocalSignalState.Buy, null),
                    now.AddMinutes(5)));
                Assert.Equal(opened.SignalId, duplicateOpen.SignalId);
                Assert.Equal(opened.PersistenceId, duplicateOpen.PersistenceId);
                Assert.Equal(1, await context.TradingSignals.CountAsync(value => value.Source == "LocalAnalyst" && value.Status == "ACTIVE"));

                var stopped = await store.SaveAsync(new LocalSignalPersistenceRequest(
                    LocalSignalMutation.Stop,
                    opened.PersistenceId,
                    null,
                    Decision(now.AddMinutes(10), LocalSignalState.Stop, "INVALIDATION_LEVEL_BREACHED"),
                    now.AddMinutes(10)));
                Assert.Equal(LocalSignalState.Stop, stopped.State);
                Assert.Equal("STOPPED", stopped.Status);
            }

            await using (var verification = new XauAiDbContext(options))
            {
                var store = new EfLocalSignalStore(verification, new LocalAnalystSettings
                {
                    Enabled = true,
                    Timeframes = [MarketTimeframe.M5]
                });
                var history = await store.GetHistoryAsync("XAUUSD", MarketTimeframe.M5, 10);
                var lifecycle = await store.GetLifecycleAsync("LOCAL-XAUUSD-M5-20261007080000-BUY");
                var chartMarkers = await store.GetChartMarkersAsync("XAUUSD", MarketTimeframe.M5, 10);

                Assert.Single(history);
                Assert.Equal(LocalSignalState.Stop, history[0].State);
                Assert.Equal(3, lifecycle.Count);
                Assert.Equal(["OPENED", "UPDATED", "STOPPED"], lifecycle.Select(value => value.EventType));
                Assert.Equal("INVALIDATION_LEVEL_BREACHED", lifecycle[^1].Reason);
                Assert.Collection(
                    chartMarkers,
                    marker =>
                    {
                        Assert.Equal(LocalSignalState.Buy, marker.State);
                        Assert.Equal(now, marker.CandleTimeUtc);
                    },
                    marker =>
                    {
                        Assert.Equal(LocalSignalState.Stop, marker.State);
                        Assert.Equal(now.AddMinutes(10), marker.CandleTimeUtc);
                        Assert.Equal("INVALIDATION_LEVEL_BREACHED", marker.Reason);
                    });
                var checkpoint = await store.GetCheckpointAsync("XAUUSD", MarketTimeframe.M5);
                Assert.NotNull(checkpoint);
                Assert.Equal(LocalSignalState.Stop, checkpoint!.LastResult);
            }
        }
        finally
        {
            await using var cleanup = new XauAiDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static LocalSignalDecision Decision(
        DateTimeOffset time,
        LocalSignalState state,
        string? reason) =>
        new(
            "XAUUSD",
            MarketTimeframe.M5,
            $"XAUUSD-M5-{time:yyyyMMddHHmmss}",
            time,
            time.AddMinutes(5),
            state,
            2500m,
            5m,
            6m,
            0.833333m,
            "BullishStructure",
            "NoConfirmedSweep",
            "BullishContextConfirmed",
            "Bullish",
            "SupportReaction",
            "Normal",
            2497m,
            2504m,
            reason,
            time.AddHours(1),
            [new LocalSignalCondition("Trend", "Bullish", true, false, 1m, ["fixture"])]);
}
