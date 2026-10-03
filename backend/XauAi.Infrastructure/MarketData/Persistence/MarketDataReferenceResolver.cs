using Microsoft.EntityFrameworkCore;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.MarketData.Persistence;

internal sealed record MarketDataReferences(Guid InstrumentId, Guid TimeframeId, Guid ProviderId);

internal sealed class MarketDataReferenceResolver(XauAiDbContext dbContext)
{
    private const string Mt5ProviderKey = "mt5";

    public async Task<MarketDataReferences?> FindAsync(
        string symbol,
        MarketTimeframe timeframe,
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
            .Where(provider => provider.Key == Mt5ProviderKey && provider.IsActive)
            .Select(provider => (Guid?)provider.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return instrumentId is null || timeframeId is null || providerId is null
            ? null
            : new MarketDataReferences(instrumentId.Value, timeframeId.Value, providerId.Value);
    }

    public async Task<MarketDataReferences> RequireAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken) =>
        await FindAsync(symbol, timeframe, cancellationToken)
        ?? throw new MarketDataException(
            MarketDataErrorCodes.DatabaseDisabled,
            "Required market reference data is unavailable.");
}
