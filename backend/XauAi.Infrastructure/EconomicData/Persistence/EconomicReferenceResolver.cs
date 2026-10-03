using Microsoft.EntityFrameworkCore;
using XauAi.Application.EconomicData;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.EconomicData.Persistence;

internal sealed class EconomicReferenceResolver(XauAiDbContext context)
{
    public async Task<Guid> ProviderIdAsync(string providerKey, CancellationToken cancellationToken)
    {
        var providerId = await context.DataProviders.AsNoTracking()
            .Where(provider => provider.Key == providerKey && provider.IsActive)
            .Select(provider => (Guid?)provider.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return providerId ?? throw new EconomicDataException(
            EconomicDataErrorCodes.DatabaseDisabled,
            "The configured economic data provider is not available in persistence.");
    }
}
