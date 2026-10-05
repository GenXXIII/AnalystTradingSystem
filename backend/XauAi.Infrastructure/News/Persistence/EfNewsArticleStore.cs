using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.News;
using XauAi.Domain.Evidence;
using XauAi.Domain.News;
using XauAi.Infrastructure.Persistence;
using XauAi.Infrastructure.Evidence.Persistence;

namespace XauAi.Infrastructure.News.Persistence;

internal sealed class EfNewsArticleStore(
    XauAiDbContext context,
    NewsReferenceResolver references,
    NewsSettings settings) : INewsArticleStore
{
    public async Task<NewsPersistenceResult> PersistAsync(
        IReadOnlyList<NormalizedNewsArticle> articles,
        CancellationToken cancellationToken = default)
    {
        if (articles.Count == 0)
        {
            return new NewsPersistenceResult(0, 0);
        }

        var providerId = await references.ProviderIdAsync(settings.ProviderKey, cancellationToken);
        var instrumentId = await references.InstrumentIdAsync(settings.Symbol, cancellationToken);
        var unique = DeduplicateBatch(articles, out var inBatchDuplicates);
        var providerIds = unique.Where(article => article.ProviderArticleId is not null)
            .Select(article => article.ProviderArticleId!)
            .ToArray();
        var urlHashes = unique.Select(article => article.CanonicalUrlHash).ToArray();
        var contentHashes = unique.Select(article => article.ContentHash).ToArray();
        var existingProviderIds = providerIds.Length == 0
            ? []
            : await context.NewsArticles.AsNoTracking()
                .Where(article => article.DataProviderId == providerId
                    && article.ProviderArticleId != null
                    && providerIds.Contains(article.ProviderArticleId))
                .Select(article => article.ProviderArticleId!)
                .ToArrayAsync(cancellationToken);
        var existingUrlHashes = await context.NewsArticles.AsNoTracking()
            .Where(article => article.CanonicalUrlHash != null && urlHashes.Contains(article.CanonicalUrlHash))
            .Select(article => article.CanonicalUrlHash!)
            .ToArrayAsync(cancellationToken);
        var existingContentHashes = await context.NewsArticles.AsNoTracking()
            .Where(article => article.ContentHash != null && contentHashes.Contains(article.ContentHash))
            .Select(article => article.ContentHash!)
            .ToArrayAsync(cancellationToken);
        var providerSet = existingProviderIds.ToHashSet(StringComparer.Ordinal);
        var urlSet = existingUrlHashes.ToHashSet(StringComparer.Ordinal);
        var contentSet = existingContentHashes.ToHashSet(StringComparer.Ordinal);
        var insertable = unique.Where(article =>
                (article.ProviderArticleId is null || !providerSet.Contains(article.ProviderArticleId))
                && !urlSet.Contains(article.CanonicalUrlHash)
                && !contentSet.Contains(article.ContentHash))
            .ToArray();

        foreach (var article in insertable)
        {
            context.EvidenceRecords.Add(EvidenceRecordFactory.NewsArticle(
                article,
                providerId,
                instrumentId,
                settings.ProviderKey));
            context.NewsArticles.Add(new NewsArticle
            {
                Id = article.Id,
                DataProviderId = providerId,
                InstrumentId = instrumentId,
                ProviderArticleId = article.ProviderArticleId,
                Title = article.Title,
                Description = article.Description,
                Author = article.Author,
                SourceName = article.SourceName,
                SourceUrl = article.SourceUrl,
                Publisher = article.Publisher,
                CanonicalUrl = article.CanonicalUrl,
                CanonicalUrlHash = article.CanonicalUrlHash,
                ContentHash = article.ContentHash,
                ImageUrl = article.ImageUrl,
                Language = article.Language,
                PrimaryCategory = article.Categories.FirstOrDefault() ?? "Other",
                CategoriesJson = JsonSerializer.Serialize(article.Categories),
                ProviderCategoriesJson = JsonSerializer.Serialize(article.ProviderCategories),
                CountryCodesJson = JsonSerializer.Serialize(article.CountryCodes),
                EntitiesJson = JsonSerializer.Serialize(article.Entities),
                RelevanceLevel = article.Relevance.ToString(),
                PublishedAtUtc = article.PublishedAtUtc,
                CollectedAtUtc = article.CollectedAtUtc
            });
            if (!string.IsNullOrWhiteSpace(article.PermittedContent))
            {
                context.NewsArticleContents.Add(new NewsArticleContent
                {
                    NewsArticleId = article.Id,
                    PermittedContent = article.PermittedContent,
                    StoragePermission = "ProviderResponse",
                    PermissionCheckedAtUtc = article.CollectedAtUtc,
                    CreatedAtUtc = article.CollectedAtUtc
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return new NewsPersistenceResult(
            insertable.Length,
            inBatchDuplicates + unique.Count - insertable.Length);
    }

    private static IReadOnlyList<NormalizedNewsArticle> DeduplicateBatch(
        IReadOnlyList<NormalizedNewsArticle> articles,
        out int duplicateCount)
    {
        var providerIds = new HashSet<string>(StringComparer.Ordinal);
        var urlHashes = new HashSet<string>(StringComparer.Ordinal);
        var contentHashes = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<NormalizedNewsArticle>();
        foreach (var article in articles)
        {
            var duplicate = (article.ProviderArticleId is not null && providerIds.Contains(article.ProviderArticleId))
                || urlHashes.Contains(article.CanonicalUrlHash)
                || contentHashes.Contains(article.ContentHash);
            if (duplicate)
            {
                continue;
            }

            if (article.ProviderArticleId is not null)
            {
                providerIds.Add(article.ProviderArticleId);
            }

            urlHashes.Add(article.CanonicalUrlHash);
            contentHashes.Add(article.ContentHash);
            result.Add(article);
        }

        duplicateCount = articles.Count - result.Count;
        return result;
    }
}
