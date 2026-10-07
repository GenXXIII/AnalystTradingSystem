using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Evidence;
using XauAi.Domain.ReferenceData;
using XauAi.Domain.Strategies;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class StrategyConfiguration : IEntityTypeConfiguration<Strategy>
{
    private static readonly Guid LocalAnalystStrategyId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<Strategy> builder)
    {
        builder.ToTable("Strategies");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Key).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Category).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(entity => entity.Key).IsUnique().HasDatabaseName("UX_Strategies_Key");
        builder.HasIndex(entity => entity.Category).HasDatabaseName("IX_Strategies_Category");

        builder.HasData(new Strategy
        {
            Id = LocalAnalystStrategyId,
            Key = "local-analyst-xauusd",
            Name = "Local XAUUSD Analyst",
            Category = "DeterministicSignal",
            Description = "Phase 12 deterministic, AI-independent local analyst and signal engine.",
            CreatedAtUtc = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero)
        });
    }
}

internal sealed class StrategyVersionConfiguration : IEntityTypeConfiguration<StrategyVersion>
{
    private static readonly Guid LocalAnalystStrategyId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid LocalAnalystStrategyVersionId = Guid.Parse("41000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<StrategyVersion> builder)
    {
        builder.ToTable("StrategyVersions", table =>
            table.HasCheckConstraint("CK_StrategyVersions_DefinitionJson", "ISJSON([DefinitionJson]) = 1"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Version).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.DefinitionJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.DefinitionHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.EffectiveFromUtc).IsUtcTimestamp();
        builder.Property(entity => entity.RetiredAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<Strategy>().WithMany().HasForeignKey(entity => entity.StrategyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.StrategyId, entity.Version })
            .IsUnique()
            .HasDatabaseName("UX_StrategyVersions_Strategy_Version");
        builder.HasIndex(entity => entity.DefinitionHash).HasDatabaseName("IX_StrategyVersions_DefinitionHash");

        builder.HasData(new StrategyVersion
        {
            Id = LocalAnalystStrategyVersionId,
            StrategyId = LocalAnalystStrategyId,
            Version = "phase12-v1",
            DefinitionJson = "{\"engine\":\"local-analyst\",\"version\":\"phase12-v1\",\"symbol\":\"XAUUSD\",\"states\":[\"NOTHING\",\"BUY\",\"SELL\",\"STOP\"],\"usesAi\":false}",
            DefinitionHash = "05aee2de1eedebe1cc22d748166ed8660012214084f332bad184fef69272d62b",
            Status = "Active",
            EffectiveFromUtc = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero),
            CreatedAtUtc = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero)
        });
    }
}

internal sealed class StrategyEvaluationConfiguration : IEntityTypeConfiguration<StrategyEvaluation>
{
    public void Configure(EntityTypeBuilder<StrategyEvaluation> builder)
    {
        builder.ToTable("StrategyEvaluations", table =>
        {
            table.HasCheckConstraint("CK_StrategyEvaluations_Score", "[Score] IS NULL OR [Score] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_StrategyEvaluations_DetailsJson", "[DetailsJson] IS NULL OR ISJSON([DetailsJson]) = 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Result).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Score).HasPrecision(9, 6);
        builder.Property(entity => entity.DetailsJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.EvaluatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<StrategyVersion>().WithMany().HasForeignKey(entity => entity.StrategyVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.StrategyVersionId, entity.EvaluatedAtUtc })
            .HasDatabaseName("IX_StrategyEvaluations_StrategyVersion_EvaluatedAtUtc");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.TimeframeId, entity.EvaluatedAtUtc })
            .HasDatabaseName("IX_StrategyEvaluations_Instrument_Timeframe_EvaluatedAtUtc");
    }
}

internal sealed class StrategyEvaluationEvidenceConfiguration : IEntityTypeConfiguration<StrategyEvaluationEvidence>
{
    public void Configure(EntityTypeBuilder<StrategyEvaluationEvidence> builder)
    {
        builder.ToTable("StrategyEvaluationEvidence");
        builder.HasKey(entity => new { entity.StrategyEvaluationId, entity.EvidenceRecordId });
        builder.Property(entity => entity.Role).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasOne<StrategyEvaluation>().WithMany().HasForeignKey(entity => entity.StrategyEvaluationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceRecord>().WithMany().HasForeignKey(entity => entity.EvidenceRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EvidenceRecordId).HasDatabaseName("IX_StrategyEvaluationEvidence_EvidenceRecordId");
    }
}
