using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application.EconomicData;

namespace XauAi.IntegrationTests;

public sealed class EconomicDataApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly Guid SeriesId = Guid.Parse("80000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-03T00:00:00Z");

    [Theory]
    [InlineData("/api/economic-data/series")]
    [InlineData("/api/economic-data/series?category=Inflation&frequency=Monthly")]
    public async Task Series_endpoints_return_application_owned_metadata(string path)
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();

        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.Equal("TEST_CPI", body.GetProperty("data")[0].GetProperty("externalSeriesId").GetString());
        Assert.False(body.GetProperty("data")[0].TryGetProperty("apiKey", out _));
    }

    [Fact]
    public async Task Detail_observations_latest_status_and_sync_are_bounded_and_safe()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();

        using var detail = await client.GetAsync($"/api/economic-data/series/{SeriesId}");
        using var observations = await client.GetAsync($"/api/economic-data/series/{SeriesId}/observations?page=1&pageSize=10&from=2026-08-01&to=2026-09-01");
        using var latest = await client.GetAsync("/api/economic-data/latest");
        using var status = await client.GetAsync("/api/economic-data/status");
        using var sync = await client.PostAsJsonAsync("/api/economic-data/synchronize", new { externalSeriesId = "TEST_CPI" });
        var observationBody = await observations.Content.ReadFromJsonAsync<JsonElement>();
        var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
        var syncBody = await sync.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, observations.StatusCode);
        Assert.Equal(1, observationBody.GetProperty("data").GetProperty("totalItems").GetInt32());
        Assert.Equal("3.2", observationBody.GetProperty("data").GetProperty("items")[0].GetProperty("originalValue").GetString());
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Equal("Available", statusBody.GetProperty("data").GetProperty("provider").GetProperty("state").GetString());
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        Assert.Equal(1, syncBody.GetProperty("data").GetProperty("succeeded").GetInt32());
    }

    [Fact]
    public async Task Economic_provider_errors_are_safe_and_do_not_expose_credentials()
    {
        const string sentinel = "secret-fred-key-must-not-leak";
        using var configuredFactory = CreateFactory(failSync: true);
        using var client = configuredFactory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/economic-data/synchronize", new { });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var json = body.ToString();

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Contains(EconomicDataErrorCodes.RateLimited, json, StringComparison.Ordinal);
        Assert.DoesNotContain(sentinel, json, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", json, StringComparison.OrdinalIgnoreCase);
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory(bool failSync = false) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEconomicDataQueryService>();
            services.RemoveAll<IEconomicDataSynchronizationService>();
            services.AddSingleton<IEconomicDataQueryService>(new ApiEconomicQueryService());
            services.AddSingleton<IEconomicDataSynchronizationService>(new ApiEconomicSyncService(failSync));
        }));

    private static EconomicSeriesResult Series() => new(
        SeriesId, "fred", "TEST_CPI", "TEST_ONLY CPI", null, "Percent", "Monthly",
        "Seasonally Adjusted", "US", "USD", "Inflation", new DateOnly(2000, 1, 1),
        new DateOnly(2026, 9, 1), Now, true, Now, Now);

    private static EconomicObservationResult Observation() => new(
        Guid.Parse("81000000-0000-0000-0000-000000000001"), SeriesId, "TEST_CPI",
        "TEST_ONLY CPI", new DateOnly(2026, 9, 1), 3.2m, "3.2", "Available",
        null, null, Now, 0);

    private sealed class ApiEconomicQueryService : IEconomicDataQueryService
    {
        public Task<IReadOnlyList<EconomicSeriesResult>> GetSeriesAsync(EconomicSeriesQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EconomicSeriesResult>>([Series()]);

        public Task<EconomicSeriesResult> GetSeriesByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Series());

        public Task<PagedEconomicObservations> GetObservationsAsync(EconomicObservationQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedEconomicObservations([Observation()], 1, query.PageSize, 1, 1));

        public Task<IReadOnlyList<EconomicObservationResult>> GetLatestAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EconomicObservationResult>>([Observation()]);

        public Task<EconomicSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new EconomicSystemStatus(
                new EconomicProviderStatus("FRED", EconomicProviderState.Available, true, "TEST_ONLY", Now),
                [], 1, 1, 1));
    }

    private sealed class ApiEconomicSyncService(bool fail) : IEconomicDataSynchronizationService
    {
        public Task<EconomicSyncResult> SynchronizeAsync(EconomicSyncRequest request, CancellationToken cancellationToken = default)
        {
            if (fail)
            {
                throw new EconomicDataException(
                    EconomicDataErrorCodes.RateLimited,
                    "The economic provider rate limit was reached.",
                    transient: true);
            }

            var series = new EconomicSeriesSyncResult(
                Guid.NewGuid(), "fred", "TEST_CPI", Now, Now, 2, 0, 1, 1, 0, 0,
                "Succeeded", null, null, 1);
            return Task.FromResult(new EconomicSyncResult([series], 1, 0, 2, 1, 1, 0, 0));
        }
    }
}
