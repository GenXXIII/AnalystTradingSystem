namespace XauAi.Application.Analysts;

internal sealed class AnalystQueryService(
    IAnalystDataProvider provider,
    IAnalystQueryStore queryStore,
    IAnalystSyncStateStore stateStore,
    AnalystSettings settings,
    TimeProvider timeProvider) : IAnalystQueryService
{
    public Task<PagedAnalystSources> GetSourcesAsync(
        AnalystSourceQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        if (query.Type?.Length > 64)
        {
            throw Invalid("The analyst source type filter exceeds its maximum length.");
        }

        return queryStore.QuerySourcesAsync(query, cancellationToken);
    }

    public async Task<AnalystSourceResult> GetSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await queryStore.GetSourceAsync(id, cancellationToken)
        ?? throw NotFound("The requested analyst source was not found.");

    public Task<PagedAnalysts> GetAnalystsAsync(
        AnalystIdentityQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        return queryStore.QueryAnalystsAsync(query, cancellationToken);
    }

    public async Task<AnalystIdentityResult> GetAnalystAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await queryStore.GetAnalystAsync(id, cancellationToken)
        ?? throw NotFound("The requested analyst was not found.");

    public Task<PagedAnalystPredictions> GetPredictionsAsync(
        AnalystPredictionQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc >= query.ToUtc)
        {
            throw Invalid("The analyst prediction query start must be before the end.");
        }

        if (query.Instrument?.Length > 32)
        {
            throw Invalid("The analyst instrument filter exceeds its maximum length.");
        }

        var now = timeProvider.GetUtcNow();
        var asOf = (query.AsOfUtc ?? now).ToUniversalTime();
        if (asOf > now.AddMinutes(1))
        {
            throw Invalid("The analyst prediction as-of timestamp cannot be in the future.");
        }

        return queryStore.QueryPredictionsAsync(query, asOf, cancellationToken);
    }

    public async Task<AnalystPredictionResult> GetPredictionAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await queryStore.GetPredictionAsync(id, timeProvider.GetUtcNow(), cancellationToken)
        ?? throw NotFound("The requested analyst prediction was not found.");

    public Task<PagedAnalystPredictions> GetLatestAsync(
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetPredictionsAsync(
            new AnalystPredictionQuery(
                FromUtc: timeProvider.GetUtcNow().AddDays(-7),
                PageSize: pageSize),
            cancellationToken);

    public async Task<AnalystSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var providerStatus = await provider.GetStatusAsync(cancellationToken);
        var synchronization = await stateStore.GetAsync(settings.ProviderKey, cancellationToken);
        var counts = await queryStore.CountAsync(cancellationToken);
        return new AnalystSystemStatus(
            providerStatus,
            synchronization,
            counts.Sources,
            counts.Analysts,
            counts.Publications,
            counts.Predictions);
    }

    private void ValidatePage(int page, int pageSize)
    {
        if (page < 1)
        {
            throw Invalid("The analyst page must be at least 1.");
        }

        if (pageSize is < 1 || pageSize > settings.MaximumPageSize)
        {
            throw Invalid($"The analyst page size must be between 1 and {settings.MaximumPageSize}.");
        }
    }

    private static AnalystException Invalid(string message) =>
        new(AnalystErrorCodes.InvalidRequest, message);

    private static AnalystException NotFound(string message) =>
        new(AnalystErrorCodes.NotFound, message);
}
