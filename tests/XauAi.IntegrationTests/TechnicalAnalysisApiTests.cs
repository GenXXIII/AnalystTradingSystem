using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.IntegrationTests;

public sealed class TechnicalAnalysisApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateTimeOffset StartUtc = new(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Single_timeframe_endpoint_returns_typed_application_analysis()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();
        var cutoff = StartUtc.AddHours(300);

        using var response = await client.GetAsync(
            $"/api/analysis/XAUUSD/H1?atUtc={Uri.EscapeDataString(cutoff.ToString("O"))}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = body.GetProperty("data");
        Assert.Equal("XAUUSD", data.GetProperty("symbol").GetString());
        Assert.Equal("H1", data.GetProperty("timeframe").GetString());
        Assert.True(data.GetProperty("diagnostics").GetProperty("candlesUsed").GetInt32() >= 200);
        Assert.True(data.GetProperty("indicators").TryGetProperty("macd", out _));
        Assert.True(data.TryGetProperty("confluence", out _));
        Assert.False(data.TryGetProperty("signal", out _));
    }

    [Fact]
    public async Task Multi_timeframe_endpoint_returns_alignment_and_independent_results()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();

        using var response = await client.GetAsync("/api/analysis/XAUUSD/multi-timeframe");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = body.GetProperty("data");
        Assert.Equal(2, data.GetProperty("timeframes").GetArrayLength());
        Assert.Equal(2, data.GetProperty("analyses").EnumerateObject().Count());
        Assert.NotEqual(string.Empty, data.GetProperty("trendAlignment").GetString());
    }

    [Theory]
    [InlineData("/api/analysis/XAUUSD/H2")]
    [InlineData("/api/analysis/EURUSD/H1")]
    public async Task Invalid_analysis_requests_return_safe_bad_request(string path)
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
    public async Task Historical_endpoint_never_uses_candles_after_cutoff()
    {
        using var configuredFactory = CreateFactory();
        using var client = configuredFactory.CreateClient();
        var cutoff = StartUtc.AddHours(100);

        using var response = await client.GetAsync(
            $"/api/analysis/XAUUSD/H1?atUtc={Uri.EscapeDataString(cutoff.ToString("O"))}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = body.GetProperty("data");
        Assert.Equal(cutoff, data.GetProperty("lastCandleCloseTimeUtc").GetDateTimeOffset());
        Assert.Equal(cutoff, data.GetProperty("analyzedAtUtc").GetDateTimeOffset());
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            var candles = new List<StoredMarketCandle>();
            candles.AddRange(CreateCandles(MarketTimeframe.H1, 300));
            candles.AddRange(CreateCandles(MarketTimeframe.M15, 300));
            services.RemoveAll<IMarketDataQueryStore>();
            services.RemoveAll<TechnicalAnalysisSettings>();
            services.AddSingleton<IMarketDataQueryStore>(new ApiAnalysisStore(candles));
            services.AddSingleton(new TechnicalAnalysisSettings
            {
                Enabled = true,
                Timeframes = [MarketTimeframe.H1, MarketTimeframe.M15]
            });
        }));

    private static IReadOnlyList<StoredMarketCandle> CreateCandles(MarketTimeframe timeframe, int count) =>
        [.. Enumerable.Range(0, count).Select(index =>
        {
            var openTime = StartUtc.AddTicks(timeframe.Duration().Ticks * index);
            var close = 2000m + (index * 0.25m);
            return new StoredMarketCandle(
                "XAUUSD",
                "XAUUSD.test",
                timeframe,
                openTime,
                openTime.Add(timeframe.Duration()),
                close - 0.1m,
                close + 0.5m,
                close - 0.5m,
                close,
                100 + index,
                0,
                20,
                true,
                openTime.Add(timeframe.Duration()));
        })];

    private sealed class ApiAnalysisStore(IReadOnlyList<StoredMarketCandle> candles) : IMarketDataQueryStore
    {
        public Task<IReadOnlyList<StoredMarketCandle>> GetHistoryUpToAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset atUtc, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMarketCandle>>([.. candles
                .Where(candle => candle.Timeframe == timeframe && candle.IsComplete && candle.CloseTimeUtc <= atUtc)
                .OrderByDescending(candle => candle.OpenTimeUtc)
                .Take(limit)
                .OrderBy(candle => candle.OpenTimeUtc)]);

        public Task<IReadOnlyList<StoredMarketCandle>> GetRangeAsync(MarketDataQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(string symbol, MarketTimeframe timeframe, int limit, bool completedOnly, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MarketDataAvailability> GetAvailabilityAsync(string symbol, MarketTimeframe timeframe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DateTimeOffset>> GetOpenTimesAsync(string symbol, MarketTimeframe timeframe, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
