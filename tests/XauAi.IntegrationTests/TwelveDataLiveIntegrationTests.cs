using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XauAi.Application;
using XauAi.Application.MarketData;
using XauAi.Infrastructure;

namespace XauAi.IntegrationTests;

public sealed class TwelveDataLiveIntegrationTests
{
    [TwelveDataLiveFact]
    public async Task Live_twelve_data_history_is_normalized_as_reference_candles()
    {
        var apiKey = Environment.GetEnvironmentVariable("TWELVE_DATA_API_KEY")
            ?? throw new InvalidOperationException("TWELVE_DATA_API_KEY is required.");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = "XAUUSD-AI Twelve Data Integration Tests",
                ["Application:Version"] = "1.0.0-test",
                ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
                ["TwelveData:Enabled"] = "true",
                ["TwelveData:ApiKey"] = apiKey
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        await using var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IReferenceMarketDataProvider>();
        var toUtc = DateTimeOffset.UtcNow;

        var candles = await provider.GetCandlesAsync(
            "XAUUSD",
            MarketTimeframe.M1,
            toUtc.AddHours(-1),
            toUtc);

        Assert.NotEmpty(candles);
        Assert.All(candles, candle =>
        {
            Assert.Equal("XAU/USD", candle.ProviderSymbol);
            Assert.Equal("twelvedata", candle.ProviderKey);
            Assert.True(candle.High >= candle.Low);
            Assert.True(candle.Open > 0);
            Assert.True(candle.Close > 0);
        });
    }
}

public sealed class TwelveDataLiveFactAttribute : FactAttribute
{
    public TwelveDataLiveFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("XAUAI_RUN_TWELVE_DATA_INTEGRATION"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set XAUAI_RUN_TWELVE_DATA_INTEGRATION=true to run the live Twelve Data integration test.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TWELVE_DATA_API_KEY")))
        {
            Skip = "Set TWELVE_DATA_API_KEY to run the live Twelve Data integration test.";
        }
    }
}
