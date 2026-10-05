using System.Globalization;
using System.Text.Json;
using XauAi.Application.Analysts;
using XauAi.Application.EconomicData;
using XauAi.Application.Evidence;
using XauAi.Application.MarketData;
using XauAi.Application.News;
using XauAi.Domain.EconomicData;
using XauAi.Domain.Evidence;

namespace XauAi.Infrastructure.Evidence.Persistence;

internal static class EvidenceRecordFactory
{
    public static void Apply(EvidenceRecord target, EvidenceRecord source)
    {
        target.DataProviderId = source.DataProviderId;
        target.InstrumentId = source.InstrumentId;
        target.TimeframeId = source.TimeframeId;
        target.Kind = source.Kind;
        target.EvidenceType = source.EvidenceType;
        target.SourceType = source.SourceType;
        target.SourceKey = source.SourceKey;
        target.ExternalId = source.ExternalId;
        target.IdentityHash = source.IdentityHash;
        target.ContentHash = source.ContentHash;
        target.CanonicalSymbol = source.CanonicalSymbol;
        target.OriginalSymbol = source.OriginalSymbol;
        target.TimeframeCode = source.TimeframeCode;
        target.ObservedAtUtc = source.ObservedAtUtc;
        target.AvailableAtUtc = source.AvailableAtUtc;
        target.PublishedAtUtc = source.PublishedAtUtc;
        target.CollectedAtUtc = source.CollectedAtUtc;
        target.ValidFromUtc = source.ValidFromUtc;
        target.ValidToUtc = source.ValidToUtc;
        target.Title = source.Title;
        target.Summary = source.Summary;
        target.NumericValue = source.NumericValue;
        target.OriginalValue = source.OriginalValue;
        target.Unit = source.Unit;
        target.OriginalUnit = source.OriginalUnit;
        target.Direction = source.Direction;
        target.OriginalDirection = source.OriginalDirection;
        target.Importance = source.Importance;
        target.OriginalImportance = source.OriginalImportance;
        target.Category = source.Category;
        target.OriginalCategory = source.OriginalCategory;
        target.CurrencyCode = source.CurrencyCode;
        target.OriginalSourceUrl = source.OriginalSourceUrl;
        target.Quality = source.Quality;
        target.Completeness = source.Completeness;
        target.TimestampQuality = source.TimestampQuality;
        target.SourceReliability = source.SourceReliability;
        target.IsRelevant = source.IsRelevant;
        target.RelevanceReason = source.RelevanceReason;
        target.MetadataJson = source.MetadataJson;
        target.UpdatedAtUtc = source.UpdatedAtUtc;
    }

    public static EvidenceRecord MarketCandle(
        Guid id,
        Guid providerId,
        Guid instrumentId,
        Guid timeframeId,
        string providerKey,
        MarketCandleSnapshot candle)
    {
        var timeframe = candle.Timeframe.Code();
        var direction = candle.Close > candle.Open ? EvidenceDirection.Bullish
            : candle.Close < candle.Open ? EvidenceDirection.Bearish
            : EvidenceDirection.Neutral;
        var availableAt = candle.CloseTimeUtc.ToUniversalTime();
        var metadata = JsonSerializer.Serialize(new
        {
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.TickVolume,
            candle.RealVolume,
            candle.Spread,
            candle.IsComplete
        });
        return Base(
            id,
            "MarketCandle",
            EvidenceType.Market,
            EvidenceSourceType.InternalMarketData,
            providerKey,
            $"{candle.ProviderSymbol}:{timeframe}:{candle.OpenTimeUtc:O}",
            EvidenceNormalization.Hash(providerKey, candle.Symbol, timeframe, candle.OpenTimeUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            EvidenceNormalization.Hash(candle.Symbol, timeframe, candle.OpenTimeUtc.ToString("O", CultureInfo.InvariantCulture), metadata),
            candle.Symbol,
            candle.ProviderSymbol,
            timeframe,
            candle.OpenTimeUtc,
            availableAt,
            null,
            candle.FetchedAtUtc,
            $"{candle.Symbol} {timeframe} candle",
            $"OHLC {candle.Open} / {candle.High} / {candle.Low} / {candle.Close}",
            candle.Close,
            candle.Close.ToString(CultureInfo.InvariantCulture),
            EvidenceUnit.Price,
            "price",
            direction,
            direction.ToString(),
            EvidenceImportance.Unknown,
            null,
            EvidenceCategory.Commodity,
            "MarketCandle",
            "USD",
            null,
            candle.IsComplete ? EvidenceQuality.High : EvidenceQuality.Low,
            candle.IsComplete ? EvidenceCompleteness.Complete : EvidenceCompleteness.Partial,
            EvidenceTimestampQuality.Exact,
            EvidenceSourceReliability.Known,
            true,
            "Canonical instrument is XAUUSD.",
            metadata,
            candle.FetchedAtUtc,
            providerId,
            instrumentId,
            timeframeId);
    }

    public static EvidenceRecord NewsArticle(
        NormalizedNewsArticle article,
        Guid providerId,
        Guid? instrumentId,
        string providerKey)
    {
        var externalId = article.ProviderArticleId ?? article.CanonicalUrlHash;
        var category = EvidenceNormalization.NormalizeCategory(article.Categories.FirstOrDefault());
        return Base(
            article.Id,
            "NewsArticle",
            EvidenceType.News,
            EvidenceSourceType.NewsProvider,
            providerKey,
            externalId,
            EvidenceNormalization.Hash(providerKey, externalId),
            article.ContentHash,
            "XAUUSD",
            "XAUUSD",
            null,
            article.PublishedAtUtc,
            article.PublishedAtUtc,
            article.PublishedAtUtc,
            article.CollectedAtUtc,
            article.Title,
            article.Description,
            null,
            null,
            EvidenceUnit.Unknown,
            null,
            EvidenceDirection.Unknown,
            null,
            EvidenceImportance.Unknown,
            null,
            category,
            article.ProviderCategories.FirstOrDefault(),
            null,
            article.SourceUrl,
            externalId is not null ? EvidenceQuality.High : EvidenceQuality.Medium,
            externalId is not null ? EvidenceCompleteness.Complete : EvidenceCompleteness.Partial,
            EvidenceTimestampQuality.Exact,
            EvidenceSourceReliability.Known,
            true,
            $"Existing deterministic news relevance classification: {article.Relevance}.",
            JsonSerializer.Serialize(new
            {
                article.Author,
                article.SourceName,
                article.Publisher,
                article.Language,
                article.Categories,
                article.CountryCodes,
                article.Entities
            }),
            article.CollectedAtUtc,
            providerId,
            instrumentId,
            null);
    }

    public static EvidenceRecord AnalystPrediction(
        Guid id,
        Guid providerId,
        Guid? instrumentId,
        string providerKey,
        NormalizedAnalystItem item,
        NormalizedAnalystPrediction prediction,
        Guid sourceId,
        Guid? analystId)
    {
        var externalId = prediction.ExternalId ?? prediction.ClaimHash;
        var unit = prediction.TargetCurrency == "USD" ? EvidenceUnit.Usd : EvidenceUnit.Price;
        return Base(
            id,
            "AnalystPrediction",
            EvidenceType.Analyst,
            EvidenceSourceType.AnalystProvider,
            providerKey,
            externalId,
            EvidenceNormalization.Hash(providerKey, item.Id.ToString(), externalId),
            prediction.ClaimHash,
            prediction.Instrument,
            prediction.Instrument,
            null,
            item.PublishedAtUtc,
            item.PublishedAtUtc,
            item.PublishedAtUtc,
            item.CollectedAtUtc,
            item.Title,
            prediction.ClaimText ?? item.Summary,
            prediction.TargetPrice,
            prediction.TargetPrice?.ToString(CultureInfo.InvariantCulture),
            unit,
            prediction.TargetCurrency,
            Parse(prediction.Direction),
            prediction.Direction.ToString(),
            EvidenceImportance.Unknown,
            null,
            EvidenceNormalization.NormalizeCategory(prediction.Category),
            prediction.Category,
            prediction.TargetCurrency,
            item.SourceUrl,
            EvidenceQuality.High,
            EvidenceCompleteness.Complete,
            EvidenceTimestampQuality.Exact,
            EvidenceSourceReliability.Known,
            true,
            "Passed the deterministic analyst relevance filter.",
            JsonSerializer.Serialize(new
            {
                AnalystSourceId = sourceId,
                AnalystId = analystId,
                prediction.TargetRangeLow,
                prediction.TargetRangeHigh,
                prediction.HorizonValue,
                HorizonUnit = prediction.HorizonUnit.ToString(),
                prediction.TimeHorizon,
                prediction.Reason
            }),
            item.CollectedAtUtc,
            providerId,
            instrumentId,
            null);
    }

    public static EvidenceRecord EconomicObservation(
        Guid id,
        Guid? instrumentId,
        EconomicSeries series,
        string providerKey,
        ProviderEconomicObservation observation,
        DateTimeOffset fetchedAtUtc)
    {
        var externalId = $"{series.ExternalSeriesId}:{observation.ObservationDate:yyyy-MM-dd}";
        var eventTime = new DateTimeOffset(
            observation.ObservationDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            TimeSpan.Zero);
        var category = EvidenceNormalization.NormalizeCategory(series.Category);
        var unit = EvidenceNormalization.NormalizeUnit(series.Units);
        var identity = EvidenceNormalization.Hash(
            providerKey,
            externalId,
            fetchedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            observation.OriginalValue);
        return Base(
            id,
            "EconomicObservation",
            EvidenceType.Economic,
            EvidenceSourceType.EconomicProvider,
            providerKey,
            externalId,
            identity,
            EvidenceNormalization.Hash(series.ExternalSeriesId, observation.ObservationDate.ToString("O"), observation.OriginalValue),
            "XAUUSD",
            series.ExternalSeriesId,
            null,
            eventTime,
            fetchedAtUtc,
            null,
            fetchedAtUtc,
            series.Name,
            $"{series.Name} observation for {observation.ObservationDate:yyyy-MM-dd}.",
            observation.Value,
            observation.OriginalValue,
            unit,
            series.Units,
            EvidenceDirection.Unknown,
            null,
            EvidenceImportance.Unknown,
            null,
            category,
            series.Category,
            string.IsNullOrWhiteSpace(series.CurrencyCode) ? null : series.CurrencyCode,
            null,
            EvidenceQuality.Medium,
            EvidenceCompleteness.Complete,
            EvidenceTimestampQuality.DateOnly,
            EvidenceSourceReliability.Known,
            true,
            $"Tracked economic series category: {series.Category}.",
            JsonSerializer.Serialize(new
            {
                series.ExternalSeriesId,
                observation.ObservationDate,
                observation.RealtimeStartDate,
                observation.RealtimeEndDate,
                observation.Status
            }),
            fetchedAtUtc,
            series.DataProviderId,
            instrumentId,
            null);
    }

    private static EvidenceRecord Base(
        Guid id,
        string kind,
        EvidenceType evidenceType,
        EvidenceSourceType sourceType,
        string sourceKey,
        string? externalId,
        string identityHash,
        string? contentHash,
        string canonicalSymbol,
        string? originalSymbol,
        string? timeframe,
        DateTimeOffset observedAt,
        DateTimeOffset availableAt,
        DateTimeOffset? publishedAt,
        DateTimeOffset? collectedAt,
        string? title,
        string? summary,
        decimal? value,
        string? originalValue,
        EvidenceUnit unit,
        string? originalUnit,
        EvidenceDirection direction,
        string? originalDirection,
        EvidenceImportance importance,
        string? originalImportance,
        EvidenceCategory category,
        string? originalCategory,
        string? currencyCode,
        string? sourceUrl,
        EvidenceQuality quality,
        EvidenceCompleteness completeness,
        EvidenceTimestampQuality timestampQuality,
        EvidenceSourceReliability sourceReliability,
        bool isRelevant,
        string relevanceReason,
        string? metadataJson,
        DateTimeOffset createdAt,
        Guid? providerId,
        Guid? instrumentId,
        Guid? timeframeId) => new()
        {
            Id = id,
            DataProviderId = providerId,
            InstrumentId = instrumentId,
            TimeframeId = timeframeId,
            Kind = kind,
            EvidenceType = evidenceType.ToString(),
            SourceType = sourceType.ToString(),
            SourceKey = sourceKey,
            ExternalId = externalId,
            IdentityHash = identityHash,
            ContentHash = contentHash,
            CanonicalSymbol = canonicalSymbol,
            OriginalSymbol = originalSymbol,
            TimeframeCode = timeframe,
            ObservedAtUtc = observedAt.ToUniversalTime(),
            AvailableAtUtc = availableAt.ToUniversalTime(),
            PublishedAtUtc = publishedAt?.ToUniversalTime(),
            CollectedAtUtc = collectedAt?.ToUniversalTime(),
            ValidFromUtc = availableAt.ToUniversalTime(),
            Title = title,
            Summary = summary,
            NumericValue = value,
            OriginalValue = originalValue,
            Unit = unit.ToString(),
            OriginalUnit = originalUnit,
            Direction = direction.ToString(),
            OriginalDirection = originalDirection,
            Importance = importance.ToString(),
            OriginalImportance = originalImportance,
            Category = category.ToString(),
            OriginalCategory = originalCategory,
            CurrencyCode = currencyCode,
            OriginalSourceUrl = sourceUrl,
            Quality = quality.ToString(),
            Completeness = completeness.ToString(),
            TimestampQuality = timestampQuality.ToString(),
            SourceReliability = sourceReliability.ToString(),
            IsRelevant = isRelevant,
            RelevanceReason = relevanceReason,
            MetadataJson = metadataJson,
            CreatedAtUtc = createdAt.ToUniversalTime(),
            UpdatedAtUtc = createdAt.ToUniversalTime()
        };

    private static EvidenceDirection Parse(AnalystDirection value) => value switch
    {
        AnalystDirection.Bullish => EvidenceDirection.Bullish,
        AnalystDirection.Bearish => EvidenceDirection.Bearish,
        AnalystDirection.Neutral => EvidenceDirection.Neutral,
        _ => EvidenceDirection.Unknown
    };
}
