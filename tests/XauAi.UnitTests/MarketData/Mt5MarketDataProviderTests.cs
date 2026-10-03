using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.MarketData.Mt5;

namespace XauAi.UnitTests.MarketData;

public sealed class Mt5MarketDataProviderTests
{
    [Fact]
    public async Task Disabled_provider_does_not_start_bridge()
    {
        var bridge = new FakeBridge(_ => throw new InvalidOperationException("Bridge must not be called."));
        var provider = CreateProvider(bridge, enabled: false);

        var status = await provider.GetStatusAsync();

        Assert.Equal(MarketDataProviderState.Disabled, status.State);
        Assert.Equal(0, bridge.CallCount);
    }

    [Fact]
    public async Task Missing_terminal_has_specific_status_without_starting_bridge()
    {
        var bridge = new FakeBridge(_ => throw new InvalidOperationException("Bridge must not be called."));
        var provider = CreateProvider(bridge, fileExists: false);

        var status = await provider.GetStatusAsync();

        Assert.Equal(MarketDataProviderState.TerminalNotFound, status.State);
        Assert.Equal(0, bridge.CallCount);
    }

    [Fact]
    public async Task Connected_status_exposes_only_masked_account_identity()
    {
        var bridge = new FakeBridge(_ => new Mt5BridgeResponse
        {
            Success = true,
            Status = new Mt5BridgeStatus(true, 12345678, "Broker-Demo", "MetaTrader 5", 5000)
        });
        var provider = CreateProvider(bridge);

        var status = await provider.GetStatusAsync();

        Assert.Equal(MarketDataProviderState.Connected, status.State);
        Assert.Equal("****5678", status.AccountLogin);
        Assert.Equal("Broker-Demo", status.Server);
        Assert.DoesNotContain("test-password", status.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authentication_failure_maps_to_safe_status()
    {
        var bridge = new FakeBridge(_ =>
            throw new Mt5BridgeException(MarketDataErrorCodes.AuthenticationFailed, "technical"));
        var provider = CreateProvider(bridge);

        var status = await provider.GetStatusAsync();

        Assert.Equal(MarketDataProviderState.AuthenticationFailed, status.State);
        Assert.Equal("MT5 authentication failed.", status.Message);
    }

    [Fact]
    public async Task Quote_is_normalized_to_application_symbol_and_utc()
    {
        var bridge = new FakeBridge(_ => new Mt5BridgeResponse
        {
            Success = true,
            Quote = new Mt5BridgeQuote("GOLD.a", 2345.12345m, 2345.22345m, 1767225600123)
        });
        var provider = CreateProvider(bridge);

        var quote = await provider.GetQuoteAsync("XAUUSD");

        Assert.Equal("XAUUSD", quote.Symbol);
        Assert.Equal("GOLD.a", quote.ProviderSymbol);
        Assert.Equal(2345.12345m, quote.Bid);
        Assert.Equal(TimeSpan.Zero, quote.TimestampUtc.Offset);
    }

    public static TheoryData<MarketTimeframe, TimeSpan> Timeframes => new()
    {
        { MarketTimeframe.M1, TimeSpan.FromMinutes(1) },
        { MarketTimeframe.M5, TimeSpan.FromMinutes(5) },
        { MarketTimeframe.M15, TimeSpan.FromMinutes(15) },
        { MarketTimeframe.M30, TimeSpan.FromMinutes(30) },
        { MarketTimeframe.H1, TimeSpan.FromHours(1) },
        { MarketTimeframe.H4, TimeSpan.FromHours(4) },
        { MarketTimeframe.D1, TimeSpan.FromDays(1) }
    };

    [Theory]
    [MemberData(nameof(Timeframes))]
    public async Task Candle_mapping_supports_each_required_timeframe(
        MarketTimeframe timeframe,
        TimeSpan duration)
    {
        const long openSeconds = 1767225600;
        var bridge = new FakeBridge(_ => new Mt5BridgeResponse
        {
            Success = true,
            Candles =
            [
                new Mt5BridgeCandle(
                    "GOLD.a",
                    timeframe.Code(),
                    openSeconds,
                    2345.12345678m,
                    2346.12345678m,
                    2344.12345678m,
                    2345.87654321m,
                    123m,
                    0m,
                    25m,
                    true)
            ]
        });
        var provider = CreateProvider(bridge);
        var from = DateTimeOffset.FromUnixTimeSeconds(openSeconds);

        var candles = await provider.GetCandlesAsync("XAUUSD", timeframe, from, from.Add(duration));

        var candle = Assert.Single(candles);
        Assert.Equal(duration, candle.CloseTimeUtc - candle.OpenTimeUtc);
        Assert.Equal(TimeSpan.Zero, candle.OpenTimeUtc.Offset);
        Assert.Equal(2345.12345678m, candle.Open);
        Assert.Equal(123m, candle.TickVolume);
        Assert.Equal(0m, candle.RealVolume);
        Assert.Equal(25m, candle.Spread);
    }

    [Fact]
    public async Task Timeout_is_returned_as_safe_application_error()
    {
        var bridge = new FakeBridge(_ =>
            throw new Mt5BridgeException(MarketDataErrorCodes.Timeout, "technical timeout"));
        var provider = CreateProvider(bridge);

        var exception = await Assert.ThrowsAsync<MarketDataException>(() =>
            provider.GetQuoteAsync("XAUUSD"));

        Assert.Equal(MarketDataErrorCodes.Timeout, exception.Code);
        Assert.Equal("The MT5 request timed out.", exception.SafeMessage);
    }

    [Fact]
    public async Task Unknown_application_symbol_is_rejected_before_bridge_call()
    {
        var bridge = new FakeBridge(_ => throw new InvalidOperationException("Bridge must not be called."));
        var provider = CreateProvider(bridge);

        var exception = await Assert.ThrowsAsync<MarketDataException>(() =>
            provider.GetQuoteAsync("EURUSD"));

        Assert.Equal(MarketDataErrorCodes.SymbolNotFound, exception.Code);
        Assert.Equal(0, bridge.CallCount);
    }

    [Fact]
    public async Task Cancellation_is_not_converted_to_provider_failure()
    {
        var bridge = new FakeBridge(_ => throw new OperationCanceledException());
        var provider = CreateProvider(bridge);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.GetQuoteAsync("XAUUSD"));
    }

    private static Mt5MarketDataProvider CreateProvider(
        IMt5BridgeClient bridge,
        bool enabled = true,
        bool fileExists = true)
    {
        var options = Options.Create(new Mt5Options
        {
            Enabled = enabled,
            Login = "12345678",
            Password = "test-password",
            Server = "Broker-Demo",
            TerminalPath = "C:\\Program Files\\MetaTrader 5\\terminal64.exe",
            ApplicationSymbol = "XAUUSD",
            Symbol = "GOLD.a",
            MaxBarsPerRequest = 10000
        });
        return new Mt5MarketDataProvider(
            options,
            bridge,
            new FakeHostEnvironment(fileExists),
            new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero)),
            NullLogger<Mt5MarketDataProvider>.Instance);
    }

    private sealed class FakeBridge(Func<Mt5BridgeRequest, Mt5BridgeResponse> execute) : IMt5BridgeClient
    {
        public int CallCount { get; private set; }

        public Task<Mt5BridgeResponse> ExecuteAsync(
            Mt5BridgeRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(execute(request));
        }
    }

    private sealed class FakeHostEnvironment(bool fileExists) : IMt5HostEnvironment
    {
        public bool IsWindows => true;

        public bool FileExists(string path) => fileExists;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
