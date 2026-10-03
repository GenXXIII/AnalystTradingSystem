using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Evidence;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class EvidenceRecordConfiguration : IEntityTypeConfiguration<EvidenceRecord>
{
    public void Configure(EntityTypeBuilder<EvidenceRecord> builder)
    {
        builder.ToTable("EvidenceRecords");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Kind).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ObservedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc)
            .IsUtcTimestamp()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(entity => new { entity.Kind, entity.ObservedAtUtc })
            .HasDatabaseName("IX_EvidenceRecords_Kind_ObservedAtUtc");
    }
}

internal sealed class DataProviderConfiguration : IEntityTypeConfiguration<DataProvider>
{
    private static readonly Guid Mt5ProviderId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid NewsDataProviderId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid FredProviderId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid AllTickProviderId = Guid.Parse("30000000-0000-0000-0000-000000000004");
    private static readonly Guid TwelveDataProviderId = Guid.Parse("30000000-0000-0000-0000-000000000005");

    public void Configure(EntityTypeBuilder<DataProvider> builder)
    {
        builder.ToTable("DataProviders");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Key).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ProviderType).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(entity => entity.Key).IsUnique().HasDatabaseName("UX_DataProviders_Key");
        builder.HasIndex(entity => new { entity.ProviderType, entity.IsActive })
            .HasDatabaseName("IX_DataProviders_Type_Active");

        builder.HasData(
            new DataProvider
            {
                Id = Mt5ProviderId,
                Key = "mt5",
                Name = "MetaTrader 5",
                ProviderType = "Market",
                IsActive = true,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DataProvider
            {
                Id = NewsDataProviderId,
                Key = "newsdata",
                Name = "NewsData.io",
                ProviderType = "News",
                IsActive = true,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DataProvider
            {
                Id = FredProviderId,
                Key = "fred",
                Name = "Federal Reserve Economic Data (FRED)",
                ProviderType = "Economic",
                IsActive = true,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DataProvider
            {
                Id = AllTickProviderId,
                Key = "alltick",
                Name = "AllTick",
                ProviderType = "Market",
                IsActive = true,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DataProvider
            {
                Id = TwelveDataProviderId,
                Key = "twelvedata",
                Name = "Twelve Data",
                ProviderType = "Market",
                IsActive = true,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
    }
}

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    private static readonly Guid XauUsdId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments", table =>
            table.HasCheckConstraint("CK_Instruments_PriceScale", "[PriceScale] BETWEEN 0 AND 12"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Symbol).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.AssetClass).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.BaseAsset).HasMaxLength(20).IsUnicode(false);
        builder.Property(entity => entity.QuoteAsset).HasMaxLength(20).IsUnicode(false);
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(entity => entity.Symbol).IsUnique().HasDatabaseName("UX_Instruments_Symbol");

        builder.HasData(new Instrument
        {
            Id = XauUsdId,
            Symbol = "XAUUSD",
            DisplayName = "Gold / US Dollar",
            AssetClass = "Commodity",
            BaseAsset = "XAU",
            QuoteAsset = "USD",
            PriceScale = 8,
            IsActive = true,
            CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}

internal sealed class TimeframeDefinitionConfiguration : IEntityTypeConfiguration<TimeframeDefinition>
{
    public void Configure(EntityTypeBuilder<TimeframeDefinition> builder)
    {
        builder.ToTable("Timeframes", table =>
            table.HasCheckConstraint("CK_Timeframes_Duration", "[DurationSeconds] > 0"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Code).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_Timeframes_Code");
        builder.HasIndex(entity => entity.DurationSeconds).IsUnique().HasDatabaseName("UX_Timeframes_Duration");

        builder.HasData(
            Create("20000000-0000-0000-0000-000000000001", "M1", "1 minute", 60, 1),
            Create("20000000-0000-0000-0000-000000000002", "M5", "5 minutes", 300, 2),
            Create("20000000-0000-0000-0000-000000000003", "M15", "15 minutes", 900, 3),
            Create("20000000-0000-0000-0000-000000000004", "M30", "30 minutes", 1800, 4),
            Create("20000000-0000-0000-0000-000000000005", "H1", "1 hour", 3600, 5),
            Create("20000000-0000-0000-0000-000000000006", "H4", "4 hours", 14400, 6),
            Create("20000000-0000-0000-0000-000000000007", "D1", "1 day", 86400, 7));
    }

    private static TimeframeDefinition Create(string id, string code, string name, int seconds, int sortOrder) =>
        new()
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            DurationSeconds = seconds,
            SortOrder = sortOrder,
            IsActive = true
        };
}
