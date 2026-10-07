using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Evidence;
using XauAi.Domain.ReferenceData;
using XauAi.Domain.TargetAnalysis;
using DomainTargetAnalysis = XauAi.Domain.TargetAnalysis.TargetAnalysis;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class TargetAnalysisConfiguration : IEntityTypeConfiguration<DomainTargetAnalysis>
{
    public void Configure(EntityTypeBuilder<DomainTargetAnalysis> builder)
    {
        builder.ToTable("TargetAnalyses", table =>
        {
            table.HasCheckConstraint("CK_TargetAnalyses_CurrentPrice", "[CurrentPrice] IS NULL OR [CurrentPrice] > 0");
            table.HasCheckConstraint("CK_TargetAnalyses_TargetPrice", "[TargetPrice] IS NULL OR [TargetPrice] > 0");
            table.HasCheckConstraint("CK_TargetAnalyses_InvalidationPrice", "[InvalidationPrice] IS NULL OR [InvalidationPrice] > 0");
            table.HasCheckConstraint("CK_TargetAnalyses_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_TargetAnalyses_SnapshotJson", "ISJSON([SnapshotJson]) = 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Symbol).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Timeframe).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.AnalysisTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CurrentPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.TargetPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.InvalidationPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.DirectionContext).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Confidence).HasPrecision(9, 6);
        builder.Property(entity => entity.ValidUntilUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ReasoningSummary).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.Uncertainty).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.NoTargetReason).HasMaxLength(500);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Provider).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.PromptVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ConfigurationVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.EndedAtUtc).IsUtcTimestamp();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TargetSpecialistResult>().WithMany().HasForeignKey(entity => entity.MasterResultId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.Symbol, entity.Status, entity.AnalysisTimeUtc })
            .HasDatabaseName("IX_TargetAnalyses_Symbol_Status_AnalysisTimeUtc");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.Timeframe, entity.AnalysisTimeUtc })
            .HasDatabaseName("IX_TargetAnalyses_Instrument_Timeframe_AnalysisTimeUtc");
    }
}

internal sealed class TargetSpecialistResultConfiguration : IEntityTypeConfiguration<TargetSpecialistResult>
{
    public void Configure(EntityTypeBuilder<TargetSpecialistResult> builder)
    {
        builder.ToTable("TargetSpecialistResults", table =>
        {
            table.HasCheckConstraint("CK_TargetSpecialistResults_TargetPrice", "[CandidateTargetPrice] IS NULL OR [CandidateTargetPrice] > 0");
            table.HasCheckConstraint("CK_TargetSpecialistResults_InvalidationPrice", "[CandidateInvalidationPrice] IS NULL OR [CandidateInvalidationPrice] > 0");
            table.HasCheckConstraint("CK_TargetSpecialistResults_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_TargetSpecialistResults_OutputJson", "ISJSON([OutputJson]) = 1");
            table.HasCheckConstraint("CK_TargetSpecialistResults_EvidenceIdsJson", "ISJSON([EvidenceIdsJson]) = 1");
            table.HasCheckConstraint("CK_TargetSpecialistResults_Tokens", "([InputTokens] IS NULL OR [InputTokens] >= 0) AND ([OutputTokens] IS NULL OR [OutputTokens] >= 0)");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Workspace).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CandidateTargetPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.CandidateInvalidationPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.DirectionContext).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Confidence).HasPrecision(9, 6);
        builder.Property(entity => entity.Summary).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.Uncertainty).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.OutputJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.EvidenceIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Provider).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.PromptVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ConfigurationVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(4000);
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CompletedAtUtc).IsUtcTimestamp();
        builder.HasOne<DomainTargetAnalysis>().WithMany().HasForeignKey(entity => entity.TargetAnalysisId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TargetAnalysisId, entity.Workspace }).IsUnique()
            .HasDatabaseName("UX_TargetSpecialistResults_Analysis_Workspace");
    }
}

internal sealed class TargetAnalysisEvidenceConfiguration : IEntityTypeConfiguration<TargetAnalysisEvidence>
{
    public void Configure(EntityTypeBuilder<TargetAnalysisEvidence> builder)
    {
        builder.ToTable("TargetAnalysisEvidence");
        builder.HasKey(entity => new { entity.TargetAnalysisId, entity.EvidenceRecordId });
        builder.Property(entity => entity.Role).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasOne<DomainTargetAnalysis>().WithMany().HasForeignKey(entity => entity.TargetAnalysisId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceRecord>().WithMany().HasForeignKey(entity => entity.EvidenceRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EvidenceRecordId).HasDatabaseName("IX_TargetAnalysisEvidence_EvidenceRecordId");
    }
}

internal sealed class TargetAnalysisLifecycleEventConfiguration : IEntityTypeConfiguration<TargetAnalysisLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<TargetAnalysisLifecycleEvent> builder)
    {
        builder.ToTable("TargetAnalysisLifecycleEvents");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.EventType).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PreviousStatus).HasMaxLength(32).IsUnicode(false);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.Property(entity => entity.Price).HasPrecision(19, 8);
        builder.Property(entity => entity.OccurredAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.HasOne<DomainTargetAnalysis>().WithMany().HasForeignKey(entity => entity.TargetAnalysisId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TargetAnalysisId, entity.OccurredAtUtc })
            .HasDatabaseName("IX_TargetAnalysisLifecycleEvents_Analysis_Time");
    }
}
