using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace XauAi.Application.News;

internal sealed class NewsArticleNormalizer(INewsRelevanceClassifier classifier) : INewsArticleNormalizer
{
    private static readonly string[] TrackingParameters =
        ["fbclid", "gclid", "mc_cid", "mc_eid", "ref", "referrer"];

    public NewsNormalizationResult Normalize(ProviderNewsArticle article, DateTimeOffset collectedAtUtc)
    {
        var title = Collapse(article.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            return Rejected("MissingTitle");
        }

        var sourceName = Collapse(article.SourceName);
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return Rejected("MissingSource");
        }

        if (!article.PublishedAtUtc.HasValue)
        {
            return Rejected("MissingPublishedTimestamp");
        }

        var publishedAtUtc = article.PublishedAtUtc.Value.ToUniversalTime();
        if (publishedAtUtc > collectedAtUtc.AddMinutes(10))
        {
            return Rejected("FuturePublishedTimestamp");
        }

        var canonicalUrl = NormalizeUrl(article.OriginalArticleUrl);
        if (canonicalUrl is null)
        {
            return Rejected("InvalidArticleUrl");
        }

        var classification = classifier.Classify(article);
        var normalizedSource = NormalizeComparable(sourceName);
        var normalizedTitle = NormalizeComparable(title);
        var fingerprint = Hash($"{normalizedSource}|{normalizedTitle}|{publishedAtUtc:yyyy-MM-dd}");
        var categories = classification.Categories.Count > 0
            ? classification.Categories
            : ["Other"];
        var author = article.Authors
            .Select(Collapse)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .DefaultIfEmpty()
            .Aggregate((left, right) => left is null ? right : right is null ? left : $"{left}, {right}");

        return new NewsNormalizationResult(
            new NormalizedNewsArticle(
                Guid.NewGuid(),
                "NewsData",
                Limit(Collapse(article.ProviderArticleId), 256),
                Limit(title, 1000)!,
                Limit(Collapse(article.Description), 4000),
                Collapse(article.PermittedContent),
                canonicalUrl,
                Hash(canonicalUrl),
                fingerprint,
                Limit(sourceName, 300)!,
                NormalizeUrl(article.SourceUrl),
                Limit(sourceName, 300),
                Limit(author, 300),
                publishedAtUtc,
                collectedAtUtc.ToUniversalTime(),
                NormalizeLanguage(article.Language),
                NormalizeCodes(article.CountryCodes),
                NormalizeValues(article.ProviderCategories),
                NormalizeValues(categories),
                NormalizeValues(classification.Entities),
                classification.Relevance,
                NormalizeUrl(article.ImageUrl),
                classification.Reasons),
            null);
    }

    internal static string? NormalizeUrl(string? value)
    {
        if (!Uri.TryCreate(Collapse(value), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            return null;
        }

        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length > 0)
            .Where(pair => !pair[0].StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
            .Where(pair => !TrackingParameters.Contains(pair[0], StringComparer.OrdinalIgnoreCase))
            .OrderBy(pair => pair[0], StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Length > 1 ? pair[1] : string.Empty, StringComparer.Ordinal)
            .Select(pair => string.Join('=', pair));
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty,
            Query = string.Join('&', query)
        };
        if ((builder.Scheme == "https" && builder.Port == 443)
            || (builder.Scheme == "http" && builder.Port == 80))
        {
            builder.Port = -1;
        }

        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }

    private static NewsNormalizationResult Rejected(string reason) => new(null, reason);

    private static string? Collapse(string? value) => string.IsNullOrWhiteSpace(value)
        ? null
        : Regex.Replace(value.Trim(), @"\s+", " ", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static string NormalizeComparable(string value) =>
        new(value.Normalize(NormalizationForm.FormKC)
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string NormalizeLanguage(string? value) => (value?.Trim().ToLowerInvariant()) switch
    {
        "english" or "en-us" or "en-gb" => "en",
        { Length: > 0 } language => language.Length <= 16 ? language : language[..16],
        _ => "und"
    };

    private static IReadOnlyList<string> NormalizeCodes(IReadOnlyList<string> values) =>
        [.. values.Select(value => value.Trim().ToUpperInvariant())
            .Where(value => value.Length is > 0 and <= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];

    private static IReadOnlyList<string> NormalizeValues(IEnumerable<string> values) =>
        [.. values.Select(value => Collapse(value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];

    private static string? Limit(string? value, int length) =>
        value is null || value.Length <= length ? value : value[..length];
}
