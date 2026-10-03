using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Backtesting;
using XauAi.Domain.ReferenceData;
using XauAi.Domain.Statistics;
using XauAi.Domain.Strategies;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class PerformanceStatisticConfiguration : IEntityTypeConfiguration<PerformanceStatistic>
{
    public void Configure(EntityTypeBuilder<PerformanceStatistic> builder)
    {
        builder.ToTable("PerformanceStatistics", table =>
        {
            table.HasCheckConstraint("CK_PerformanceStatistics_DateRange", "[RangeEndUtc] > [RangeStartUtc]");
            table.HasCheckConstraint("CK_PerformanceStatistics_SampleSize", "[SampleSize] >= 0");
            table.HasCheckConstraint("CK_PerformanceStatistics_WinRate", "[WinRate] IS NULL OR [WinRate] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_PerformanceStatistics_LossRate", "[LossRate] IS NULL OR [LossRate] BETWEEN 0 AND 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Scope).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.MarketRegime).HasMaxLength(100);
        builder.Property(entity => entity.WinRate).HasPrecision(9, 6);
        builder.Property(entity => entity.LossRate).HasPrecision(9, 6);
        builder.Property(entity => entity.Expectancy).HasPrecision(19, 8);
        builder.Property(entity => entity.ProfitFactor).HasPrecision(19, 8);
        builder.Property(entity => entity.MaximumDrawdown).HasPrecision(19, 8);
        builder.Property(entity => entity.AverageR).HasPrecision(19, 8);
        builder.Property(entity => entity.AverageFavorableExcursion).HasPrecision(19, 8);
        builder.Property(entity => entity.AverageAdverseExcursion).HasPrecision(19, 8);
        builder.Property(entity => entity.CalculationVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.RangeStartUtc).IsUtcTimestamp();
        builder.Property(entity => entity.RangeEndUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CalculatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<StrategyVersion>().WithMany().HasForeignKey(entity => entity.StrategyVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BacktestRun>().WithMany().HasForeignKey(entity => entity.BacktestRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.StrategyVersionId, entity.CalculatedAtUtc })
            .HasDatabaseName("IX_PerformanceStatistics_StrategyVersion_CalculatedAtUtc");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.TimeframeId, entity.MarketRegime, entity.CalculatedAtUtc })
            .HasDatabaseName("IX_PerformanceStatistics_Dimensions_CalculatedAtUtc");
    }
}
