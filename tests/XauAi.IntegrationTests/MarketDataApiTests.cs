using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application.MarketData;

namespace XauAi.IntegrationTests;

public sealed class MarketDataApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateTimeOffset StartUtc = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Latest_range_and_status_endpoints_return_normalized_market_data()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();

        using var latestResponse = await client.GetAsync("/api/market-data/XAUUSD/latest?timeframe=H1&limit=2&completedOnly=true");
        var latest = await latestResponse.Content.ReadFromJsonAsync<JsonElement>();
        using var rangeResponse = await client.GetAsync(
            $"/api/market-data/XAUUSD/candles?timeframe=H1&from={Uri.EscapeDataString(StartUtc.ToString("O"))}&to={Uri.EscapeDataString(StartUtc.AddHours(3).ToString("O"))}&limit=10&completedOnly=true");
        var range = await rangeResponse.Content.ReadFromJsonAsync<JsonElement>();
        using var statusResponse = await client.GetAsync("/api/market-data/XAUUSD/status?timeframe=H1");
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, latestResponse.StatusCode);
        Assert.Equal(2, latest.GetProperty("data").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, rangeResponse.StatusCode);
        Assert.Equal(3, range.GetProperty("data").GetProperty("candles").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal(3, status.GetProperty("data").GetProperty("availability").GetProperty("storedCandles").GetInt64());
        Assert.Equal("Healthy", status.GetProperty("data").GetProperty("synchronization").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Empty_range_returns_an_empty_success_result()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();
        var from = StartUtc.AddDays(-2);
        var to = from.AddHours(2);

        using var response = await client.GetAsync(
            $"/api/market-data/XAUUSD/candles?timeframe=H1&from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}&limit=10");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body.GetProperty("data").GetProperty("candles").EnumerateArray());
    }

    [Theory]
    [InlineData("/api/market-data/XAUUSD/latest?limit=10")]
    [InlineData("/api/market-data/XAUUSD/latest?timeframe=H2&limit=10")]
    [InlineData("/api/market-data/XAUUSD/latest?timeframe=H1&limit=5001")]
    public async Task Missing_or_invalid_parameters_return_safe_bad_request(string path)
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();

        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Reversed_and_excessive_ranges_are_rejected()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();
        var reversed = $"/api/market-data/XAUUSD/candles?timeframe=H1&from={Uri.EscapeDataString(StartUtc.AddHours(2).ToString("O"))}&to={Uri.EscapeDataString(StartUtc.ToString("O"))}&limit=10";
        var tooLarge = $"/api/market-data/XAUUSD/candles?timeframe=H1&from={Uri.EscapeDataString(StartUtc.AddDays(-367).ToString("O"))}&to={Uri.EscapeDataString(StartUtc.ToString("O"))}&limit=10";

        using var reversedResponse = await client.GetAsync(reversed);
        using var tooLargeResponse = await client.GetAsync(tooLarge);

        Assert.Equal(HttpStatusCode.BadRequest, reversedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLargeResponse.StatusCode);
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            var store = new ApiMarketDataStore(CreateCandles());
            services.RemoveAll<IMarketDataQueryStore>();
            services.RemoveAll<IMarketDataSyncStateStore>();
            services.AddSingleton<IMarketDataQueryStore>(store);
            services.AddSingleton<IMarketDataSyncStateStore>(store);
        }));

    private static IReadOnlyList<StoredMarketCandle> CreateCandles() =>
        [.. Enumerable.Range(0, 3)
            .Select(index => new StoredMarketCandle(
                "XAUUSD",
                "XAUUSD.test",
                MarketTimeframe.H1,
                StartUtc.AddHours(index),
                StartUtc.AddHours(index + 1),
                2300 + index,
                2302 + index,
                2299 + index,
                2301 + index,
                100 + index,
                0,
                20,
                true,
                StartUtc.AddHours(index + 1)))];

    private sealed class ApiMarketDataStore(IReadOnlyList<StoredMarketCandle> candles)
        : IMarketDataQueryStore, IMarketDataSyncStateStore
    {
        public Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(MarketDataQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([.. candles
                .Where(candle => candle.Timeframe == query.Timeframe && candle.OpenTimeUtc >= query.FromUtc && candle.OpenTimeUtc <= query.ToUtc)
                .Take(query.Limit)]);

        public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(string symbol, MarketTimeframe timeframe, int limit, bool completedOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([.. candles
                .Where(candle => candle.Timeframe == timeframe && (!completedOnly || candle.IsComplete))
                .OrderByDescending(candle => candle.OpenTimeUtc)
                .Take(limit)]);

        public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset atUtc, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([.. candles
                .Where(candle => candle.Timeframe == timeframe && candle.IsComplete && candle.CloseTimeUtc <= atUtc)
                .OrderByDescending(candle => candle.OpenTimeUtc)
                .Take(limit)
                .OrderBy(candle => candle.OpenTimeUtc)]);

        public Task<MarketDataAvailability> GetAvailabilityAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MarketDataAvailability(symbol, timeframe, candles.Count, candles.Min(candle => candle.OpenTimeUtc), candles.Max(candle => candle.OpenTimeUtc), candles.Max(candle => candle.OpenTimeUtc)));

        public Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DateTimeOffset>>([.. candles.Select(candle => candle.OpenTimeUtc).Where(value => value >= fromUtc && value <= toUtc)]);

        public Task<MarketDataSyncStatus?> GetStatusAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketDataSyncStatus?>(new MarketDataSyncStatus(symbol, timeframe, "Healthy", StartUtc, StartUtc, StartUtc, StartUtc.AddHours(3), StartUtc.AddHours(2), 0, 0, null, null));

        public Task<Guid> StartRunAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset fromUtc, DateTimeOffset toUtc, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CompleteRunAsync(Guid runId, MarketDataPipelineResult result, DateTimeOffset completedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task FailRunAsync(Guid runId, MarketDataSyncProgress progress, string errorCode, string safeMessage, DateTimeOffset failedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
