using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.News;
using XauAi.Domain.News;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.News.Persistence;

internal sealed class EfNewsQueryStore(XauAiDbContext context) : INewsQueryStore
{
    public async Task<PagedNewsArticles> QueryAsync(
        NewsArticleQuery query,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var source = context.NewsArticles.AsNoTracking().AsQueryable();
        if (query.FromUtc.HasValue)
        {
            source = source.Where(article => article.PublishedAtUtc >= query.FromUtc.Value.ToUniversalTime());
        }

        if (query.ToUtc.HasValue)
        {
            source = source.Where(article => article.PublishedAtUtc <= query.ToUtc.Value.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = $"\"{query.Category.Trim()}\"";
            source = source.Where(article => article.CategoriesJson.Contains(category));
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var sourceName = query.Source.Trim();
            source = source.Where(article => article.SourceName == sourceName);
        }

        if (query.MinimumRelevance.HasValue)
        {
            var allowed = Enum.GetValues<NewsRelevanceLevel>()
                .Where(level => level >= query.MinimumRelevance.Value)
                .Select(level => level.ToString())
                .ToArray();
            source = source.Where(article => allowed.Contains(article.RelevanceLevel));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(article => article.Title.Contains(search)
                || (article.Description != null && article.Description.Contains(search)));
        }

        var total = await source.CountAsync(cancellationToken);
        var rows = await source
            .OrderByDescending(article => article.PublishedAtUtc)
            .ThenByDescending(article => article.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .GroupJoin(
                context.NewsArticleContents.AsNoTracking(),
                article => article.Id,
                content => content.NewsArticleId,
                (article, content) => new { Article = article, Content = content.FirstOrDefault() })
            .ToArrayAsync(cancellationToken);
        var items = rows.Select(row => Map(row.Article, row.Content?.PermittedContent, nowUtc)).ToArray();
        return new PagedNewsArticles(
            items,
            query.Page,
            query.PageSize,
            total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<NewsArticleResult?> GetByIdAsync(
        Guid id,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var row = await context.NewsArticles.AsNoTracking()
            .Where(article => article.Id == id)
            .GroupJoin(
                context.NewsArticleContents.AsNoTracking(),
                article => article.Id,
                content => content.NewsArticleId,
                (article, content) => new { Article = article, Content = content.FirstOrDefault() })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row.Article, row.Content?.PermittedContent, nowUtc);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.NewsArticles.AsNoTracking().CountAsync(cancellationToken);

    private static NewsArticleResult Map(
        NewsArticle article,
        string? summary,
        DateTimeOffset nowUtc)
    {
        var age = nowUtc - article.PublishedAtUtc;
        var freshness = age <= TimeSpan.FromHours(1) ? NewsFreshness.VeryRecent
            : age <= TimeSpan.FromHours(24) ? NewsFreshness.Recent
            : age <= TimeSpan.FromDays(7) ? NewsFreshness.Older
            : NewsFreshness.Historical;
        return new NewsArticleResult(
            article.Id,
            "NewsData",
            article.ProviderArticleId,
            article.Title,
            article.Description,
            summary,
            article.CanonicalUrl ?? string.Empty,
            article.SourceName,
            article.SourceUrl,
            article.Publisher,
            article.Author,
            article.PublishedAtUtc,
            article.CollectedAtUtc,
            article.Language,
            Deserialize(article.CountryCodesJson),
            Deserialize(article.CategoriesJson),
            Deserialize(article.EntitiesJson),
            Enum.TryParse<NewsRelevanceLevel>(article.RelevanceLevel, out var relevance)
                ? relevance
                : NewsRelevanceLevel.Irrelevant,
            freshness,
            article.ImageUrl);
    }

    private static IReadOnlyList<string> Deserialize(string value) =>
        JsonSerializer.Deserialize<string[]>(value) ?? [];
}
