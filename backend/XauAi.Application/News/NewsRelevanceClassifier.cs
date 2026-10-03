using System.Text.RegularExpressions;

namespace XauAi.Application.News;

internal sealed class NewsRelevanceClassifier(NewsSettings settings) : INewsRelevanceClassifier
{
    private readonly IReadOnlyList<KeywordGroup> _groups =
    [
        new("Gold", settings.GoldKeywords),
        new("USD", settings.UsdKeywords),
        new("FederalReserve", settings.FedKeywords),
        new("Inflation", settings.InflationKeywords),
        new("Employment", settings.EmploymentKeywords),
        new("InterestRates", settings.RatesKeywords),
        new("Economy", settings.EconomyKeywords),
        new("CentralBank", settings.CentralBankKeywords),
        new("Geopolitics", settings.GeopoliticsKeywords),
        new("Commodity", settings.CommodityKeywords)
    ];

    public NewsClassification Classify(ProviderNewsArticle article)
    {
        var title = article.Title ?? string.Empty;
        var supportingText = string.Join(' ', new[]
        {
            article.Description,
            article.PermittedContent,
            string.Join(' ', article.ProviderKeywords),
            string.Join(' ', article.ProviderCategories)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var titleMatches = _groups.Where(group => Matches(title, group.Keywords)).ToArray();
        var supportingMatches = _groups.Where(group => Matches(supportingText, group.Keywords)).ToArray();
        var categories = titleMatches.Concat(supportingMatches)
            .Select(group => group.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var providerContext = article.ProviderCategories.Any(category =>
            category.Equals("business", StringComparison.OrdinalIgnoreCase)
            || category.Equals("politics", StringComparison.OrdinalIgnoreCase)
            || category.Equals("world", StringComparison.OrdinalIgnoreCase)
            || category.Equals("environment", StringComparison.OrdinalIgnoreCase));

        var titleHasGold = titleMatches.Any(group => group.Category == "Gold");
        var titleHasFedAndRates = titleMatches.Any(group => group.Category == "FederalReserve")
            && titleMatches.Any(group => group.Category == "InterestRates");
        var highImpactTitle = titleMatches.Any(group => group.Category is
            "USD" or "FederalReserve" or "Inflation" or "Employment" or "InterestRates");
        var supportingHasGold = supportingMatches.Any(group => group.Category == "Gold");
        var relevance = titleHasGold || titleHasFedAndRates
            ? NewsRelevanceLevel.VeryHigh
            : highImpactTitle || supportingHasGold
                ? NewsRelevanceLevel.High
                : categories.Length > 0
                    ? NewsRelevanceLevel.Medium
                    : providerContext
                        ? NewsRelevanceLevel.Low
                        : NewsRelevanceLevel.Irrelevant;
        var reasons = new List<string>();
        reasons.AddRange(titleMatches.Select(group => $"Title:{group.Category}"));
        reasons.AddRange(supportingMatches
            .Where(group => !titleMatches.Contains(group))
            .Select(group => $"SupportingText:{group.Category}"));
        if (providerContext && categories.Length == 0)
        {
            reasons.Add("ProviderCategory:GeneralMacroContext");
        }

        return new NewsClassification(
            relevance,
            categories,
            Entities(title, supportingText),
            reasons);
    }

    private static bool Matches(string text, IReadOnlyList<string> keywords) =>
        keywords.Any(keyword => ContainsTerm(text, keyword));

    private static bool ContainsTerm(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        return Regex.IsMatch(
            text,
            $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(term)}(?![\p{{L}}\p{{N}}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }

    private static IReadOnlyList<string> Entities(string title, string supportingText)
    {
        var text = $"{title} {supportingText}";
        var candidates = new (string Entity, string[] Terms)[]
        {
            ("Gold", ["gold", "bullion", "xau", "xauusd"]),
            ("USD", ["usd", "us dollar", "dxy", "greenback"]),
            ("Federal Reserve", ["federal reserve", "fed chair"]),
            ("Jerome Powell", ["jerome powell", "powell"]),
            ("FOMC", ["fomc"]),
            ("CPI", ["cpi", "consumer price index"]),
            ("PCE", ["pce", "personal consumption expenditures"]),
            ("NFP", ["nfp", "nonfarm payroll", "non-farm payroll"]),
            ("US Treasury", ["treasury yield", "us treasury"]),
            ("ECB", ["ecb", "european central bank"]),
            ("BOJ", ["boj", "bank of japan"]),
            ("China", ["china", "chinese"])
        };
        return [.. candidates
            .Where(candidate => candidate.Terms.Any(term => ContainsTerm(text, term)))
            .Select(candidate => candidate.Entity)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private sealed record KeywordGroup(string Category, IReadOnlyList<string> Keywords);
}
