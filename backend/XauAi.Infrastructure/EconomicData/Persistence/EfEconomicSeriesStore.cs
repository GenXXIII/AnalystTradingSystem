using Microsoft.EntityFrameworkCore;
using XauAi.Application.EconomicData;
using XauAi.Domain.EconomicData;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.EconomicData.Persistence;

internal sealed class EfEconomicSeriesStore(
    XauAiDbContext context,
    EconomicReferenceResolver references) : IEconomicSeriesStore
{
    public async Task<EconomicSeriesResult> UpsertAsync(
        ProviderEconomicSeries metadata,
        EconomicSeriesDefinition definition,
        string providerKey,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var providerId = await references.ProviderIdAsync(providerKey, cancellationToken);
        var series = await context.EconomicSeries.SingleOrDefaultAsync(
            item => item.DataProviderId == providerId
                && item.ExternalSeriesId == metadata.ExternalSeriesId,
            cancellationToken);
        if (series is null)
        {
            series = new EconomicSeries
            {
                Id = Guid.NewGuid(),
                DataProviderId = providerId,
                ExternalSeriesId = metadata.ExternalSeriesId,
                CreatedAtUtc = updatedAtUtc
            };
            context.EconomicSeries.Add(series);
        }

        series.Name = metadata.Name;
        series.Description = metadata.Description;
        series.Units = metadata.Units;
        series.Frequency = metadata.Frequency;
        series.SeasonalAdjustment = metadata.SeasonalAdjustment;
        series.CountryCode = definition.CountryCode.ToUpperInvariant();
        series.CurrencyCode = definition.CurrencyCode.ToUpperInvariant();
        series.Category = definition.Category;
        series.ObservationStartDate = metadata.ObservationStartDate;
        series.ObservationEndDate = metadata.ObservationEndDate;
        series.ProviderUpdatedAtUtc = metadata.ProviderUpdatedAtUtc;
        series.IsActive = true;
        series.UpdatedAtUtc = updatedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
        return Map(series, providerKey);
    }

    public async Task<IReadOnlyList<EconomicSeriesResult>> QueryAsync(
        EconomicSeriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = context.EconomicSeries.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            items = items.Where(series => series.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            items = items.Where(series => series.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query.Frequency))
        {
            var frequency = query.Frequency.Trim();
            items = items.Where(series => series.Frequency == frequency);
        }

        return await Project(items
            .OrderBy(series => series.Category)
            .ThenBy(series => series.Name))
            .ToArrayAsync(cancellationToken);
    }

    public Task<EconomicSeriesResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Project(context.EconomicSeries.AsNoTracking().Where(series => series.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<EconomicSeriesResult?> GetByExternalIdAsync(
        string providerKey,
        string externalSeriesId,
        CancellationToken cancellationToken = default) =>
        Project(context.EconomicSeries.AsNoTracking()
                .Where(series => series.ExternalSeriesId == externalSeriesId), providerKey)
            .SingleOrDefaultAsync(cancellationToken);

    private IQueryable<EconomicSeriesResult> Project(
        IQueryable<EconomicSeries> source,
        string? providerKey = null) =>
        source.Join(
            context.DataProviders.AsNoTracking()
                .Where(provider => providerKey == null || provider.Key == providerKey),
            series => series.DataProviderId,
            provider => provider.Id,
            (series, provider) => new EconomicSeriesResult(
                series.Id,
                provider.Key,
                series.ExternalSeriesId,
                series.Name,
                series.Description,
                series.Units,
                series.Frequency,
                series.SeasonalAdjustment,
                series.CountryCode,
                series.CurrencyCode,
                series.Category,
                series.ObservationStartDate,
                series.ObservationEndDate,
                series.ProviderUpdatedAtUtc,
                series.IsActive,
                series.CreatedAtUtc,
                series.UpdatedAtUtc));

    private static EconomicSeriesResult Map(EconomicSeries series, string provider) =>
        new(
            series.Id,
            provider,
            series.ExternalSeriesId,
            series.Name,
            series.Description,
            series.Units,
            series.Frequency,
            series.SeasonalAdjustment,
            series.CountryCode,
            series.CurrencyCode,
            series.Category,
            series.ObservationStartDate,
            series.ObservationEndDate,
            series.ProviderUpdatedAtUtc,
            series.IsActive,
            series.CreatedAtUtc,
            series.UpdatedAtUtc);
}
