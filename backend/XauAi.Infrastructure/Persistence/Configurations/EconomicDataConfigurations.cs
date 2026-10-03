using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.EconomicData;
using XauAi.Domain.Evidence;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class EconomicSeriesConfiguration : IEntityTypeConfiguration<EconomicSeries>
{
    public void Configure(EntityTypeBuilder<EconomicSeries> builder)
    {
        builder.ToTable("EconomicSeries");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ExternalSeriesId).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(4000);
        builder.Property(entity => entity.Units).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.SeasonalAdjustment).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.CountryCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Category).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.ObservationStartDate).HasColumnType("date");
        builder.Property(entity => entity.ObservationEndDate).HasColumnType("date");
        builder.Property(entity => entity.ProviderUpdatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.DataProviderId, entity.ExternalSeriesId })
            .IsUnique()
            .HasDatabaseName("UX_EconomicSeries_Provider_ExternalSeriesId");
        builder.HasIndex(entity => new { entity.Category, entity.IsActive })
            .HasDatabaseName("IX_EconomicSeries_Category_Active");
        builder.HasIndex(entity => new { entity.Frequency, entity.IsActive })
            .HasDatabaseName("IX_EconomicSeries_Frequency_Active");
    }
}

internal sealed class EconomicObservationConfiguration : IEntityTypeConfiguration<EconomicObservation>
{
    public void Configure(EntityTypeBuilder<EconomicObservation> builder)
    {
        builder.ToTable("EconomicObservations", table =>
            table.HasCheckConstraint(
                "CK_EconomicObservations_StatusValue",
                "([Status] = 'Missing' AND [Value] IS NULL) OR ([Status] = 'Available' AND [Value] IS NOT NULL)"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ObservationDate).HasColumnType("date");
        builder.Property(entity => entity.Value).HasPrecision(28, 8);
        builder.Property(entity => entity.OriginalValue).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.RealtimeStartDate).HasColumnType("date");
        builder.Property(entity => entity.RealtimeEndDate).HasColumnType("date");
        builder.Property(entity => entity.FetchedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<EvidenceRecord>().WithOne().HasForeignKey<EconomicObservation>(entity => entity.Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EconomicSeries>().WithMany().HasForeignKey(entity => entity.EconomicSeriesId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EconomicSeriesId, entity.ObservationDate })
            .IsUnique()
            .HasDatabaseName("UX_EconomicObservations_Series_ObservationDate");
    }
}

internal sealed class EconomicObservationRevisionConfiguration : IEntityTypeConfiguration<EconomicObservationRevision>
{
    public void Configure(EntityTypeBuilder<EconomicObservationRevision> builder)
    {
        builder.ToTable("EconomicObservationRevisions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Value).HasPrecision(28, 8);
        builder.Property(entity => entity.OriginalValue).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.RealtimeStartDate).HasColumnType("date");
        builder.Property(entity => entity.RealtimeEndDate).HasColumnType("date");
        builder.Property(entity => entity.FetchedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.SupersededAtUtc).IsUtcTimestamp();
        builder.HasOne<EconomicObservation>().WithMany().HasForeignKey(entity => entity.EconomicObservationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EconomicObservationId, entity.SupersededAtUtc })
            .HasDatabaseName("IX_EconomicObservationRevisions_Observation_SupersededAtUtc");
    }
}

internal sealed class EconomicSyncStateConfiguration : IEntityTypeConfiguration<EconomicSyncState>
{
    public void Configure(EntityTypeBuilder<EconomicSyncState> builder)
    {
        builder.ToTable("EconomicSyncStates");
        builder.HasKey(entity => entity.EconomicSeriesId);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.LastAttemptAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastSuccessfulSyncAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastObservationDate).HasColumnType("date");
        builder.Property(entity => entity.LastErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.LastErrorMessage).HasMaxLength(1000);
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<EconomicSeries>().WithOne().HasForeignKey<EconomicSyncState>(entity => entity.EconomicSeriesId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.LastSuccessfulSyncAtUtc)
            .HasDatabaseName("IX_EconomicSyncStates_LastSuccessful");
    }
}

internal sealed class EconomicSyncRunConfiguration : IEntityTypeConfiguration<EconomicSyncRun>
{
    public void Configure(EntityTypeBuilder<EconomicSyncRun> builder)
    {
        builder.ToTable("EconomicSyncRuns");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.RequestedFromDate).HasColumnType("date");
        builder.Property(entity => entity.RequestedToDate).HasColumnType("date");
        builder.Property(entity => entity.StartedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CompletedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(1000);
        builder.HasOne<EconomicSeries>().WithMany().HasForeignKey(entity => entity.EconomicSeriesId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EconomicSeriesId, entity.StartedAtUtc })
            .HasDatabaseName("IX_EconomicSyncRuns_Series_StartedAtUtc");
        builder.HasIndex(entity => new { entity.Status, entity.StartedAtUtc })
            .HasDatabaseName("IX_EconomicSyncRuns_Status_StartedAtUtc");
    }
}
