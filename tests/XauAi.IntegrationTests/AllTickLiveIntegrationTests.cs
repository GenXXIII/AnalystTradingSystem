using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XauAi.Application;
using XauAi.Application.MarketData;
using XauAi.Infrastructure;

namespace XauAi.IntegrationTests;

public sealed class AllTickLiveIntegrationTests
{
    [AllTickLiveFact]
    public async Task Live_alltick_history_is_normalized_without_a_desktop_terminal()
    {
        var token = Environment.GetEnvironmentVariable("ALLTICK_TOKEN")
            ?? throw new InvalidOperationException("ALLTICK_TOKEN is required.");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = "XAUUSD-AI AllTick Integration Tests",
                ["Application:Version"] = "1.0.0-test",
                ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
                ["AllTick:Enabled"] = "true",
                ["AllTick:Token"] = token,
                ["MarketData:Provider"] = "AllTick",
                ["MarketData:ProviderKey"] = "alltick"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        await using var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IMarketDataProvider>();
        var toUtc = DateTimeOffset.UtcNow;

        var candles = await provider.GetCandlesAsync(
            "XAUUSD",
            MarketTimeframe.H1,
            toUtc.AddDays(-7),
            toUtc);

        Assert.NotEmpty(candles);
        Assert.All(candles, candle =>
        {
            Assert.Equal("XAUUSD", candle.Symbol);
            Assert.Equal("GOLD", candle.ProviderSymbol);
            Assert.Equal(MarketTimeframe.H1, candle.Timeframe);
            Assert.True(candle.High >= candle.Low);
            Assert.True(candle.Open > 0);
            Assert.True(candle.Close > 0);
        });
    }
}

public sealed class AllTickLiveFactAttribute : FactAttribute
{
    public AllTickLiveFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("XAUAI_RUN_ALLTICK_INTEGRATION"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set XAUAI_RUN_ALLTICK_INTEGRATION=true to run the live AllTick integration test.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ALLTICK_TOKEN")))
        {
            Skip = "Set ALLTICK_TOKEN to run the live AllTick integration test.";
        }
    }
}
