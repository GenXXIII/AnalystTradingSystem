namespace XauAi.Domain.News;

public sealed class NewsArticle
{
    public Guid Id { get; set; }

    public Guid DataProviderId { get; set; }

    public Guid? InstrumentId { get; set; }

    public string? ProviderArticleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Author { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? Publisher { get; set; }

    public string? CanonicalUrl { get; set; }

    public string? CanonicalUrlHash { get; set; }

    public string? ContentHash { get; set; }

    public string? ImageUrl { get; set; }

    public string Language { get; set; } = "und";

    public string PrimaryCategory { get; set; } = string.Empty;

    public string CategoriesJson { get; set; } = "[]";

    public string ProviderCategoriesJson { get; set; } = "[]";

    public string CountryCodesJson { get; set; } = "[]";

    public string EntitiesJson { get; set; } = "[]";

    public string RelevanceLevel { get; set; } = string.Empty;

    // Retained for schema compatibility only. Phase 7 does not populate or expose numeric relevance scores.
    public decimal? RelevanceScore { get; set; }

    public DateTimeOffset PublishedAtUtc { get; set; }

    public DateTimeOffset CollectedAtUtc { get; set; }
}

public sealed class NewsArticleContent
{
    public Guid NewsArticleId { get; set; }

    public string? PermittedContent { get; set; }

    public string StoragePermission { get; set; } = string.Empty;

    public DateTimeOffset? PermissionCheckedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
