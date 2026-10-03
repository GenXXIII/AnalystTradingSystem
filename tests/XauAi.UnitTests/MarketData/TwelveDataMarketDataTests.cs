using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.MarketData.TwelveData;

namespace XauAi.UnitTests.MarketData;

public sealed class TwelveDataMarketDataTests
{
    [Fact]
    public async Task Provider_normalizes_utc_time_series_response_with_source_identity()
    {
        const string responseJson = """
            {
              "meta": {"symbol":"XAU/USD","interval":"1min","currency_base":"Gold Spot","currency_quote":"US Dollar"},
              "values": [
                {"datetime":"2026-10-03 10:00:00","open":"2675.10","high":"2677.30","low":"2674.00","close":"2676.20","volume":"12"}
              ],
              "status":"ok"
            }
            """;
        var options = Options();
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, responseJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) };
        var rawClient = new TwelveDataHttpClient(
            httpClient,
            options,
            TimeProvider.System,
            NullLogger<TwelveDataHttpClient>.Instance);
        var provider = new TwelveDataMarketDataProvider(
            options,
            rawClient,
            TimeProvider.System,
            NullLogger<TwelveDataMarketDataProvider>.Instance);

        var candles = await provider.GetCandlesAsync(
            "XAUUSD",
            MarketTimeframe.M1,
            new DateTimeOffset(2026, 10, 3, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 3, 10, 1, 0, TimeSpan.Zero));

        var candle = Assert.Single(candles);
        Assert.Equal("twelvedata", candle.ProviderKey);
        Assert.Equal("XAU/USD", candle.ProviderSymbol);
        Assert.Equal(2675.10m, candle.Open);
        Assert.Equal(2677.30m, candle.High);
        Assert.Equal(2674.00m, candle.Low);
        Assert.Equal(2676.20m, candle.Close);
        Assert.Equal(TimeSpan.Zero, candle.OpenTimeUtc.Offset);
        Assert.Contains("timezone=UTC", handler.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("interval=1min", handler.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Provider_error_is_safe_and_does_not_expose_key_or_provider_message()
    {
        const string responseJson = """
            {"code":401,"message":"bad secret-key-for-test","status":"error"}
            """;
        var options = Options();
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, responseJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) };
        var rawClient = new TwelveDataHttpClient(
            httpClient,
            options,
            TimeProvider.System,
            NullLogger<TwelveDataHttpClient>.Instance);

        var exception = await Assert.ThrowsAsync<MarketDataException>(() => rawClient.GetCandlesAsync(
            MarketTimeframe.M1,
            DateTimeOffset.UtcNow.AddMinutes(-2),
            DateTimeOffset.UtcNow,
            CancellationToken.None));

        Assert.Equal(MarketDataErrorCodes.ProviderAuthenticationFailed, exception.Code);
        Assert.DoesNotContain(options.ApiKey, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("bad secret", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(MarketTimeframe.M1, "1min")]
    [InlineData(MarketTimeframe.H4, "4h")]
    [InlineData(MarketTimeframe.D1, "1day")]
    public void Timeframes_map_to_documented_intervals(MarketTimeframe timeframe, string interval) =>
        Assert.Equal(interval, TwelveDataHttpClient.ToProviderInterval(timeframe));

    private static TwelveDataOptions Options() => new()
    {
        Enabled = true,
        ApiKey = "secret-key-for-test",
        MinimumRequestIntervalSeconds = 8
    };

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseJson) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
