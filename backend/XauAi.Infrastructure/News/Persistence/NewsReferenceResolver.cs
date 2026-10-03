using Microsoft.EntityFrameworkCore;
using XauAi.Application.News;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.News.Persistence;

internal sealed class NewsReferenceResolver(XauAiDbContext context)
{
    public async Task<Guid> ProviderIdAsync(string providerKey, CancellationToken cancellationToken) =>
        await context.DataProviders
            .Where(provider => provider.Key == providerKey && provider.IsActive)
            .Select(provider => provider.Id)
            .SingleOrDefaultAsync(cancellationToken) is var id && id != Guid.Empty
                ? id
                : throw new NewsException(
                    NewsErrorCodes.ProviderUnavailable,
                    "The configured news provider is not available in reference data.");

    public async Task<Guid> InstrumentIdAsync(string symbol, CancellationToken cancellationToken) =>
        await context.Instruments
            .Where(instrument => instrument.Symbol == symbol && instrument.IsActive)
            .Select(instrument => instrument.Id)
            .SingleOrDefaultAsync(cancellationToken) is var id && id != Guid.Empty
                ? id
                : throw new NewsException(
                    NewsErrorCodes.InvalidRequest,
                    "The configured news instrument is not available in reference data.");
}
