using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XauAi.Domain.Analysts;
using XauAi.Domain.ReferenceData;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal sealed class AnalystSourceConfiguration : IEntityTypeConfiguration<AnalystSource>
{
    public void Configure(EntityTypeBuilder<AnalystSource> builder)
    {
        builder.ToTable("AnalystSources");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.IdentityHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.Type).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Website).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.CountryCode).HasMaxLength(3).IsUnicode(false);
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.DataProviderId, entity.IdentityHash })
            .IsUnique()
            .HasDatabaseName("UX_AnalystSources_Provider_IdentityHash");
        builder.HasIndex(entity => new { entity.Type, entity.IsActive })
            .HasDatabaseName("IX_AnalystSources_Type_Active");
    }
}

internal sealed class AnalystConfiguration : IEntityTypeConfiguration<Analyst>
{
    public void Configure(EntityTypeBuilder<Analyst> builder)
    {
        builder.ToTable("Analysts");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.IdentityHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.Role).HasMaxLength(200);
        builder.Property(entity => entity.ProfileUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<AnalystSource>().WithMany().HasForeignKey(entity => entity.AnalystSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.AnalystSourceId, entity.IdentityHash })
            .IsUnique()
            .HasDatabaseName("UX_Analysts_Source_IdentityHash");
        builder.HasIndex(entity => new { entity.AnalystSourceId, entity.IsActive })
            .HasDatabaseName("IX_Analysts_Source_Active");
    }
}

internal sealed class AnalystPublicationConfiguration : IEntityTypeConfiguration<AnalystPublication>
{
    public void Configure(EntityTypeBuilder<AnalystPublication> builder)
    {
        builder.ToTable("AnalystPublications", table =>
        {
            table.HasCheckConstraint("CK_AnalystPublications_Version", "[Version] >= 1");
            table.HasCheckConstraint(
                "CK_AnalystPublications_RelationshipType",
                "[RelationshipType] IN ('Original', 'Revision', 'Republished', 'Related')");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.IdentityHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Summary).HasMaxLength(4000);
        builder.Property(entity => entity.SourceUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(entity => entity.SourceUrlHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(entity => entity.ContentHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Language).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Category).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.RelationshipType).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.PublishedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CollectedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.ProviderUpdatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.CreatedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<DataProvider>().WithMany().HasForeignKey(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AnalystSource>().WithMany().HasForeignKey(entity => entity.AnalystSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AnalystPublication>().WithMany().HasForeignKey(entity => entity.OriginalPublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AnalystPublication>().WithMany().HasForeignKey(entity => entity.RelatedPublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.DataProviderId, entity.IdentityHash, entity.Version })
            .IsUnique()
            .HasDatabaseName("UX_AnalystPublications_Provider_Identity_Version");
        builder.HasIndex(entity => entity.ContentHash)
            .HasDatabaseName("IX_AnalystPublications_ContentHash");
        builder.HasIndex(entity => entity.SourceUrlHash)
            .HasFilter("[SourceUrlHash] IS NOT NULL")
            .HasDatabaseName("IX_AnalystPublications_SourceUrlHash");
        builder.HasIndex(entity => new { entity.AnalystSourceId, entity.PublishedAtUtc })
            .HasDatabaseName("IX_AnalystPublications_Source_PublishedAtUtc");
        builder.HasIndex(entity => new { entity.DataProviderId, entity.PublishedAtUtc })
            .HasDatabaseName("IX_AnalystPublications_Provider_PublishedAtUtc");
    }
}

internal sealed class AnalystSyncStateConfiguration : IEntityTypeConfiguration<AnalystSyncState>
{
    public void Configure(EntityTypeBuilder<AnalystSyncState> builder)
    {
        builder.ToTable("AnalystSyncStates");
        builder.HasKey(entity => entity.DataProviderId);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.LastAttemptAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastSuccessfulSyncAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastPublishedAtUtc).IsUtcTimestamp();
        builder.Property(entity => entity.LastExternalId).HasMaxLength(256).IsUnicode(false);
        builder.Property(entity => entity.LastErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.LastErrorMessage).HasMaxLength(1000);
        builder.Property(entity => entity.UpdatedAtUtc).IsUtcTimestamp();
        builder.HasOne<DataProvider>().WithOne().HasForeignKey<AnalystSyncState>(entity => entity.DataProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.LastSuccessfulSyncAtUtc)
            .HasDatabaseName("IX_AnalystSyncStates_LastSuccessful");
    }
}

internal sealed class AnalystSyncRunConfiguration : IEntityTypeConfiguration<AnalystSyncRun>
{
    public void Configure(EntityTypeBuilder<AnalystSyncRun> builder)
    {
        builder.ToTable("AnalystSyncRuns");
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
            .HasDatabaseName("IX_AnalystSyncRuns_Provider_StartedAtUtc");
        builder.HasIndex(entity => new { entity.Status, entity.StartedAtUtc })
            .HasDatabaseName("IX_AnalystSyncRuns_Status_StartedAtUtc");
    }
}
