using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace XauAi.Application.Analysts;

internal sealed class AnalystItemNormalizer(
    IAnalystRelevanceFilter relevanceFilter,
    AnalystSettings settings) : IAnalystItemNormalizer
{
    private static readonly string[] TrackingParameters =
        ["fbclid", "gclid", "mc_cid", "mc_eid", "ref", "referrer"];

    private static readonly string[] BullishTerms =
        ["bullish", "expects higher", "expected higher", "expects to rise", "expected to rise", "move higher", "rise toward", "rally toward", "upside"];

    private static readonly string[] BearishTerms =
        ["bearish", "expects lower", "expected lower", "expects to fall", "expected to fall", "move lower", "fall toward", "decline toward", "downside"];

    private static readonly string[] NeutralTerms =
        ["neutral", "range-bound", "range bound", "sideways", "consolidate"];

    private static readonly Regex RangeRegex = new(
        @"(?:target|range|between)\s*(?:is|of|near|:)?\s*\$?(?<low>\d{1,3}(?:,\d{3})+(?:\.\d+)?|\d{4,5}(?:\.\d+)?)\s*(?:-|–|—|to|and)\s*\$?(?<high>\d{1,3}(?:,\d{3})+(?:\.\d+)?|\d{4,5}(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex TargetRegex = new(
        @"(?:target|towards?|around|near|at)\s*(?:is|of|near|:)?\s*\$?(?<value>\d{1,3}(?:,\d{3})+(?:\.\d+)?|\d{4,5}(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex DurationRegex = new(
        @"(?<value>\d+)\s*(?<unit>day|days|week|weeks|month|months|year|years)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public AnalystNormalizationResult Normalize(ProviderAnalystItem item, DateTimeOffset collectedAtUtc)
    {
        var title = Collapse(item.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            return Rejected("MissingTitle");
        }

        var sourceName = Collapse(item.Source.Name);
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return Rejected("MissingSource");
        }

        if (!item.PublishedAtUtc.HasValue)
        {
            return Rejected("MissingPublishedTimestamp");
        }

        var publishedAtUtc = item.PublishedAtUtc.Value.ToUniversalTime();
        var collectedUtc = collectedAtUtc.ToUniversalTime();
        if (publishedAtUtc > collectedUtc.AddMinutes(10))
        {
            return Rejected("FuturePublishedTimestamp");
        }

        var relevance = relevanceFilter.Classify(item);
        if (!relevance.IsRelevant)
        {
            return Rejected("NotRelevantToXauUsd");
        }

        var sourceUrl = PreserveUrl(item.SourceUrl);
        var canonicalSourceUrl = NormalizeUrl(item.SourceUrl);
        var sourceWebsite = NormalizeUrl(item.Source.Website);
        var externalId = Limit(Collapse(item.ExternalId), 256);
        var summary = Limit(Collapse(item.Summary) ?? Collapse(item.PermittedContent), 4000);
        var category = Limit(Collapse(item.Category) ?? relevance.Categories.FirstOrDefault() ?? "Other", 100)!;
        var source = new NormalizedAnalystSource(
            Limit(Collapse(item.Source.ExternalId), 256),
            Hash(IdentityValue(item.Source.ExternalId, sourceWebsite, sourceName)),
            Limit(sourceName, 300)!,
            NormalizeSourceType(item.Source.Type),
            sourceWebsite,
            NormalizeCountryCode(item.Source.CountryCode));
        var analyst = NormalizeAnalyst(item.Analyst);
        var rawClaims = item.Claims.Count > 0
            ? item.Claims
            : [new ProviderAnalystClaim(
                null,
                summary ?? title,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                category)];
        var predictions = rawClaims.Select((claim, index) => NormalizeClaim(
                claim,
                index,
                externalId,
                analyst,
                title,
                summary,
                category))
            .ToArray();
        var contentHash = Hash(NormalizeComparable(string.Join('|', new[]
        {
            title,
            summary,
            string.Join('|', predictions.Select(prediction => prediction.ClaimText))
        }.Where(value => !string.IsNullOrWhiteSpace(value)))));
        var identityValue = !string.IsNullOrWhiteSpace(externalId)
            ? $"external:{externalId}"
            : canonicalSourceUrl is not null
                ? $"url:{canonicalSourceUrl}"
                : $"fallback:{source.IdentityHash}|{publishedAtUtc:O}|{NormalizeComparable(title)}|{contentHash}";

        return new AnalystNormalizationResult(
            new NormalizedAnalystItem(
                Guid.NewGuid(),
                settings.Provider,
                externalId,
                Hash(identityValue),
                Limit(title, 1000)!,
                summary,
                sourceUrl,
                canonicalSourceUrl is null ? null : Hash(canonicalSourceUrl),
                contentHash,
                publishedAtUtc,
                collectedUtc,
                item.UpdatedAtUtc?.ToUniversalTime(),
                NormalizeLanguage(item.Language),
                category,
                source,
                analyst,
                predictions),
            null);
    }

    private static NormalizedAnalystPrediction NormalizeClaim(
        ProviderAnalystClaim claim,
        int index,
        string? itemExternalId,
        NormalizedAnalystIdentity? analyst,
        string title,
        string? summary,
        string defaultCategory)
    {
        var claimText = Limit(Collapse(claim.Text), 4000);
        var extractionText = string.Join(' ', new[] { title, summary, claimText, claim.Reason }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var direction = ParseDirection(claim.Direction, extractionText);
        var (parsedTarget, parsedLow, parsedHigh) = ParseTargets(extractionText);
        var (parsedHorizonValue, parsedHorizonUnit, parsedTimeHorizon) = ParseHorizon(extractionText);
        var instrument = NormalizeInstrument(claim.Instrument, extractionText);
        var targetPrice = claim.TargetPrice ?? parsedTarget;
        var rangeLow = claim.TargetRangeLow ?? parsedLow;
        var rangeHigh = claim.TargetRangeHigh ?? parsedHigh;
        if (rangeLow.HasValue && rangeHigh.HasValue && rangeLow > rangeHigh)
        {
            (rangeLow, rangeHigh) = (rangeHigh, rangeLow);
        }

        var horizonUnit = ParseHorizonUnit(claim.HorizonUnit) ?? parsedHorizonUnit;
        var horizonValue = claim.HorizonValue ?? parsedHorizonValue;
        var timeHorizon = Limit(Collapse(claim.TimeHorizon) ?? parsedTimeHorizon, 100);
        var externalId = Limit(Collapse(claim.ExternalId)
            ?? (itemExternalId is null ? null : $"{itemExternalId}#claim-{index + 1}"), 256);
        var confidence = claim.Confidence is >= 0 and <= 100 ? claim.Confidence : null;
        var claimHash = Hash(NormalizeComparable(string.Join('|', new[]
        {
            analyst?.IdentityHash,
            instrument,
            direction.ToString(),
            targetPrice?.ToString(CultureInfo.InvariantCulture),
            rangeLow?.ToString(CultureInfo.InvariantCulture),
            rangeHigh?.ToString(CultureInfo.InvariantCulture),
            horizonValue?.ToString(CultureInfo.InvariantCulture),
            horizonUnit.ToString(),
            claimText,
            claim.Reason
        }.Where(value => !string.IsNullOrWhiteSpace(value)))));

        return new NormalizedAnalystPrediction(
            Guid.NewGuid(),
            externalId,
            instrument,
            NormalizeAssetClass(claim.AssetClass, instrument),
            direction,
            claimText,
            targetPrice,
            rangeLow,
            rangeHigh,
            Limit(Collapse(claim.TargetCurrency) ?? (IsGoldInstrument(instrument) ? "USD" : null), 3)?.ToUpperInvariant(),
            horizonValue,
            horizonUnit,
            timeHorizon,
            confidence,
            Limit(Collapse(claim.Reason), 2000),
            Limit(Collapse(claim.Category) ?? defaultCategory, 100)!,
            claimHash);
    }

    internal static AnalystDirection ParseDirection(string? supplied, string text)
    {
        if (Enum.TryParse<AnalystDirection>(Collapse(supplied), true, out var parsed))
        {
            return parsed;
        }

        var bullish = BullishTerms.Any(term => ContainsPhrase(text, term));
        var bearish = BearishTerms.Any(term => ContainsPhrase(text, term));
        var neutral = NeutralTerms.Any(term => ContainsPhrase(text, term));
        return (bullish, bearish, neutral) switch
        {
            (true, false, false) => AnalystDirection.Bullish,
            (false, true, false) => AnalystDirection.Bearish,
            (false, false, true) => AnalystDirection.Neutral,
            _ => AnalystDirection.Unknown
        };
    }

    internal static (decimal? Target, decimal? Low, decimal? High) ParseTargets(string text)
    {
        var range = RangeRegex.Match(text);
        if (range.Success
            && ParseDecimal(range.Groups["low"].Value) is { } low
            && ParseDecimal(range.Groups["high"].Value) is { } high)
        {
            return (null, Math.Min(low, high), Math.Max(low, high));
        }

        var target = TargetRegex.Match(text);
        return target.Success ? (ParseDecimal(target.Groups["value"].Value), null, null) : (null, null, null);
    }

    internal static (int? Value, AnalystHorizonUnit Unit, string? Raw) ParseHorizon(string text)
    {
        if (ContainsPhrase(text, "intraday"))
        {
            return (null, AnalystHorizonUnit.Intraday, "Intraday");
        }

        if (ContainsPhrase(text, "several days"))
        {
            return (null, AnalystHorizonUnit.Days, "Several days");
        }

        if (ContainsPhrase(text, "long term") || ContainsPhrase(text, "long-term"))
        {
            return (null, AnalystHorizonUnit.LongTerm, "Long term");
        }

        var match = DurationRegex.Match(text);
        if (!match.Success || !int.TryParse(match.Groups["value"].Value, out var value) || value <= 0)
        {
            return (null, AnalystHorizonUnit.Unknown, null);
        }

        var unit = match.Groups["unit"].Value.ToLowerInvariant() switch
        {
            "day" or "days" => AnalystHorizonUnit.Days,
            "week" or "weeks" => AnalystHorizonUnit.Weeks,
            "month" or "months" => AnalystHorizonUnit.Months,
            "year" or "years" => AnalystHorizonUnit.Years,
            _ => AnalystHorizonUnit.Unknown
        };
        return (value, unit, $"{value} {match.Groups["unit"].Value.ToLowerInvariant()}");
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

    private static string? PreserveUrl(string? value)
    {
        var collapsed = Collapse(value);
        return Uri.TryCreate(collapsed, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
                ? collapsed
                : null;
    }

    private static NormalizedAnalystIdentity? NormalizeAnalyst(ProviderAnalystIdentity? analyst)
    {
        var name = Collapse(analyst?.Name);
        if (name is null)
        {
            return null;
        }

        var profileUrl = NormalizeUrl(analyst?.ProfileUrl);
        var externalId = Limit(Collapse(analyst?.ExternalId), 256);
        return new NormalizedAnalystIdentity(
            externalId,
            Hash(IdentityValue(externalId, profileUrl, name)),
            Limit(name, 300)!,
            Limit(Collapse(analyst?.Role), 200),
            profileUrl);
    }

    private static string IdentityValue(string? externalId, string? url, string name) =>
        !string.IsNullOrWhiteSpace(externalId) ? $"external:{externalId}"
        : !string.IsNullOrWhiteSpace(url) ? $"url:{url}"
        : $"name:{NormalizeComparable(name)}";

    private static AnalystHorizonUnit? ParseHorizonUnit(string? value) =>
        Enum.TryParse<AnalystHorizonUnit>(Collapse(value), true, out var result) ? result : null;

    private static string NormalizeInstrument(string? value, string text)
    {
        var supplied = Collapse(value)?.ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            return Limit(supplied, 32)!;
        }

        if (new[] { "xauusd", "gold", "bullion", "xau", "precious metal" }.Any(term => ContainsPhrase(text, term)))
        {
            return "XAUUSD";
        }

        if (new[] { "treasury yield", "10-year yield", "10 year yield" }.Any(term => ContainsPhrase(text, term)))
        {
            return "US10Y";
        }

        if (new[] { "us dollar", "usd", "dxy", "dollar index" }.Any(term => ContainsPhrase(text, term)))
        {
            return "USD";
        }

        return "MACRO";
    }

    private static string NormalizeAssetClass(string? value, string instrument)
    {
        var supplied = Collapse(value);
        if (supplied is not null)
        {
            return Limit(supplied, 64)!;
        }

        return instrument switch
        {
            "XAUUSD" or "XAU" or "GOLD" => "Commodity",
            "USD" or "DXY" => "Currency",
            "US10Y" => "Rates",
            _ => "Macro"
        };
    }

    private static bool IsGoldInstrument(string instrument) =>
        instrument is "XAUUSD" or "XAU" or "GOLD";

    private static string NormalizeSourceType(string? value) => Collapse(value)?.ToLowerInvariant() switch
    {
        "bank" => "Bank",
        "broker" => "Broker",
        "research" => "Research",
        "publication" => "Publication",
        "independent analyst" or "independent" => "IndependentAnalyst",
        "institution" => "Institution",
        _ => "Other"
    };

    private static string? NormalizeCountryCode(string? value)
    {
        var code = Collapse(value)?.ToUpperInvariant();
        return code is { Length: 2 or 3 } ? code : null;
    }

    private static string NormalizeLanguage(string? value) => Collapse(value)?.ToLowerInvariant() switch
    {
        "english" or "en-us" or "en-gb" => "en",
        { Length: > 0 } language => language.Length <= 16 ? language : language[..16],
        _ => "und"
    };

    private static decimal? ParseDecimal(string value) =>
        decimal.TryParse(value.Replace(",", string.Empty, StringComparison.Ordinal), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static bool ContainsPhrase(string text, string phrase) =>
        Regex.IsMatch(
            text,
            $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(phrase)}(?![\p{{L}}\p{{N}}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

    private static AnalystNormalizationResult Rejected(string reason) => new(null, reason);

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

    private static string? Limit(string? value, int length) =>
        value is null || value.Length <= length ? value : value[..length];
}
