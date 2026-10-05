using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.AI;
using XauAi.Domain.Evidence;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class AiAnalysisConfiguration : IEntityTypeConfiguration<AiAnalysis>
{
    public void Configure(EntityTypeBuilder<AiAnalysis> builder)
    {
        builder.ToTable("AiAnalyses", table =>
        {
            table.HasCheckConstraint("CK_AiAnalyses_Tokens", "([InputTokens] IS NULL OR [InputTokens] >= 0) AND ([OutputTokens] IS NULL OR [OutputTokens] >= 0)");
            table.HasCheckConstraint("CK_AiAnalyses_Cost", "[CostUsd] IS NULL OR [CostUsd] >= 0");
            table.HasCheckConstraint("CK_AiAnalyses_Latency", "[LatencyMilliseconds] IS NULL OR [LatencyMilliseconds] >= 0");
            table.HasCheckConstraint("CK_AiAnalyses_Confidence", "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");
            table.HasCheckConstraint("CK_AiAnalyses_AffectedAssetsJson", "ISJSON([AffectedAssetsJson]) = 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.AnalysisType).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Specialist).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Timeframe).HasMaxLength(16).IsUnicode(false);
        builder.Property(entity => entity.Provider).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ModelVersion).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.PromptIdentifier).HasMaxLength(160).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PromptVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.AnalysisVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ApplicationVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.InputDigest).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(entity => entity.CacheKey).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.EvidenceVersion).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.AnalysisTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.EvidenceUpdatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.Direction).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Impact).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.AffectedAssetsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Mechanism).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ExpectedEffect).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ObservedReaction).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.ReactionAlignment).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CurrentRelevance).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Confidence).HasPrecision(9, 6);
        builder.Property(entity => entity.Uncertainty).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.Summary).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.Output).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.LifecycleStatus).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CostUsd).HasPrecision(19, 8);
        builder.Property(entity => entity.ErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(4000);
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.CompletedAtUtc).IsUtcTimestamp();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.AnalysisType, entity.CreatedAtUtc })
            .HasDatabaseName("IX_AiAnalyses_Type_CreatedAtUtc");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.AnalysisTimeUtc })
            .HasDatabaseName("IX_AiAnalyses_Instrument_AnalysisTimeUtc");
        builder.HasIndex(entity => new { entity.Specialist, entity.AnalysisType, entity.LifecycleStatus, entity.AnalysisTimeUtc })
            .HasDatabaseName("IX_AiAnalyses_Specialist_Type_Lifecycle_AnalysisTimeUtc");
        builder.HasIndex(entity => new { entity.CacheKey, entity.Status, entity.LifecycleStatus })
            .IsUnique()
            .HasFilter("[Status] = 'Completed' AND [LifecycleStatus] = 'Current'")
            .HasDatabaseName("IX_AiAnalyses_CacheKey_Status_Lifecycle");
        builder.HasIndex(entity => new
        {
            entity.Provider,
            entity.Model,
            entity.ModelVersion,
            entity.PromptIdentifier,
            entity.PromptVersion
        })
            .HasDatabaseName("IX_AiAnalyses_Model_PromptVersion");
        builder.HasIndex(entity => new { entity.Status, entity.CreatedAtUtc })
            .HasDatabaseName("IX_AiAnalyses_Status_CreatedAtUtc");
    }
}

internal sealed class AiAnalysisEvidenceConfiguration : IEntityTypeConfiguration<AiAnalysisEvidence>
{
    public void Configure(EntityTypeBuilder<AiAnalysisEvidence> builder)
    {
        builder.ToTable("AiAnalysisEvidence");
        builder.HasKey(entity => new { entity.AiAnalysisId, entity.EvidenceRecordId });
        builder.Property(entity => entity.Role).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasOne<AiAnalysis>().WithMany().HasForeignKey(entity => entity.AiAnalysisId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceRecord>().WithMany().HasForeignKey(entity => entity.EvidenceRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EvidenceRecordId).HasDatabaseName("IX_AiAnalysisEvidence_EvidenceRecordId");
    }
}
