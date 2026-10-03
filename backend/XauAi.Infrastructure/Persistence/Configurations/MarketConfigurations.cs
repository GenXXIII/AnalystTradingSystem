using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Evidence;
using XauAi.Domain.Market;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class MarketCandleConfiguration : IEntityTypeConfiguration<MarketCandle>
{
    public void Configure(EntityTypeBuilder<MarketCandle> builder)
    {
        builder.ToTable("MarketCandles", table =>
        {
            table.HasCheckConstraint("CK_MarketCandles_TimeRange", "[CloseTimeUtc] > [OpenTimeUtc]");
            table.HasCheckConstraint(
                "CK_MarketCandles_Prices",
                "[High] >= [Low] AND [Open] BETWEEN [Low] AND [High] AND [Close] BETWEEN [Low] AND [High]");
            table.HasCheckConstraint(
                "CK_MarketCandles_NonNegativeMeasurements",
                "([TickVolume] IS NULL OR [TickVolume] >= 0) AND ([RealVolume] IS NULL OR [RealVolume] >= 0) AND ([Spread] IS NULL OR [Spread] >= 0)");
        });

        builder.HasKey(entity => entity.Id).IsClustered(false);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ProviderSymbol).HasMaxLength(64).IsUnicode(false).IsRequired();
        ConfigurePrice(builder.Property(entity => entity.Open));
        ConfigurePrice(builder.Property(entity => entity.High));
        ConfigurePrice(builder.Property(entity => entity.Low));
        ConfigurePrice(builder.Property(entity => entity.Close));
        builder.Property(entity => entity.TickVolume).HasPrecision(28, 8);
        builder.Property(entity => entity.RealVolume).HasPrecision(28, 8);
        builder.Property(entity => entity.Spread).HasPrecision(19, 8);
        builder.Property(entity => entity.SourceTimeZone).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.OpenTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CloseTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.FetchedAtUtc).IsUtcTimestamp();

        builder.HasOne<EvidenceRecord>().WithOne().HasForeignKey<MarketCandle>(entity => entity.Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new
        {
            entity.InstrumentId,
            entity.TimeframeId,
            entity.OpenTimeUtc,
            entity.DataProviderId
        })
            .IsUnique()
            .IsClustered()
            .HasDatabaseName("UX_MarketCandles_Instrument_Timeframe_OpenTime_Provider");
        builder.HasIndex(entity => new
        {
            entity.DataProviderId,
            entity.InstrumentId,
            entity.TimeframeId,
            entity.OpenTimeUtc
        })
            .HasDatabaseName("IX_MarketCandles_Provider_Instrument_Timeframe_OpenTime");
    }

    private static void ConfigurePrice(PropertyBuilder<decimal> property) => property.HasPrecision(19, 8);
}

internal sealed class TechnicalObservationConfiguration : IEntityTypeConfiguration<TechnicalObservation>
{
    public void Configure(EntityTypeBuilder<TechnicalObservation> builder)
    {
        builder.ToTable("TechnicalObservations", table =>
        {
            table.HasCheckConstraint("CK_TechnicalObservations_ParametersJson", "ISJSON([ParametersJson]) = 1");
            table.HasCheckConstraint("CK_TechnicalObservations_ValuesJson", "[ValuesJson] IS NULL OR ISJSON([ValuesJson]) = 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ObservationType).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.CalculationVersion).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ParametersHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ParametersJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.NumericValue).HasPrecision(28, 10);
        builder.Property(entity => entity.ValueText).HasMaxLength(512);
        builder.Property(entity => entity.ValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ObservedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne<EvidenceRecord>().WithOne().HasForeignKey<TechnicalObservation>(entity => entity.Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new
        {
            entity.InstrumentId,
            entity.TimeframeId,
            entity.Name,
            entity.CalculationVersion,
            entity.ObservedAtUtc,
            entity.ParametersHash
        })
            .IsUnique()
            .HasDatabaseName("UX_TechnicalObservations_Identity");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.TimeframeId, entity.ObservedAtUtc })
            .HasDatabaseName("IX_TechnicalObservations_Instrument_Timeframe_Time");
    }
}
