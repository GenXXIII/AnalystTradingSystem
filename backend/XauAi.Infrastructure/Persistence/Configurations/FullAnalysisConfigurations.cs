using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.FullAnalysis;
using XauAi.Domain.ReferenceData;
using DomainFullAnalysis = XauAi.Domain.FullAnalysis.FullAnalysis;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class FullAnalysisConfiguration : IEntityTypeConfiguration<DomainFullAnalysis>
{
    public void Configure(EntityTypeBuilder<DomainFullAnalysis> builder)
    {
        builder.ToTable("FullAnalyses", table =>
        {
            table.HasCheckConstraint("CK_FullAnalyses_CurrentPrice", "[CurrentPrice] IS NULL OR [CurrentPrice] > 0");
            table.HasCheckConstraint("CK_FullAnalyses_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_FullAnalyses_Agreement", "[Agreement] IS NULL OR [Agreement] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_FullAnalyses_Json", "ISJSON([ConflictsJson]) = 1 AND ISJSON([KeyEvidenceIdsJson]) = 1 AND ISJSON([InvalidationJson]) = 1 AND ISJSON([SnapshotJson]) = 1 AND ISJSON([WorkspaceResultsJson]) = 1");
            table.HasCheckConstraint("CK_FullAnalyses_Tokens", "[InputTokens] >= 0 AND [OutputTokens] >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Symbol).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Timeframe).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.AnalysisTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CurrentPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.Decision).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Confidence).HasPrecision(9, 6);
        builder.Property(entity => entity.Agreement).HasPrecision(9, 6);
        builder.Property(entity => entity.ConflictsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.KeyEvidenceIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Reasoning).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.InvalidationJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Uncertainty).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ValidUntilUtc).IsUtcTimestamp();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Provider).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.PromptVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ConfigurationVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SnapshotHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.WorkspaceResultsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.EndedAtUtc).IsUtcTimestamp();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.Symbol, entity.Status, entity.AnalysisTimeUtc })
            .HasDatabaseName("IX_FullAnalyses_Symbol_Status_AnalysisTimeUtc");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.Timeframe, entity.AnalysisTimeUtc })
            .HasDatabaseName("IX_FullAnalyses_Instrument_Timeframe_AnalysisTimeUtc");
        builder.HasIndex(entity => entity.SnapshotHash).HasDatabaseName("IX_FullAnalyses_SnapshotHash");
    }
}

internal sealed class FullAnalysisLifecycleEventConfiguration : IEntityTypeConfiguration<FullAnalysisLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<FullAnalysisLifecycleEvent> builder)
    {
        builder.ToTable("FullAnalysisLifecycleEvents");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.EventType).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PreviousStatus).HasMaxLength(32).IsUnicode(false);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.Property(entity => entity.Price).HasPrecision(19, 8);
        builder.Property(entity => entity.OccurredAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.HasOne<DomainFullAnalysis>().WithMany().HasForeignKey(entity => entity.FullAnalysisId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.FullAnalysisId, entity.OccurredAtUtc })
            .HasDatabaseName("IX_FullAnalysisLifecycleEvents_Analysis_Time");
    }
}
