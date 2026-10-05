using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Evidence;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class EvidenceRecordConfiguration : IEntityTypeConfiguration<EvidenceRecord>
{
    public void Configure(EntityTypeBuilder<EvidenceRecord> builder)
    {
        builder.ToTable("EvidenceRecords", table =>
        {
            table.HasCheckConstraint(
                "CK_EvidenceRecords_EvidenceType",
                "[EvidenceType] IN ('Market', 'Technical', 'Candle', 'CandleFlow', 'Liquidity', 'OrderFlow', 'Session', 'News', 'Economic', 'Analyst')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_SourceType",
                "[SourceType] IN ('InternalMarketData', 'InternalTechnicalEngine', 'NewsProvider', 'EconomicProvider', 'AnalystProvider', 'Manual', 'Other')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_Direction",
                "[Direction] IN ('Unknown', 'Bullish', 'Bearish', 'Neutral')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_Importance",
                "[Importance] IN ('Unknown', 'Low', 'Medium', 'High', 'Critical')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_Quality",
                "[Quality] IN ('Unknown', 'Low', 'Medium', 'High')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_Completeness",
                "[Completeness] IN ('Unknown', 'Partial', 'Complete')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_TimestampQuality",
                "[TimestampQuality] IN ('Unknown', 'Approximate', 'DateOnly', 'Exact')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_SourceReliability",
                "[SourceReliability] IN ('Unknown', 'Known')");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_Validity",
                "[ValidToUtc] IS NULL OR [ValidFromUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
            table.HasCheckConstraint(
                "CK_EvidenceRecords_MetadataJson",
                "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Kind).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.EvidenceType).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SourceType).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SourceKey).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.IdentityHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ContentHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(entity => entity.CanonicalSymbol).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.OriginalSymbol).HasMaxLength(64).IsUnicode(false);
        builder.Property(entity => entity.TimeframeCode).HasMaxLength(8).IsUnicode(false);
        builder.Property(entity => entity.ObservedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.AvailableAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.PublishedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CollectedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ValidFromUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ValidToUtc).IsUtcTimestamp();
        builder.Property(entity => entity.Title).HasMaxLength(1000);
        builder.Property(entity => entity.Summary).HasMaxLength(4000);
        builder.Property(entity => entity.NumericValue).HasPrecision(28, 10);
        builder.Property(entity => entity.OriginalValue).HasMaxLength(256);
        builder.Property(entity => entity.Unit).HasMaxLength(32).IsUnicode(false);
        builder.Property(entity => entity.OriginalUnit).HasMaxLength(100);
        builder.Property(entity => entity.Direction).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.OriginalDirection).HasMaxLength(100);
        builder.Property(entity => entity.Importance).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.OriginalImportance).HasMaxLength(100);
        builder.Property(entity => entity.Category).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.OriginalCategory).HasMaxLength(200);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false);
        builder.Property(entity => entity.OriginalSourceUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.Quality).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Completeness).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.TimestampQuality).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SourceReliability).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.RelevanceReason).HasMaxLength(500);
        builder.Property(entity => entity.MetadataJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.CreatedAtUtc)
            .IsUtcTimestamp()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.UpdatedAtUtc)
            .IsUtcTimestamp()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeframeDefinition>().WithMany().HasForeignKey(entity => entity.TimeframeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.IdentityHash)
            .IsUnique()
            .HasDatabaseName("UX_EvidenceRecords_IdentityHash");
        builder.HasIndex(entity => new { entity.SourceKey, entity.ExternalId })
            .HasFilter("[ExternalId] IS NOT NULL")
            .HasDatabaseName("IX_EvidenceRecords_Source_ExternalId");
        builder.HasIndex(entity => entity.ContentHash)
            .HasFilter("[ContentHash] IS NOT NULL")
            .HasDatabaseName("IX_EvidenceRecords_ContentHash");
        builder.HasIndex(entity => new { entity.CanonicalSymbol, entity.AvailableAtUtc })
            .HasDatabaseName("IX_EvidenceRecords_Instrument_AvailableAtUtc");
        builder.HasIndex(entity => new { entity.EvidenceType, entity.AvailableAtUtc })
            .HasDatabaseName("IX_EvidenceRecords_Type_AvailableAtUtc");
        builder.HasIndex(entity => new { entity.SourceType, entity.AvailableAtUtc })
            .HasDatabaseName("IX_EvidenceRecords_SourceType_AvailableAtUtc");
        builder.HasIndex(entity => new { entity.Direction, entity.AvailableAtUtc })
            .HasDatabaseName("IX_EvidenceRecords_Direction_AvailableAtUtc");
        builder.HasIndex(entity => new { entity.TimeframeCode, entity.AvailableAtUtc })
            .HasFilter("[TimeframeCode] IS NOT NULL")
            .HasDatabaseName("IX_EvidenceRecords_Timeframe_AvailableAtUtc");
        builder.HasIndex(entity => new { entity.Kind, entity.ObservedAtUtc })
            .HasDatabaseName("IX_EvidenceRecords_Kind_ObservedAtUtc");
    }
}

internal sealed class EvidenceRelationConfiguration : IEntityTypeConfiguration<EvidenceRelation>
{
    public void Configure(EntityTypeBuilder<EvidenceRelation> builder)
    {
        builder.ToTable("EvidenceRelations", table =>
        {
            table.HasCheckConstraint("CK_EvidenceRelations_NotSelf", "[EvidenceId] <> [RelatedEvidenceId]");
            table.HasCheckConstraint(
                "CK_EvidenceRelations_Type",
                "[RelationType] IN ('Duplicate', 'Republished', 'SameEvent', 'Contradicts', 'Supports', 'Updates', 'References')");
        });
        builder.HasKey(entity => new { entity.EvidenceId, entity.RelatedEvidenceId, entity.RelationType });
        builder.Property(entity => entity.RelationType).HasMaxLength(24).IsUnicode(false);
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<EvidenceRecord>().WithMany().HasForeignKey(entity => entity.EvidenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceRecord>().WithMany().HasForeignKey(entity => entity.RelatedEvidenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.RelatedEvidenceId, entity.RelationType })
            .HasDatabaseName("IX_EvidenceRelations_Related_Type");
    }
}

internal sealed class EvidenceClusterConfiguration : IEntityTypeConfiguration<EvidenceCluster>
{
    public void Configure(EntityTypeBuilder<EvidenceCluster> builder)
    {
        builder.ToTable("EvidenceClusters", table => table.HasCheckConstraint(
            "CK_EvidenceClusters_Type",
            "[ClusterType] IN ('NewsEvent', 'EconomicEvent', 'MarketEvent', 'Other')"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ClusterType).HasMaxLength(24).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.DeterministicKey).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.EventTimeUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasIndex(entity => new { entity.ClusterType, entity.DeterministicKey })
            .IsUnique()
            .HasDatabaseName("UX_EvidenceClusters_Type_Key");
        builder.HasIndex(entity => new { entity.ClusterType, entity.EventTimeUtc })
            .HasDatabaseName("IX_EvidenceClusters_Type_EventTimeUtc");
    }
}

internal sealed class EvidenceClusterMemberConfiguration : IEntityTypeConfiguration<EvidenceClusterMember>
{
    public void Configure(EntityTypeBuilder<EvidenceClusterMember> builder)
    {
        builder.ToTable("EvidenceClusterMembers");
        builder.HasKey(entity => new { entity.EvidenceClusterId, entity.EvidenceId });
        builder.Property(entity => entity.Role).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.HasOne<EvidenceCluster>().WithMany().HasForeignKey(entity => entity.EvidenceClusterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceRecord>().WithMany().HasForeignKey(entity => entity.EvidenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EvidenceId).HasDatabaseName("IX_EvidenceClusterMembers_EvidenceId");
    }
}

internal sealed class EvidenceQuarantineRecordConfiguration : IEntityTypeConfiguration<EvidenceQuarantineRecord>
{
    public void Configure(EntityTypeBuilder<EvidenceQuarantineRecord> builder)
    {
        builder.ToTable("EvidenceQuarantineRecords", table => table.HasCheckConstraint(
            "CK_EvidenceQuarantineRecords_MetadataJson",
            "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.EvidenceType).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SourceType).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.SourceKey).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.PayloadHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.OriginalTimestampUtc).IsUtcTimestamp();
        builder.Property(entity => entity.MetadataJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.QuarantinedAtUtc).IsUtcTimestamp();
        builder.HasIndex(entity => new { entity.SourceKey, entity.QuarantinedAtUtc })
            .HasDatabaseName("IX_EvidenceQuarantine_Source_Time");
        builder.HasIndex(entity => entity.PayloadHash).HasDatabaseName("IX_EvidenceQuarantine_PayloadHash");
    }
}

internal sealed class DataProviderConfiguration : IEntityTypeConfiguration<DataProvider>
{
    private static readonly Guid RetiredMt5ProviderId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid NewsDataProviderId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid FredProviderId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid AllTickProviderId = Guid.Parse("30000000-0000-0000-0000-000000000004");
    private static readonly Guid TwelveDataProviderId = Guid.Parse("30000000-0000-0000-0000-000000000005");
    private static readonly Guid AnalystRssProviderId = Guid.Parse("30000000-0000-0000-0000-000000000006");

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
                Id = RetiredMt5ProviderId,
                Key = "mt5",
                Name = "MetaTrader 5 (retired)",
                ProviderType = "Market",
                IsActive = false,
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
            },
            new DataProvider
            {
                Id = AnalystRssProviderId,
                Key = "analyst-rss",
                Name = "Configured Analyst RSS/Atom",
                ProviderType = "Analyst",
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
