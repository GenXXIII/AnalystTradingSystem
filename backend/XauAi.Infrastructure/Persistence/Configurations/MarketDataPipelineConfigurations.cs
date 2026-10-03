using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Market;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class MarketDataSyncStateConfiguration : IEntityTypeConfiguration<MarketDataSyncState>
{
    public void Configure(EntityTypeBuilder<MarketDataSyncState> builder)
    {
        builder.ToTable("MarketDataSyncStates", table =>
        {
            table.HasCheckConstraint("CK_MarketDataSyncStates_Failures", "[ConsecutiveFailures] >= 0");
            table.HasCheckConstraint("CK_MarketDataSyncStates_Gaps", "[DetectedGapCount] >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.LastErrorCode).HasMaxLength(64).IsUnicode(false);
        builder.Property(entity => entity.LastErrorMessage).HasMaxLength(512);
        builder.Property(entity => entity.LastAttemptAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastSuccessfulSyncAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastRequestedFromUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastRequestedToUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastStoredCandleOpenTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();

        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.InstrumentId, entity.TimeframeId, entity.DataProviderId })
            .IsUnique()
            .HasDatabaseName("UX_MarketDataSyncStates_Instrument_Timeframe_Provider");
    }
}

internal sealed class MarketDataSyncRunConfiguration : IEntityTypeConfiguration<MarketDataSyncRun>
{
    public void Configure(EntityTypeBuilder<MarketDataSyncRun> builder)
    {
        builder.ToTable("MarketDataSyncRuns", table =>
        {
            table.HasCheckConstraint("CK_MarketDataSyncRuns_TimeRange", "[RequestedToUtc] > [RequestedFromUtc]");
            table.HasCheckConstraint(
                "CK_MarketDataSyncRuns_Counts",
                "[BatchesProcessed] >= 0 AND [RecordsReceived] >= 0 AND [RecordsAccepted] >= 0 AND [RecordsInserted] >= 0 AND [RecordsUpdated] >= 0 AND [RecordsSkipped] >= 0 AND [RecordsRejected] >= 0 AND [DetectedGapCount] >= 0 AND [DurationMilliseconds] >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(64).IsUnicode(false);
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(512);
        builder.Property(entity => entity.StartedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CompletedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.RequestedFromUtc).IsUtcTimestamp();
        builder.Property(entity => entity.RequestedToUtc).IsUtcTimestamp();

        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.InstrumentId, entity.TimeframeId, entity.StartedAtUtc })
            .HasDatabaseName("IX_MarketDataSyncRuns_Instrument_Timeframe_StartedAtUtc");
        builder.HasIndex(entity => new { entity.Status, entity.StartedAtUtc })
            .HasDatabaseName("IX_MarketDataSyncRuns_Status_StartedAtUtc");
    }
}
