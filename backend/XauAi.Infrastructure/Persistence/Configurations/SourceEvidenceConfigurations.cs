using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Analysts;
using XauAi.Domain.EconomicData;
using XauAi.Domain.Evidence;
using XauAi.Domain.News;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("NewsArticles", table =>
        {
            table.HasCheckConstraint("CK_NewsArticles_CategoriesJson", "ISJSON([CategoriesJson]) = 1");
            table.HasCheckConstraint("CK_NewsArticles_ProviderCategoriesJson", "ISJSON([ProviderCategoriesJson]) = 1");
            table.HasCheckConstraint("CK_NewsArticles_CountryCodesJson", "ISJSON([CountryCodesJson]) = 1");
            table.HasCheckConstraint("CK_NewsArticles_EntitiesJson", "ISJSON([EntitiesJson]) = 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ProviderArticleId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.Title).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(4000);
        builder.Property(entity => entity.Author).HasMaxLength(300);
        builder.Property(entity => entity.SourceName).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.SourceUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.Publisher).HasMaxLength(300);
        builder.Property(entity => entity.CanonicalUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.CanonicalUrlHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(entity => entity.ContentHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(entity => entity.ImageUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.Language).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PrimaryCategory).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CategoriesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ProviderCategoriesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.CountryCodesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.EntitiesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.RelevanceLevel).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.RelevanceScore).HasPrecision(9, 6);
        builder.Property(entity => entity.PublishedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CollectedAtUtc).IsUtcTimestamp();

        builder.HasOne<EvidenceRecord>().WithOne().HasForeignKey<NewsArticle>(entity => entity.Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.DataProviderId, entity.ProviderArticleId })
            .IsUnique()
            .HasFilter("[ProviderArticleId] IS NOT NULL")
            .HasDatabaseName("UX_NewsArticles_Provider_ExternalId");
        builder.HasIndex(entity => entity.CanonicalUrlHash)
            .IsUnique()
            .HasFilter("[CanonicalUrlHash] IS NOT NULL")
            .HasDatabaseName("UX_NewsArticles_CanonicalUrlHash");
        builder.HasIndex(entity => entity.ContentHash)
            .IsUnique()
            .HasFilter("[ContentHash] IS NOT NULL")
            .HasDatabaseName("UX_NewsArticles_ContentHash");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.PublishedAtUtc })
            .HasDatabaseName("IX_NewsArticles_Instrument_PublishedAtUtc");
        builder.HasIndex(entity => entity.PublishedAtUtc)
            .HasDatabaseName("IX_NewsArticles_PublishedAtUtc");
        builder.HasIndex(entity => new { entity.PrimaryCategory, entity.PublishedAtUtc })
            .HasDatabaseName("IX_NewsArticles_Category_PublishedAtUtc");
        builder.HasIndex(entity => new { entity.RelevanceLevel, entity.PublishedAtUtc })
            .HasDatabaseName("IX_NewsArticles_Relevance_PublishedAtUtc");
        builder.HasIndex(entity => new { entity.SourceName, entity.PublishedAtUtc })
            .HasDatabaseName("IX_NewsArticles_Source_PublishedAtUtc");
    }
}

internal sealed class NewsCollectionStateConfiguration : IEntityTypeConfiguration<NewsCollectionState>
{
    public void Configure(EntityTypeBuilder<NewsCollectionState> builder)
    {
        builder.ToTable("NewsCollectionStates");
        builder.HasKey(entity => entity.DataProviderId);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.LastAttemptAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastSuccessfulCollectionAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastRequestedFromUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastRequestedToUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.LastErrorMessage).HasMaxLength(1000);
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<DataProvider>().WithOne().HasForeignKey<NewsCollectionState>(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.LastSuccessfulCollectionAtUtc)
            .HasDatabaseName("IX_NewsCollectionStates_LastSuccessful");
    }
}

internal sealed class NewsCollectionRunConfiguration : IEntityTypeConfiguration<NewsCollectionRun>
{
    public void Configure(EntityTypeBuilder<NewsCollectionRun> builder)
    {
        builder.ToTable("NewsCollectionRuns");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.RequestedFromUtc).IsUtcTimestamp();
        builder.Property(entity => entity.RequestedToUtc).IsUtcTimestamp();
        builder.Property(entity => entity.StartedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CompletedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(1000);
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.DataProviderId, entity.StartedAtUtc })
            .HasDatabaseName("IX_NewsCollectionRuns_Provider_StartedAtUtc");
        builder.HasIndex(entity => new { entity.Status, entity.StartedAtUtc })
            .HasDatabaseName("IX_NewsCollectionRuns_Status_StartedAtUtc");
    }
}

internal sealed class NewsArticleContentConfiguration : IEntityTypeConfiguration<NewsArticleContent>
{
    public void Configure(EntityTypeBuilder<NewsArticleContent> builder)
    {
        builder.ToTable("NewsArticleContents");
        builder.HasKey(entity => entity.NewsArticleId);
        builder.Property(entity => entity.PermittedContent).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.StoragePermission).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PermissionCheckedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<NewsArticle>().WithOne().HasForeignKey<NewsArticleContent>(entity => entity.NewsArticleId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EconomicEventConfiguration : IEntityTypeConfiguration<EconomicEvent>
{
    public void Configure(EntityTypeBuilder<EconomicEvent> builder)
    {
        builder.ToTable("EconomicEvents", table =>
            table.HasCheckConstraint("CK_EconomicEvents_RawValuesJson", "[RawValuesJson] IS NULL OR ISJSON([RawValuesJson]) = 1"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.Name).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.CountryCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Category).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Importance).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PreviousValue).HasPrecision(28, 8);
        builder.Property(entity => entity.ForecastValue).HasPrecision(28, 8);
        builder.Property(entity => entity.ActualValue).HasPrecision(28, 8);
        builder.Property(entity => entity.ValueUnit).HasMaxLength(40).IsUnicode(false);
        builder.Property(entity => entity.RawValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ScheduledAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.FetchedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();

        builder.HasOne<EvidenceRecord>().WithOne().HasForeignKey<EconomicEvent>(entity => entity.Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.DataProviderId, entity.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalId] IS NOT NULL")
            .HasDatabaseName("UX_EconomicEvents_Provider_ExternalId");
        builder.HasIndex(entity => new { entity.CurrencyCode, entity.ScheduledAtUtc })
            .HasDatabaseName("IX_EconomicEvents_Currency_ScheduledAtUtc");
        builder.HasIndex(entity => entity.ScheduledAtUtc)
            .HasDatabaseName("IX_EconomicEvents_ScheduledAtUtc");
    }
}

internal sealed class AnalystStatementConfiguration : IEntityTypeConfiguration<AnalystStatement>
{
    public void Configure(EntityTypeBuilder<AnalystStatement> builder)
    {
        builder.ToTable("AnalystStatements");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.AnalystName).HasMaxLength(300);
        builder.Property(entity => entity.Title).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.PermittedContent).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.SourceUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.SourceUrlHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(entity => entity.Direction).HasMaxLength(32).IsUnicode(false);
        builder.Property(entity => entity.TargetPrice).HasPrecision(19, 8);
        builder.Property(entity => entity.TimeHorizon).HasMaxLength(100);
        builder.Property(entity => entity.PublishedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.FetchedAtUtc).IsUtcTimestamp();

        builder.HasOne<EvidenceRecord>().WithOne().HasForeignKey<AnalystStatement>(entity => entity.Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(entity => entity.InstrumentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.DataProviderId, entity.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalId] IS NOT NULL")
            .HasDatabaseName("UX_AnalystStatements_Provider_ExternalId");
        builder.HasIndex(entity => entity.SourceUrlHash)
            .IsUnique()
            .HasFilter("[SourceUrlHash] IS NOT NULL")
            .HasDatabaseName("UX_AnalystStatements_SourceUrlHash");
        builder.HasIndex(entity => new { entity.InstrumentId, entity.PublishedAtUtc })
            .HasDatabaseName("IX_AnalystStatements_Instrument_PublishedAtUtc");
    }
}
