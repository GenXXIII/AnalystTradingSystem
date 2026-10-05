using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Domain.Evidence;
using XauAi.Domain.Market;
using XauAi.Infrastructure.Persistence;
using XauAi.Infrastructure.Evidence.Persistence;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed class EfMarketCandleStore(
    XauAiDbContext dbContext,
    MarketDataPipelineSettings settings,
    ILogger<EfMarketCandleStore> logger) : IMarketCandleStore
{
    public async Task<StoredCandleCursor?> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        DateTimeOffset notAfterUtc,
        CancellationToken cancellationToken = default)
    {
        var references = await FindReferencesAsync(symbol, timeframe, settings.ProviderKey, cancellationToken);
        if (references is not { } foundReferences)
        {
            return null;
        }

        return await dbContext.MarketCandles
            .AsNoTracking()
            .Where(candle =>
                candle.InstrumentId == foundReferences.InstrumentId
                && candle.TimeframeId == foundReferences.TimeframeId
                && candle.DataProviderId == foundReferences.ProviderId
                && candle.OpenTimeUtc <= notAfterUtc.ToUniversalTime())
            .OrderByDescending(candle => candle.OpenTimeUtc)
            .Select(candle => new StoredCandleCursor(candle.OpenTimeUtc, candle.IsComplete))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CandleSaveResult> SaveAsync(
        IReadOnlyCollection<MarketCandleSnapshot> candles,
        CancellationToken cancellationToken = default)
    {
        if (candles.Count == 0)
        {
            return new CandleSaveResult(0, 0, 0);
        }

        var first = candles.First();
        if (candles.Any(candle =>
                !string.Equals(candle.Symbol, first.Symbol, StringComparison.OrdinalIgnoreCase)
                || candle.Timeframe != first.Timeframe
                || !string.Equals(candle.ProviderKey, first.ProviderKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                "A persistence batch must contain one symbol and timeframe.");
        }

        var providerKey = string.IsNullOrWhiteSpace(first.ProviderKey)
            ? settings.ProviderKey
            : first.ProviderKey;
        var (InstrumentId, TimeframeId, ProviderId) = await FindReferencesAsync(
            first.Symbol,
            first.Timeframe,
            providerKey,
            cancellationToken)
            ?? throw new MarketDataException(
                MarketDataErrorCodes.DatabaseDisabled,
                "Required market reference data is unavailable.");
        var normalized = candles
            .OrderBy(candle => candle.OpenTimeUtc)
            .DistinctBy(candle => candle.OpenTimeUtc)
            .ToArray();
        var fromUtc = normalized[0].OpenTimeUtc;
        var toUtc = normalized[^1].OpenTimeUtc;
        var existing = await dbContext.MarketCandles
            .Where(candle =>
                candle.InstrumentId == InstrumentId
                && candle.TimeframeId == TimeframeId
                && candle.DataProviderId == ProviderId
                && candle.OpenTimeUtc >= fromUtc
                && candle.OpenTimeUtc <= toUtc)
            .ToDictionaryAsync(candle => candle.OpenTimeUtc, cancellationToken);

        var inserted = 0;
        var updated = 0;
        var skipped = candles.Count - normalized.Length;

        foreach (var snapshot in normalized)
        {
            if (existing.TryGetValue(snapshot.OpenTimeUtc, out var stored))
            {
                if (stored.IsComplete)
                {
                    skipped++;
                    continue;
                }

                ApplySnapshot(stored, snapshot);
                var storedEvidence = await dbContext.EvidenceRecords.FindAsync([stored.Id], cancellationToken);
                if (storedEvidence is not null)
                {
                    EvidenceRecordFactory.Apply(
                        storedEvidence,
                        EvidenceRecordFactory.MarketCandle(
                            stored.Id,
                            ProviderId,
                            InstrumentId,
                            TimeframeId,
                            providerKey,
                            snapshot));
                }
                updated++;
                continue;
            }

            var candleId = Guid.NewGuid();
            dbContext.EvidenceRecords.Add(EvidenceRecordFactory.MarketCandle(
                candleId,
                ProviderId,
                InstrumentId,
                TimeframeId,
                providerKey,
                snapshot));
            var candle = new MarketCandle
            {
                Id = candleId,
                InstrumentId = InstrumentId,
                TimeframeId = TimeframeId,
                DataProviderId = ProviderId
            };
            ApplySnapshot(candle, snapshot);
            dbContext.MarketCandles.Add(candle);
            inserted++;
        }

        if (inserted > 0 || updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Persisted {ProviderKey} candle batch for {Symbol} {Timeframe}: {Inserted} inserted, {Updated} updated, {Skipped} skipped",
            providerKey,
            first.Symbol,
            first.Timeframe.Code(),
            inserted,
            updated,
            skipped);
        return new CandleSaveResult(inserted, updated, skipped);
    }

    private async Task<(Guid InstrumentId, Guid TimeframeId, Guid ProviderId)?> FindReferencesAsync(
        string symbol,
        MarketTimeframe timeframe,
        string providerKey,
        CancellationToken cancellationToken)
    {
        var instrumentId = await dbContext.Instruments
            .Where(instrument => instrument.Symbol == symbol && instrument.IsActive)
            .Select(instrument => (Guid?)instrument.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var timeframeCode = timeframe.Code();
        var timeframeId = await dbContext.Timeframes
            .Where(value => value.Code == timeframeCode && value.IsActive)
            .Select(value => (Guid?)value.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var providerId = await dbContext.DataProviders
            .Where(provider => provider.Key == providerKey && provider.IsActive)
            .Select(provider => (Guid?)provider.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return instrumentId is null || timeframeId is null || providerId is null
            ? null
            : (instrumentId.Value, timeframeId.Value, providerId.Value);
    }

    private static void ApplySnapshot(MarketCandle target, MarketCandleSnapshot source)
    {
        target.ProviderSymbol = source.ProviderSymbol;
        target.OpenTimeUtc = source.OpenTimeUtc.ToUniversalTime();
        target.CloseTimeUtc = source.CloseTimeUtc.ToUniversalTime();
        target.Open = source.Open;
        target.High = source.High;
        target.Low = source.Low;
        target.Close = source.Close;
        target.TickVolume = source.TickVolume;
        target.RealVolume = source.RealVolume;
        target.Spread = source.Spread;
        target.IsComplete = source.IsComplete;
        target.SourceTimeZone = source.SourceTimeZone;
        target.FetchedAtUtc = source.FetchedAtUtc.ToUniversalTime();
    }
}
