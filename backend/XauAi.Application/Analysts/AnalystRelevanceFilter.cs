using System.Text.RegularExpressions;

namespace XauAi.Application.Analysts;

internal sealed class AnalystRelevanceFilter(AnalystSettings settings) : IAnalystRelevanceFilter
{
    public AnalystRelevance Classify(ProviderAnalystItem item)
    {
        var title = item.Title ?? string.Empty;
        var supporting = string.Join(' ', new[]
        {
            item.Summary,
            item.PermittedContent,
            string.Join(' ', item.Claims.Select(claim => claim.Text)),
            string.Join(' ', item.Claims.Select(claim => claim.Reason)),
            string.Join(' ', item.Claims.Select(claim => claim.Instrument))
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var titleMatches = settings.RelevanceKeywords
            .Where(keyword => ContainsTerm(title, keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var supportingMatches = settings.RelevanceKeywords
            .Where(keyword => ContainsTerm(supporting, keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var matches = titleMatches.Concat(supportingMatches)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var categories = Categories(matches);
        var reasons = titleMatches.Select(match => $"Title:{match}")
            .Concat(supportingMatches.Select(match => $"SupportingText:{match}"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new AnalystRelevance(matches.Length > 0, categories, reasons);
    }

    private static IReadOnlyList<string> Categories(IEnumerable<string> matches)
    {
        var values = matches.Select(match => match.ToLowerInvariant()).ToArray();
        var result = new List<string>();
        AddIf(result, values, "Gold", "xauusd", "gold", "xau", "bullion", "precious metals");
        AddIf(result, values, "USD", "us dollar", "usd", "dxy");
        AddIf(result, values, "FederalReserve", "federal reserve", "fed", "fomc");
        AddIf(result, values, "Inflation", "inflation", "cpi", "pce");
        AddIf(result, values, "Employment", "nfp");
        AddIf(result, values, "InterestRates", "treasury yield", "interest rate");
        AddIf(result, values, "Geopolitics", "geopolitical");
        return result.Count == 0 ? ["Other"] : result;
    }

    private static void AddIf(List<string> result, IReadOnlyList<string> values, string category, params string[] terms)
    {
        if (values.Any(value => terms.Contains(value, StringComparer.OrdinalIgnoreCase)))
        {
            result.Add(category);
        }
    }

    private static bool ContainsTerm(string text, string term) =>
        !string.IsNullOrWhiteSpace(text)
        && !string.IsNullOrWhiteSpace(term)
        && Regex.IsMatch(
            text,
            $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(term)}(?![\p{{L}}\p{{N}}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
}
