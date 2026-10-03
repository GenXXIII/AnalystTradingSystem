using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.MarketData.AllTick;

namespace XauAi.UnitTests.MarketData;

public sealed class AllTickMarketDataTests
{
    [Fact]
    public void Websocket_protocol_parses_trade_and_order_book_timestamps()
    {
        const string tradeJson = """
            {"cmd_id":22998,"data":{"code":"GOLD","tick_time":"1791000000","price":"2675.25","volume":"3"}}
            """;
        const string orderBookJson = """
            {"cmd_id":22999,"data":{"code":"GOLD","tick_time":"1791000000123","bids":[{"price":"2675.20"}],"asks":[{"price":"2675.30"}]}}
            """;

        Assert.True(AllTickWebSocketProtocol.TryReadTrade(tradeJson, out var trade));
        Assert.True(AllTickWebSocketProtocol.TryReadOrderBook(orderBookJson, out var orderBook));

        Assert.Equal(2675.25m, trade!.Price);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1791000000), trade.TimestampUtc);
        Assert.Equal(2675.20m, orderBook!.Bid);
        Assert.Equal(2675.30m, orderBook.Ask);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1791000000123), orderBook.TimestampUtc);
    }

    [Fact]
    public void Realtime_accumulator_builds_ohlc_but_keeps_candles_provisional()
    {
        var accumulator = new AllTickCandleAccumulator(
            "XAUUSD",
            "GOLD",
            "alltick",
            [MarketTimeframe.M1]);
        var start = new DateTimeOffset(2026, 10, 3, 10, 0, 1, TimeSpan.Zero);

        accumulator.ApplyTick(start, 2675m, 1m, start);
        accumulator.ApplyTick(start.AddSeconds(10), 2677m, 2m, start.AddSeconds(10));
        accumulator.ApplyTick(start.AddSeconds(20), 2674m, 3m, start.AddSeconds(20));
        accumulator.ApplyTick(start.AddSeconds(30), 2676m, 4m, start.AddSeconds(30));

        var candle = Assert.Single(accumulator.Snapshot(start.AddSeconds(30)));
        Assert.Equal(2675m, candle.Open);
        Assert.Equal(2677m, candle.High);
        Assert.Equal(2674m, candle.Low);
        Assert.Equal(2676m, candle.Close);
        Assert.Equal(10m, candle.TickVolume);
        Assert.False(candle.IsComplete);
    }

    [Fact]
    public async Task Http_client_normalizes_documented_kline_response()
    {
        const string responseJson = """
            {
              "ret": 200,
              "msg": "ok",
              "data": {
                "code": "GOLD",
                "kline_type": 1,
                "kline_list": [
                  {"timestamp":"1791000000","open_price":"2675.1","close_price":"2676.2","high_price":"2677.3","low_price":"2674.0","volume":"12","turnover":"0"}
                ]
              }
            }
            """;
        var handler = new StubHttpMessageHandler(responseJson);
        var options = Options();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(options.HttpBaseUrl) };
        var client = new AllTickHttpClient(
            httpClient,
            options,
            TimeProvider.System,
            NullLogger<AllTickHttpClient>.Instance);
        var from = DateTimeOffset.FromUnixTimeSeconds(1791000000);

        var candles = await client.GetCandlesAsync(
            MarketTimeframe.M1,
            from,
            from.AddMinutes(1),
            CancellationToken.None);

        var candle = Assert.Single(candles);
        Assert.Equal(2675.1m, candle.Open);
        Assert.Equal(2677.3m, candle.High);
        Assert.Equal(2674.0m, candle.Low);
        Assert.Equal(2676.2m, candle.Close);
        Assert.Equal(12m, candle.Volume);
        Assert.Contains("/quote-b-api/kline", handler.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(MarketTimeframe.M1, 1)]
    [InlineData(MarketTimeframe.H4, 7)]
    [InlineData(MarketTimeframe.D1, 8)]
    public void Timeframes_map_to_documented_AllTick_codes(MarketTimeframe timeframe, int providerCode)
    {
        Assert.Equal(providerCode, AllTickHttpClient.ToProviderTimeframe(timeframe));
        Assert.Equal(timeframe, AllTickHttpClient.FromProviderTimeframe(providerCode));
    }

    private static AllTickOptions Options() => new()
    {
        Enabled = true,
        Token = "secret-token",
        MinimumHttpRequestIntervalSeconds = 10
    };

    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
