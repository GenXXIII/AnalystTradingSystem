using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.AI;
using XauAi.Domain.Evidence;
using XauAi.Domain.ReferenceData;
using XauAi.Domain.Signals;
using XauAi.Domain.Strategies;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class TradingSignalConfiguration : IEntityTypeConfiguration<TradingSignal>
{
    public void Configure(EntityTypeBuilder<TradingSignal> builder)
    {
        builder.ToTable("TradingSignals", table =>
        {
            table.HasCheckConstraint("CK_TradingSignals_ModelConfidence", "[ModelConfidence] IS NULL OR [ModelConfidence] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_TradingSignals_Score", "[Score] IS NULL OR [Score] >= 0");
            table.HasCheckConstraint("CK_TradingSignals_MaxScore", "[MaxScore] IS NULL OR [MaxScore] > 0");
            table.HasCheckConstraint("CK_TradingSignals_RiskConditionsJson", "[RiskConditionsJson] IS NULL OR ISJSON([RiskConditionsJson]) = 1");
            table.HasCheckConstraint("CK_TradingSignals_ExplanationJson", "[ExplanationJson] IS NULL OR ISJSON([ExplanationJson]) = 1");
            table.HasCheckConstraint("CK_TradingSignals_TimeHorizon", "[TimeHorizonSeconds] IS NULL OR [TimeHorizonSeconds] > 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.SignalKey).HasMaxLength(160).IsUnicode(false);
        builder.Property(entity => entity.Source).HasMaxLength(48).IsUnicode(false);
        builder.Property(entity => entity.SignalCandleId).HasMaxLength(160).IsUnicode(false);
        builder.Property(entity => entity.SignalCandleTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.Direction).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.EntryPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.StopLossPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.TakeProfitPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.ModelConfidence).HasPrecision(9, 6);
        builder.Property(entity => entity.Score).HasPrecision(9, 4);
        builder.Property(entity => entity.MaxScore).HasPrecision(9, 4);
        builder.Property(entity => entity.StructureState).HasMaxLength(80).IsUnicode(false);
        builder.Property(entity => entity.LiquidityState).HasMaxLength(80).IsUnicode(false);
        builder.Property(entity => entity.CandleState).HasMaxLength(80).IsUnicode(false);
        builder.Property(entity => entity.MomentumState).HasMaxLength(80).IsUnicode(false);
        builder.Property(entity => entity.KtrState).HasMaxLength(80).IsUnicode(false);
        builder.Property(entity => entity.VolatilityState).HasMaxLength(80).IsUnicode(false);
        builder.Property(entity => entity.ExplanationJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.InvalidationReason).HasMaxLength(500);
        builder.Property(entity => entity.ValidUntilUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastEvaluatedCandleTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ConfigurationVersion).HasMaxLength(64).IsUnicode(false);
        builder.Property(entity => entity.RiskConditionsJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SignalAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.EndedAtUtc).IsUtcTimestamp();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StrategyVersion>().WithMany().HasForeignKey(entity => entity.StrategyVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StrategyEvaluation>().WithMany().HasForeignKey(entity => entity.StrategyEvaluationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiAnalysis>().WithMany().HasForeignKey(entity => entity.AiAnalysisId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.InstrumentId, entity.SignalAtUtc })
            .HasDatabaseName("IX_TradingSignals_Instrument_SignalAtUtc");
        builder.HasIndex(entity => new { entity.StrategyVersionId, entity.SignalAtUtc })
            .HasDatabaseName("IX_TradingSignals_StrategyVersion_SignalAtUtc");
        builder.HasIndex(entity => new { entity.Status, entity.SignalAtUtc })
            .HasDatabaseName("IX_TradingSignals_Status_SignalAtUtc");
        builder.HasIndex(entity => entity.SignalKey)
            .IsUnique()
            .HasFilter("[SignalKey] IS NOT NULL AND [SignalKey] <> ''")
            .HasDatabaseName("UX_TradingSignals_SignalKey");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.TimeframeId, entity.Source })
            .IsUnique()
            .HasFilter("[Status] = 'ACTIVE' AND [Source] = 'LocalAnalyst'")
            .HasDatabaseName("UX_TradingSignals_LocalAnalyst_Active");
    }
}

internal sealed class TradingSignalLifecycleEventConfiguration : IEntityTypeConfiguration<TradingSignalLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<TradingSignalLifecycleEvent> builder)
    {
        builder.ToTable("TradingSignalLifecycleEvents", table =>
        {
            table.HasCheckConstraint("CK_TradingSignalLifecycleEvents_DetailsJson", "[DetailsJson] IS NULL OR ISJSON([DetailsJson]) = 1");
            table.HasCheckConstraint("CK_TradingSignalLifecycleEvents_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.EventType).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PreviousStatus).HasMaxLength(32).IsUnicode(false);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Direction).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.Property(entity => entity.CandleTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.Price).HasPrecision(19, 8);
        builder.Property(entity => entity.Score).HasPrecision(9, 4);
        builder.Property(entity => entity.Confidence).HasPrecision(9, 6);
        builder.Property(entity => entity.DetailsJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.OccurredAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<TradingSignal>().WithMany().HasForeignKey(entity => entity.TradingSignalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StrategyEvaluation>().WithMany().HasForeignKey(entity => entity.StrategyEvaluationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TradingSignalId, entity.OccurredAtUtc })
            .HasDatabaseName("IX_TradingSignalLifecycleEvents_Signal_Time");
    }
}

internal sealed class LocalAnalystProcessingStateConfiguration : IEntityTypeConfiguration<LocalAnalystProcessingState>
{
    public void Configure(EntityTypeBuilder<LocalAnalystProcessingState> builder)
    {
        builder.ToTable("LocalAnalystProcessingStates", table =>
            table.HasCheckConstraint("CK_LocalAnalystProcessingStates_SnapshotJson", "[LastSnapshotJson] IS NULL OR ISJSON([LastSnapshotJson]) = 1"));
        builder.HasKey(entity => new { entity.InstrumentId, entity.TimeframeId });
        builder.Property(entity => entity.LastProcessedCandleTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastResult).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.LastReason).HasMaxLength(500);
        builder.Property(entity => entity.LastSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StrategyEvaluation>().WithMany().HasForeignKey(entity => entity.LastStrategyEvaluationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TradingSignal>().WithMany().HasForeignKey(entity => entity.CurrentTradingSignalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.UpdatedAtUtc).HasDatabaseName("IX_LocalAnalystProcessingStates_UpdatedAtUtc");
    }
}

internal sealed class TradingSignalEvidenceConfiguration : IEntityTypeConfiguration<TradingSignalEvidence>
{
    public void Configure(EntityTypeBuilder<TradingSignalEvidence> builder)
    {
        builder.ToTable("TradingSignalEvidence");
        builder.HasKey(entity => new { entity.TradingSignalId, entity.EvidenceRecordId });
        builder.Property(entity => entity.Role).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasOne<TradingSignal>().WithMany().HasForeignKey(entity => entity.TradingSignalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceRecord>().WithMany().HasForeignKey(entity => entity.EvidenceRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EvidenceRecordId).HasDatabaseName("IX_TradingSignalEvidence_EvidenceRecordId");
    }
}

internal sealed class SignalOutcomeConfiguration : IEntityTypeConfiguration<SignalOutcome>
{
    public void Configure(EntityTypeBuilder<SignalOutcome> builder)
    {
        builder.ToTable("SignalOutcomes", table =>
            table.HasCheckConstraint("CK_SignalOutcomes_MeasurementDetailsJson", "[MeasurementDetailsJson] IS NULL OR ISJSON([MeasurementDetailsJson]) = 1"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.OutcomeStatus).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ExitPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.ProfitLossAmount).HasPrecision(19, 8);
        builder.Property(entity => entity.ProfitLossR).HasPrecision(19, 8);
        builder.Property(entity => entity.MaximumFavorableExcursion).HasPrecision(19, 8);
        builder.Property(entity => entity.MaximumAdverseExcursion).HasPrecision(19, 8);
        builder.Property(entity => entity.MeasurementDetailsJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ClosedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.MeasuredAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<TradingSignal>().WithOne().HasForeignKey<SignalOutcome>(entity => entity.TradingSignalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.TradingSignalId).IsUnique().HasDatabaseName("UX_SignalOutcomes_TradingSignalId");
        builder.HasIndex(entity => new { entity.OutcomeStatus, entity.MeasuredAtUtc })
            .HasDatabaseName("IX_SignalOutcomes_Status_MeasuredAtUtc");
    }
}
