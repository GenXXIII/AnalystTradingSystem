using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Backtesting;
using XauAi.Domain.ReferenceData;
using XauAi.Domain.Strategies;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class BacktestRunConfiguration : IEntityTypeConfiguration<BacktestRun>
{
    public void Configure(EntityTypeBuilder<BacktestRun> builder)
    {
        builder.ToTable("BacktestRuns", table =>
        {
            table.HasCheckConstraint("CK_BacktestRuns_ParametersJson", "ISJSON([ParametersJson]) = 1");
            table.HasCheckConstraint("CK_BacktestRuns_ResultsJson", "[ResultsJson] IS NULL OR ISJSON([ResultsJson]) = 1");
            table.HasCheckConstraint("CK_BacktestRuns_DateRange", "[RangeEndUtc] > [RangeStartUtc]");
            table.HasCheckConstraint("CK_BacktestRuns_StartingCapital", "[StartingCapital] >= 0");
            table.HasCheckConstraint("CK_BacktestRuns_TradeCounts", "([TradeCount] IS NULL OR [TradeCount] >= 0) AND ([WinningTrades] IS NULL OR [WinningTrades] >= 0) AND ([LosingTrades] IS NULL OR [LosingTrades] >= 0)");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.EngineVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ParametersJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ParametersHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.StartingCapital).HasPrecision(19, 8);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.GrossProfit).HasPrecision(19, 8);
        builder.Property(entity => entity.GrossLoss).HasPrecision(19, 8);
        builder.Property(entity => entity.MaximumDrawdown).HasPrecision(19, 8);
        builder.Property(entity => entity.WinRate).HasPrecision(9, 6);
        builder.Property(entity => entity.Expectancy).HasPrecision(19, 8);
        builder.Property(entity => entity.ResultsJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(4000);
        builder.Property(entity => entity.RangeStartUtc).IsUtcTimestamp();
        builder.Property(entity => entity.RangeEndUtc).IsUtcTimestamp();
        builder.Property(entity => entity.StartedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CompletedAtUtc).IsUtcTimestamp();
        builder.HasOne<StrategyVersion>().WithMany().HasForeignKey(entity => entity.StrategyVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.StrategyVersionId, entity.StartedAtUtc })
            .HasDatabaseName("IX_BacktestRuns_StrategyVersion_StartedAtUtc");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.TimeframeId, entity.RangeStartUtc, entity.RangeEndUtc })
            .HasDatabaseName("IX_BacktestRuns_Market_DateRange");
        builder.HasIndex(entity => new
        {
            entity.StrategyVersionId,
            entity.InstrumentId,
            entity.TimeframeId,
            entity.RangeStartUtc,
            entity.RangeEndUtc,
            entity.ParametersHash,
            entity.EngineVersion
        })
            .HasDatabaseName("IX_BacktestRuns_Reproducibility");
    }
}

internal sealed class BacktestTradeConfiguration : IEntityTypeConfiguration<BacktestTrade>
{
    public void Configure(EntityTypeBuilder<BacktestTrade> builder)
    {
        builder.ToTable("BacktestTrades", table =>
            table.HasCheckConstraint("CK_BacktestTrades_AuditJson", "[AuditJson] IS NULL OR ISJSON([AuditJson]) = 1"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Direction).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.EntryPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.ExitPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.ProfitLossAmount).HasPrecision(19, 8);
        builder.Property(entity => entity.ProfitLossR).HasPrecision(19, 8);
        builder.Property(entity => entity.AuditJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.EnteredAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ExitedAtUtc).IsUtcTimestamp();
        builder.HasOne<BacktestRun>().WithMany().HasForeignKey(entity => entity.BacktestRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.BacktestRunId, entity.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("UX_BacktestTrades_Run_Sequence");
        builder.HasIndex(entity => new { entity.BacktestRunId, entity.EnteredAtUtc })
            .HasDatabaseName("IX_BacktestTrades_Run_EnteredAtUtc");
    }
}
