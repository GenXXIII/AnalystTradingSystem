using Microsoft.EntityFrameworkCore;
using XauAi.Application.EconomicData;
using XauAi.Domain.EconomicData;
using XauAi.Infrastructure.EconomicData.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.UnitTests.EconomicData;

public sealed class EconomicObservationStoreTests
{
    [Fact]
    public async Task Persistence_is_idempotent_preserves_missing_values_and_audits_revisions()
    {
        await using var context = CreateContext();
        var seriesId = Guid.NewGuid();
        context.EconomicSeries.Add(new EconomicSeries
        {
            Id = seriesId,
            DataProviderId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            ExternalSeriesId = "TEST_CPI",
            Name = "TEST_ONLY CPI",
            Units = "TEST units",
            Frequency = "Monthly",
            SeasonalAdjustment = "Test",
            CountryCode = "US",
            CurrencyCode = "USD",
            Category = "Inflation",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.Parse("2026-10-01T00:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-10-01T00:00:00Z")
        });
        await context.SaveChangesAsync();
        var store = new EfEconomicObservationStore(context);
        var firstFetch = DateTimeOffset.Parse("2026-10-02T00:00:00Z");
        var first = new[]
        {
            Observation("2026-08-01", 100.125m, "100.125", "Available"),
            Observation("2026-09-01", null, ".", "Missing")
        };

        var inserted = await store.PersistAsync(seriesId, first, firstFetch);
        var replayed = await store.PersistAsync(seriesId, first, firstFetch.AddHours(1));
        var revised = await store.PersistAsync(
            seriesId,
            [Observation("2026-08-01", 100.250m, "100.250", "Available")],
            firstFetch.AddDays(1));

        Assert.Equal(2, inserted.Inserted);
        Assert.Equal(2, replayed.Skipped);
        Assert.Equal(1, revised.Updated);
        Assert.Equal(2, await context.EconomicObservations.CountAsync());
        Assert.Equal(3, await context.EvidenceRecords.CountAsync(record => record.Kind == "EconomicObservation"));
        Assert.Single(await context.EvidenceRelations.Where(relation => relation.RelationType == "Updates").ToArrayAsync());
        var versions = await context.EvidenceRecords
            .Where(record => record.ExternalId == "TEST_CPI:2026-08-01")
            .OrderBy(record => record.AvailableAtUtc)
            .ToArrayAsync();
        Assert.Equal(2, versions.Length);
        Assert.Equal(firstFetch.AddDays(1), versions[0].ValidToUtc);
        Assert.Null(versions[1].ValidToUtc);
        var missing = await context.EconomicObservations.SingleAsync(item => item.ObservationDate == new DateOnly(2026, 9, 1));
        Assert.Null(missing.Value);
        Assert.Equal("Missing", missing.Status);
        var current = await context.EconomicObservations.SingleAsync(item => item.ObservationDate == new DateOnly(2026, 8, 1));
        Assert.Equal(100.250m, current.Value);
        var revision = await context.EconomicObservationRevisions.SingleAsync();
        Assert.Equal(100.125m, revision.Value);
        Assert.Equal(firstFetch, revision.FetchedAtUtc);
    }

    private static ProviderEconomicObservation Observation(
        string date,
        decimal? value,
        string original,
        string status) =>
        new(DateOnly.Parse(date), value, original, status, null, null);

    private static XauAiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseInMemoryDatabase($"EconomicObservationTests-{Guid.NewGuid():N}")
            .Options;
        var context = new XauAiDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
