using Microsoft.EntityFrameworkCore;
using XauAi.Application.Analysts;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.Analysts.Persistence;

internal sealed class EfAnalystQueryStore(XauAiDbContext context) : IAnalystQueryStore
{
    public async Task<PagedAnalystSources> QuerySourcesAsync(
        AnalystSourceQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = context.AnalystSources.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = query.Type.Trim();
            source = source.Where(value => value.Type == type);
        }

        if (query.ActiveOnly)
        {
            source = source.Where(value => value.IsActive);
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await (from value in source
                           join provider in context.DataProviders.AsNoTracking() on value.DataProviderId equals provider.Id
                           orderby value.Name, value.Id
                           select new AnalystSourceResult(
                               value.Id,
                               provider.Key,
                               value.ExternalId,
                               value.Name,
                               value.Type,
                               value.Website,
                               value.CountryCode,
                               value.IsActive,
                               value.CreatedAtUtc,
                               value.UpdatedAtUtc))
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedAnalystSources(items, query.Page, query.PageSize, total, Pages(total, query.PageSize));
    }

    public Task<AnalystSourceResult?> GetSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        (from source in context.AnalystSources.AsNoTracking()
         join provider in context.DataProviders.AsNoTracking() on source.DataProviderId equals provider.Id
         where source.Id == id
         select new AnalystSourceResult(
             source.Id,
             provider.Key,
             source.ExternalId,
             source.Name,
             source.Type,
             source.Website,
             source.CountryCode,
             source.IsActive,
             source.CreatedAtUtc,
             source.UpdatedAtUtc))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedAnalysts> QueryAnalystsAsync(
        AnalystIdentityQuery query,
        CancellationToken cancellationToken = default)
    {
        var analysts = context.Analysts.AsNoTracking().AsQueryable();
        if (query.SourceId.HasValue)
        {
            analysts = analysts.Where(value => value.AnalystSourceId == query.SourceId.Value);
        }

        if (query.ActiveOnly)
        {
            analysts = analysts.Where(value => value.IsActive);
        }

        var total = await analysts.CountAsync(cancellationToken);
        var items = await (from analyst in analysts
                           join source in context.AnalystSources.AsNoTracking() on analyst.AnalystSourceId equals source.Id
                           orderby analyst.Name, analyst.Id
                           select new AnalystIdentityResult(
                               analyst.Id,
                               source.Id,
                               source.Name,
                               analyst.ExternalId,
                               analyst.Name,
                               analyst.Role,
                               analyst.ProfileUrl,
                               analyst.IsActive,
                               analyst.CreatedAtUtc,
                               analyst.UpdatedAtUtc))
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedAnalysts(items, query.Page, query.PageSize, total, Pages(total, query.PageSize));
    }

    public Task<AnalystIdentityResult?> GetAnalystAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        (from analyst in context.Analysts.AsNoTracking()
         join source in context.AnalystSources.AsNoTracking() on analyst.AnalystSourceId equals source.Id
         where analyst.Id == id
         select new AnalystIdentityResult(
             analyst.Id,
             source.Id,
             source.Name,
             analyst.ExternalId,
             analyst.Name,
             analyst.Role,
             analyst.ProfileUrl,
             analyst.IsActive,
             analyst.CreatedAtUtc,
             analyst.UpdatedAtUtc))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedAnalystPredictions> QueryPredictionsAsync(
        AnalystPredictionQuery query,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var source = context.AnalystStatements.AsNoTracking()
            .Where(prediction => prediction.PublicationId != null && prediction.PublishedAtUtc <= asOfUtc);
        if (!string.IsNullOrWhiteSpace(query.Instrument))
        {
            var instrument = query.Instrument.Trim().ToUpperInvariant();
            source = source.Where(prediction => prediction.InstrumentCode == instrument);
        }

        if (query.Direction.HasValue)
        {
            var direction = query.Direction.Value.ToString();
            source = source.Where(prediction => prediction.Direction == direction);
        }

        if (query.SourceId.HasValue)
        {
            source = source.Where(prediction => prediction.AnalystSourceId == query.SourceId.Value);
        }

        if (query.AnalystId.HasValue)
        {
            source = source.Where(prediction => prediction.AnalystId == query.AnalystId.Value);
        }

        if (query.FromUtc.HasValue)
        {
            var from = query.FromUtc.Value.ToUniversalTime();
            source = source.Where(prediction => prediction.PublishedAtUtc >= from);
        }

        if (query.ToUtc.HasValue)
        {
            var to = query.ToUtc.Value.ToUniversalTime();
            source = source.Where(prediction => prediction.PublishedAtUtc <= to);
        }

        if (query.Horizon.HasValue)
        {
            var horizon = query.Horizon.Value.ToString();
            source = source.Where(prediction => prediction.HorizonUnit == horizon);
        }

        var total = await source.CountAsync(cancellationToken);
        var page = source
            .OrderByDescending(prediction => prediction.PublishedAtUtc)
            .ThenByDescending(prediction => prediction.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize);
        var rows = await JoinPredictionRows(page).ToArrayAsync(cancellationToken);
        return new PagedAnalystPredictions(
            rows.Select(Map).ToArray(),
            query.Page,
            query.PageSize,
            total,
            Pages(total, query.PageSize),
            asOfUtc);
    }

    public async Task<AnalystPredictionResult?> GetPredictionAsync(
        Guid id,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var prediction = context.AnalystStatements.AsNoTracking()
            .Where(value => value.Id == id
                && value.PublicationId != null
                && value.PublishedAtUtc <= asOfUtc);
        var row = await JoinPredictionRows(prediction).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<(int Sources, int Analysts, int Publications, int Predictions)> CountAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await context.AnalystSources.AsNoTracking().CountAsync(cancellationToken);
        var analysts = await context.Analysts.AsNoTracking().CountAsync(cancellationToken);
        var publications = await context.AnalystPublications.AsNoTracking().CountAsync(cancellationToken);
        var predictions = await context.AnalystStatements.AsNoTracking()
            .CountAsync(value => value.PublicationId != null, cancellationToken);
        return (sources, analysts, publications, predictions);
    }

    private IQueryable<PredictionRow> JoinPredictionRows(
        IQueryable<XauAi.Domain.Analysts.AnalystStatement> predictions) =>
        from prediction in predictions
        join publication in context.AnalystPublications.AsNoTracking()
            on prediction.PublicationId equals (Guid?)publication.Id
        join source in context.AnalystSources.AsNoTracking()
            on prediction.AnalystSourceId equals (Guid?)source.Id
        join analystValue in context.Analysts.AsNoTracking()
            on prediction.AnalystId equals (Guid?)analystValue.Id into analystGroup
        from analyst in analystGroup.DefaultIfEmpty()
        select new PredictionRow(prediction, publication, source, analyst);

    private static AnalystPredictionResult Map(PredictionRow row) => new(
        row.Prediction.Id,
        row.Publication.Id,
        row.Source.Id,
        row.Source.Name,
        row.Analyst == null ? null : row.Analyst.Id,
        row.Prediction.AnalystName,
        row.Prediction.InstrumentCode,
        row.Prediction.AssetClass,
        Enum.TryParse<AnalystDirection>(row.Prediction.Direction, out var direction)
            ? direction
            : AnalystDirection.Unknown,
        row.Publication.Title,
        row.Publication.Summary,
        row.Prediction.PermittedContent,
        row.Prediction.TargetPrice,
        row.Prediction.TargetRangeLow,
        row.Prediction.TargetRangeHigh,
        row.Prediction.TargetCurrency,
        row.Prediction.HorizonValue,
        Enum.TryParse<AnalystHorizonUnit>(row.Prediction.HorizonUnit, out var horizon)
            ? horizon
            : AnalystHorizonUnit.Unknown,
        row.Prediction.TimeHorizon,
        row.Prediction.Confidence,
        row.Prediction.Reason,
        row.Prediction.Category,
        row.Prediction.Status,
        row.Publication.SourceUrl,
        row.Prediction.ExternalId,
        row.Prediction.Language,
        row.Prediction.PublishedAtUtc,
        row.Prediction.FetchedAtUtc,
        row.Publication.Version,
        row.Publication.RelatedPublicationId,
        row.Publication.RelationshipType);

    private static int Pages(int total, int pageSize) =>
        total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

    private sealed record PredictionRow(
        Domain.Analysts.AnalystStatement Prediction,
        Domain.Analysts.AnalystPublication Publication,
        Domain.Analysts.AnalystSource Source,
        Domain.Analysts.Analyst? Analyst);
}
