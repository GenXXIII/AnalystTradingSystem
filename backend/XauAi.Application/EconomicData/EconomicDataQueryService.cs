namespace XauAi.Application.EconomicData;

internal sealed class EconomicDataQueryService(
    IEconomicSeriesStore seriesStore,
    IEconomicObservationStore observationStore,
    IEconomicSyncStateStore stateStore,
    IEconomicDataProvider provider,
    EconomicDataSettings settings) : IEconomicDataQueryService
{
    public Task<IReadOnlyList<EconomicSeriesResult>> GetSeriesAsync(
        EconomicSeriesQuery query,
        CancellationToken cancellationToken = default) =>
        seriesStore.QueryAsync(query, cancellationToken);

    public async Task<EconomicSeriesResult> GetSeriesByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await seriesStore.GetByIdAsync(id, cancellationToken)
        ?? throw new EconomicDataException(
            EconomicDataErrorCodes.SeriesNotFound,
            "The requested economic series was not found.");

    public async Task<PagedEconomicObservations> GetObservationsAsync(
        EconomicObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        if (await seriesStore.GetByIdAsync(query.EconomicSeriesId, cancellationToken) is null)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.SeriesNotFound,
                "The requested economic series was not found.");
        }

        return await observationStore.QueryAsync(query, cancellationToken);
    }

    public Task<IReadOnlyList<EconomicObservationResult>> GetLatestAsync(
        CancellationToken cancellationToken = default) =>
        observationStore.GetLatestForAllAsync(cancellationToken);

    public async Task<EconomicSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var providerStatus = await provider.GetStatusAsync(cancellationToken);
        var states = await stateStore.ListAsync(cancellationToken);
        var series = await seriesStore.QueryAsync(
            new EconomicSeriesQuery(ActiveOnly: false),
            cancellationToken);
        var observationCount = await observationStore.CountAsync(cancellationToken);

        return new EconomicSystemStatus(
            providerStatus,
            states,
            settings.Series.Count,
            series.Count,
            observationCount);
    }

    private void ValidateQuery(EconomicObservationQuery query)
    {
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > settings.MaximumPageSize)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.InvalidRequest,
                $"Page must be at least 1 and pageSize must be between 1 and {settings.MaximumPageSize}.");
        }

        if (query.From.HasValue && query.To.HasValue)
        {
            if (query.From > query.To)
            {
                throw new EconomicDataException(
                    EconomicDataErrorCodes.InvalidRequest,
                    "The economic observation start date must not be after the end date.");
            }

            if (ExceedsYears(query.From.Value, query.To.Value, settings.MaximumQueryRangeYears))
            {
                throw new EconomicDataException(
                    EconomicDataErrorCodes.InvalidRequest,
                    $"The economic observation range cannot exceed {settings.MaximumQueryRangeYears} years.");
            }
        }
    }

    private static bool ExceedsYears(DateOnly from, DateOnly to, int maximumYears)
    {
        var targetYear = from.Year + maximumYears;
        if (targetYear > DateOnly.MaxValue.Year)
        {
            return false;
        }

        var anniversary = new DateOnly(
            targetYear,
            from.Month,
            Math.Min(from.Day, DateTime.DaysInMonth(targetYear, from.Month)));
        return anniversary < to;
    }
}
