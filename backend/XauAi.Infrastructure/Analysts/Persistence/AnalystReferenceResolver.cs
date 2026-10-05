using Microsoft.EntityFrameworkCore;
using XauAi.Application.Analysts;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.Analysts.Persistence;

internal sealed class AnalystReferenceResolver(XauAiDbContext context)
{
    public async Task<Guid> ProviderIdAsync(string providerKey, CancellationToken cancellationToken) =>
        await context.DataProviders
            .Where(provider => provider.Key == providerKey && provider.IsActive)
            .Select(provider => provider.Id)
            .SingleOrDefaultAsync(cancellationToken) is var id && id != Guid.Empty
                ? id
                : throw new AnalystException(
                    AnalystErrorCodes.ProviderUnavailable,
                    "The configured analyst provider is not available in reference data.");

    public Task<Guid?> InstrumentIdAsync(string symbol, CancellationToken cancellationToken) =>
        context.Instruments
            .Where(instrument => instrument.Symbol == symbol && instrument.IsActive)
            .Select(instrument => (Guid?)instrument.Id)
            .SingleOrDefaultAsync(cancellationToken);
}
