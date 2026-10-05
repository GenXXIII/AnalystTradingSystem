using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XauAi.Application.Evidence;

internal sealed class EvidenceNormalizer : IEvidenceNormalizer
{
    public EvidenceNormalizationResult Normalize(EvidenceInput input, DateTimeOffset normalizedAtUtc)
    {
        var payloadHash = EvidenceNormalization.Hash(
            input.SourceKey,
            input.ExternalId,
            input.Instrument,
            input.EventTime.ToString("O", CultureInfo.InvariantCulture),
            input.Title,
            input.Summary,
            input.OriginalValue,
            input.MetadataJson);
        var error = Validate(input, normalizedAtUtc);
        if (error is not null)
        {
            return new EvidenceNormalizationResult(null, error.Value.Code, error.Value.Message, payloadHash);
        }

        var canonicalSymbol = EvidenceNormalization.NormalizeInstrument(input.Instrument)!;
        var timeframe = EvidenceNormalization.NormalizeTimeframe(input.Timeframe);
        var direction = EvidenceNormalization.NormalizeDirection(input.Direction);
        var importance = EvidenceNormalization.NormalizeImportance(input.Importance);
        var category = EvidenceNormalization.NormalizeCategory(input.Category);
        var unit = EvidenceNormalization.NormalizeUnit(input.Unit);
        var eventTime = input.EventTime.ToUniversalTime();
        var availableAt = input.AvailableAt.ToUniversalTime();
        var publishedAt = input.PublishedAt?.ToUniversalTime();
        var collectedAt = input.CollectedAt?.ToUniversalTime();
        var validFrom = (input.ValidFrom ?? input.AvailableAt).ToUniversalTime();
        var validTo = input.ValidTo?.ToUniversalTime();
        var normalizedTitle = EvidenceNormalization.Collapse(input.Title);
        var normalizedSummary = EvidenceNormalization.Collapse(input.Summary);
        var sourceKey = input.SourceKey.Trim().ToLowerInvariant();
        var externalId = EvidenceNormalization.Collapse(input.ExternalId);
        var contentHash = EvidenceNormalization.Hash(
            canonicalSymbol,
            timeframe,
            eventTime.ToString("O", CultureInfo.InvariantCulture),
            normalizedTitle,
            normalizedSummary,
            input.Value?.ToString(CultureInfo.InvariantCulture),
            unit.ToString(),
            direction.ToString(),
            category.ToString());
        var identityHash = externalId is not null
            ? EvidenceNormalization.Hash(sourceKey, externalId)
            : EvidenceNormalization.Hash(sourceKey, canonicalSymbol, eventTime.ToString("O", CultureInfo.InvariantCulture), contentHash);
        var completeness = DetermineCompleteness(input, externalId);
        var quality = DetermineQuality(input, completeness, externalId);
        var (relevant, relevanceReason) = EvidenceNormalization.ClassifyRelevance(
            canonicalSymbol,
            normalizedTitle,
            normalizedSummary,
            category,
            input.MetadataJson);
        var clusterKey = string.IsNullOrWhiteSpace(input.ClusterKey)
            ? null
            : EvidenceNormalization.Hash(input.ClusterType?.ToString(), input.ClusterKey);

        return new EvidenceNormalizationResult(
            new NormalizedEvidenceItem(
                Guid.NewGuid(),
                input.EvidenceType,
                input.SourceType,
                sourceKey,
                externalId,
                identityHash,
                contentHash,
                canonicalSymbol,
                EvidenceNormalization.Collapse(input.OriginalInstrument) ?? input.Instrument.Trim(),
                timeframe,
                eventTime,
                availableAt,
                publishedAt,
                collectedAt,
                validFrom,
                validTo,
                normalizedTitle,
                normalizedSummary,
                input.Value,
                EvidenceNormalization.Collapse(input.OriginalValue),
                unit,
                EvidenceNormalization.Collapse(input.Unit),
                direction,
                EvidenceNormalization.Collapse(input.Direction),
                importance,
                EvidenceNormalization.Collapse(input.Importance),
                category,
                EvidenceNormalization.Collapse(input.Category),
                NormalizeCurrency(input.CurrencyCode),
                EvidenceNormalization.Collapse(input.OriginalSourceUrl),
                quality,
                completeness,
                input.TimestampQuality,
                input.SourceReliability,
                relevant,
                relevanceReason,
                EvidenceNormalization.Collapse(input.MetadataJson),
                input.ClusterType,
                clusterKey,
                normalizedAtUtc.ToUniversalTime()),
            null,
            null,
            payloadHash);
    }

    private static (string Code, string Message)? Validate(EvidenceInput input, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(input.SourceKey) || input.SourceKey.Trim().Length > 100)
        {
            return ("SOURCE_INVALID", "Evidence requires a source key of at most 100 characters.");
        }

        if (EvidenceNormalization.NormalizeInstrument(input.Instrument) is null)
        {
            return ("INSTRUMENT_INVALID", "Evidence requires a supported canonical XAUUSD instrument alias.");
        }

        if (input.EventTime == default || input.AvailableAt == default)
        {
            return ("TIMESTAMP_MISSING", "Evidence requires event and availability timestamps.");
        }

        if (input.AvailableAt.ToUniversalTime() > now.ToUniversalTime())
        {
            return ("FUTURE_AVAILABILITY", "Evidence cannot be ingested before its availability timestamp.");
        }

        var normalizedTimeframe = EvidenceNormalization.NormalizeTimeframe(input.Timeframe);
        if (!string.IsNullOrWhiteSpace(input.Timeframe) && normalizedTimeframe is null)
        {
            return ("TIMEFRAME_INVALID", "Evidence contains an unsupported timeframe.");
        }

        if (RequiresTimeframe(input.EvidenceType) && normalizedTimeframe is null)
        {
            return ("TIMEFRAME_MISSING", "Market-related evidence requires a supported timeframe.");
        }

        if (input.ValidTo.HasValue
            && input.ValidTo.Value.ToUniversalTime() <= (input.ValidFrom ?? input.AvailableAt).ToUniversalTime())
        {
            return ("VALIDITY_INVALID", "Evidence validity end must be after its validity start.");
        }

        if (!string.IsNullOrWhiteSpace(input.OriginalSourceUrl)
            && (!Uri.TryCreate(input.OriginalSourceUrl, UriKind.Absolute, out var sourceUri)
                || sourceUri.Scheme is not ("http" or "https")))
        {
            return ("SOURCE_URL_INVALID", "Evidence source URL must be an absolute HTTP or HTTPS URL.");
        }

        if (!string.IsNullOrWhiteSpace(input.CurrencyCode)
            && !Regex.IsMatch(input.CurrencyCode.Trim(), "^[A-Za-z]{3}$", RegexOptions.CultureInvariant))
        {
            return ("CURRENCY_INVALID", "Evidence currency must be a three-letter ISO-style code.");
        }

        if (!string.IsNullOrWhiteSpace(input.MetadataJson))
        {
            try
            {
                using var _ = JsonDocument.Parse(input.MetadataJson);
            }
            catch (JsonException)
            {
                return ("METADATA_INVALID", "Evidence metadata must be valid JSON.");
            }
        }

        if (string.IsNullOrWhiteSpace(input.Title)
            && string.IsNullOrWhiteSpace(input.Summary)
            && input.Value is null
            && string.IsNullOrWhiteSpace(input.MetadataJson))
        {
            return ("PAYLOAD_MISSING", "Evidence requires a title, summary, numeric value, or structured metadata.");
        }

        return null;
    }

    private static bool RequiresTimeframe(EvidenceType type) => type is
        EvidenceType.Market
        or EvidenceType.Technical
        or EvidenceType.Candle
        or EvidenceType.CandleFlow
        or EvidenceType.Liquidity
        or EvidenceType.OrderFlow;

    private static EvidenceCompleteness DetermineCompleteness(EvidenceInput input, string? externalId)
    {
        var hasPayload = !string.IsNullOrWhiteSpace(input.Title)
            || !string.IsNullOrWhiteSpace(input.Summary)
            || input.Value.HasValue
            || !string.IsNullOrWhiteSpace(input.MetadataJson);
        return hasPayload && externalId is not null
            ? EvidenceCompleteness.Complete
            : EvidenceCompleteness.Partial;
    }

    private static EvidenceQuality DetermineQuality(
        EvidenceInput input,
        EvidenceCompleteness completeness,
        string? externalId)
    {
        if (completeness == EvidenceCompleteness.Complete
            && externalId is not null
            && input.TimestampQuality == EvidenceTimestampQuality.Exact
            && input.SourceReliability == EvidenceSourceReliability.Known)
        {
            return EvidenceQuality.High;
        }

        return completeness == EvidenceCompleteness.Complete
            || input.TimestampQuality is EvidenceTimestampQuality.Exact or EvidenceTimestampQuality.DateOnly
                ? EvidenceQuality.Medium
                : EvidenceQuality.Low;
    }

    private static string? NormalizeCurrency(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

public static class EvidenceNormalization
{
    private static readonly HashSet<string> Timeframes =
        new(StringComparer.OrdinalIgnoreCase) { "M1", "M5", "M15", "M30", "H1", "H4", "D1", "W1", "MN1" };

    public static string? NormalizeInstrument(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compact = Regex.Replace(value.Trim().ToUpperInvariant(), "[^A-Z0-9]", string.Empty);
        return compact is "XAUUSD" or "GOLD" or "GOLDUSD" or "XAU" ? "XAUUSD" : null;
    }

    public static string? NormalizeTimeframe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compact = value.Trim().ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
        compact = compact switch
        {
            "1M" or "1MIN" or "1MINUTE" => "M1",
            "5M" or "5MIN" or "5MINUTES" => "M5",
            "15M" or "15MIN" or "15MINUTES" => "M15",
            "30M" or "30MIN" or "30MINUTES" => "M30",
            "1H" or "1HOUR" => "H1",
            "4H" or "4HOURS" => "H4",
            "1D" or "DAILY" => "D1",
            "1W" or "WEEKLY" => "W1",
            "1MO" or "MONTHLY" => "MN1",
            _ => compact
        };
        return Timeframes.Contains(compact) ? compact : null;
    }

    public static EvidenceDirection NormalizeDirection(string? value)
    {
        var normalized = Collapse(value)?.ToUpperInvariant();
        return normalized switch
        {
            "BUY" or "LONG" or "BULLISH" or "UP" or "POSITIVE" => EvidenceDirection.Bullish,
            "SELL" or "SHORT" or "BEARISH" or "DOWN" or "NEGATIVE" => EvidenceDirection.Bearish,
            "NEUTRAL" or "SIDEWAYS" or "RANGE" or "RANGE-BOUND" => EvidenceDirection.Neutral,
            _ => EvidenceDirection.Unknown
        };
    }

    public static EvidenceImportance NormalizeImportance(string? value)
    {
        var normalized = Collapse(value)?.ToUpperInvariant();
        return normalized switch
        {
            "1" or "LOW" or "MINOR" => EvidenceImportance.Low,
            "2" or "MEDIUM" or "MODERATE" => EvidenceImportance.Medium,
            "3" or "HIGH" or "IMPORTANT" => EvidenceImportance.High,
            "4" or "CRITICAL" or "SEVERE" => EvidenceImportance.Critical,
            _ => EvidenceImportance.Unknown
        };
    }

    public static EvidenceCategory NormalizeCategory(string? value)
    {
        var normalized = Collapse(value)?.ToUpperInvariant() ?? string.Empty;
        if (ContainsAny(normalized, "FOMC", "FEDERAL RESERVE", "FED")) return EvidenceCategory.Fed;
        if (ContainsAny(normalized, "INTEREST RATE", "FED FUNDS", "RATE")) return EvidenceCategory.InterestRates;
        if (ContainsAny(normalized, "CPI", "PCE", "INFLATION")) return EvidenceCategory.Inflation;
        if (ContainsAny(normalized, "NFP", "PAYROLL", "EMPLOYMENT", "UNEMPLOYMENT")) return EvidenceCategory.Employment;
        if (ContainsAny(normalized, "GDP", "GROWTH")) return EvidenceCategory.Gdp;
        if (ContainsAny(normalized, "USD", "DOLLAR", "DXY")) return EvidenceCategory.Usd;
        if (ContainsAny(normalized, "TREASURY", "YIELD")) return EvidenceCategory.Treasury;
        if (ContainsAny(normalized, "GEOPOLITICAL", "WAR", "CONFLICT")) return EvidenceCategory.Geopolitical;
        if (ContainsAny(normalized, "CENTRAL BANK")) return EvidenceCategory.CentralBank;
        if (ContainsAny(normalized, "GOLD", "XAU", "COMMODITY", "PRECIOUS METAL")) return EvidenceCategory.Commodity;
        if (ContainsAny(normalized, "MARKET STRUCTURE", "STRUCTURE")) return EvidenceCategory.MarketStructure;
        if (ContainsAny(normalized, "LIQUIDITY", "SWEEP")) return EvidenceCategory.Liquidity;
        if (ContainsAny(normalized, "TECHNICAL", "INDICATOR", "CANDLE")) return EvidenceCategory.Technical;
        if (ContainsAny(normalized, "SESSION", "LONDON", "NEW YORK", "ASIA")) return EvidenceCategory.Session;
        return EvidenceCategory.Other;
    }

    public static EvidenceUnit NormalizeUnit(string? value)
    {
        var normalized = Collapse(value)?.ToUpperInvariant();
        return normalized switch
        {
            "%" or "PERCENT" or "PERCENTAGE" => EvidenceUnit.Percent,
            "USD" or "US DOLLAR" => EvidenceUnit.Usd,
            "USD BILLION" or "USD BILLIONS" or "BILLIONS OF DOLLARS" => EvidenceUnit.UsdBillions,
            "USD TRILLION" or "USD TRILLIONS" or "TRILLIONS OF DOLLARS" => EvidenceUnit.UsdTrillions,
            "INDEX" or "INDEX POINTS" => EvidenceUnit.Index,
            "THOUSAND" or "THOUSANDS" => EvidenceUnit.Thousands,
            "MILLION" or "MILLIONS" => EvidenceUnit.Millions,
            "COUNT" or "NUMBER" => EvidenceUnit.Count,
            "BPS" or "BASIS POINT" or "BASIS POINTS" => EvidenceUnit.BasisPoints,
            "PRICE" => EvidenceUnit.Price,
            _ => EvidenceUnit.Unknown
        };
    }

    public static (bool Relevant, string Reason) ClassifyRelevance(
        string canonicalSymbol,
        string? title,
        string? summary,
        EvidenceCategory category,
        string? metadata)
    {
        if (canonicalSymbol == "XAUUSD")
        {
            return (true, "Canonical instrument is XAUUSD.");
        }

        var text = string.Join(' ', title, summary, metadata).ToUpperInvariant();
        var relevant = category != EvidenceCategory.Other
            || ContainsAny(text, "XAU", "GOLD", "USD", "DOLLAR", "FED", "FOMC", "CPI", "PCE", "NFP", "TREASURY", "YIELD", "INFLATION", "GEOPOLITICAL");
        return relevant
            ? (true, "Deterministic XAUUSD category or keyword match.")
            : (false, "No deterministic XAUUSD relevance match.");
    }

    public static string Hash(params string?[] values)
    {
        var canonical = string.Join('\u001f', values.Select(value => Collapse(value)?.ToLowerInvariant() ?? string.Empty));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static string? Collapse(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Regex.Replace(value.Trim(), "\\s+", " ", RegexOptions.CultureInvariant);

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}
