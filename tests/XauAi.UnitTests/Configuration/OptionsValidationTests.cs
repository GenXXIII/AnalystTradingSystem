using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.UnitTests.Configuration;

public sealed class OptionsValidationTests
{
    [Fact]
    public void Disabled_integrations_do_not_require_credentials()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "USER_PROVIDED_LATER",
            ["Redis:ConnectionString"] = "USER_PROVIDED_LATER",
            ["AI:ApiKey"] = "USER_PROVIDED_LATER",
            ["AI:Model"] = "MODEL_DEFINED_LATER",
            ["MT5:Login"] = "USER_PROVIDED_LATER",
            ["MT5:Password"] = "USER_PROVIDED_LATER",
            ["TwelveData:ApiKey"] = "USER_PROVIDED_LATER",
            ["News:ApiKey"] = "USER_PROVIDED_LATER",
            ["EconomicData:ApiKey"] = "USER_PROVIDED_LATER",
            ["Analysts:ApiKey"] = "USER_PROVIDED_LATER"
        });

        Assert.False(Get<DatabaseOptions>(provider).Enabled);
        Assert.False(Get<RedisOptions>(provider).Enabled);
        Assert.False(Get<AiOptions>(provider).Enabled);
        Assert.False(Get<Mt5Options>(provider).Enabled);
        Assert.False(Get<TwelveDataOptions>(provider).Enabled);
        Assert.False(Get<NewsOptions>(provider).Enabled);
        Assert.False(Get<EconomicDataOptions>(provider).Enabled);
        Assert.False(Get<AnalystOptions>(provider).Enabled);
    }

    [Fact]
    public void Configuration_binds_to_strongly_typed_options()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Application:Name"] = "Test Intelligence",
            ["AI:Provider"] = "ReplaceableProvider",
            ["AI:Model"] = "analysis-model",
            ["AI:Temperature"] = "0.35",
            ["AI:MaxOutputTokens"] = "4096",
            ["Redis:InstanceName"] = "Tests:"
        });

        var application = Get<ApplicationOptions>(provider);
        var ai = Get<AiOptions>(provider);
        var redis = Get<RedisOptions>(provider);

        Assert.Equal("Test Intelligence", application.Name);
        Assert.Equal("ReplaceableProvider", ai.Provider);
        Assert.Equal("analysis-model", ai.Model);
        Assert.Equal(0.35, ai.Temperature);
        Assert.Equal(4096, ai.MaxOutputTokens);
        Assert.Equal("Tests:", redis.InstanceName);
    }

    [Fact]
    public void Enabled_ai_requires_a_real_key_when_the_provider_requires_one()
    {
        var configuration = ValidEnabledAiConfiguration();
        configuration["AI:ApiKey"] = "USER_PROVIDED_LATER";
        using var provider = BuildProvider(configuration);

        var exception = Assert.Throws<OptionsValidationException>(() => Get<AiOptions>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains("AI:ApiKey", StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_ai_can_use_a_provider_that_does_not_require_a_key()
    {
        var configuration = ValidEnabledAiConfiguration();
        configuration["AI:RequiresApiKey"] = "false";
        configuration["AI:ApiKey"] = string.Empty;
        using var provider = BuildProvider(configuration);

        var options = Get<AiOptions>(provider);

        Assert.True(options.Enabled);
        Assert.False(options.RequiresApiKey);
    }

    [Fact]
    public void Enabled_database_requires_a_non_placeholder_connection_string()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Database:Enabled"] = "true",
            ["Database:ConnectionString"] = "USER_PROVIDED_LATER"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => Get<DatabaseOptions>(provider));

        Assert.Contains(exception.Failures, failure =>
            failure.Contains("Database:ConnectionString", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("MT5:Login")]
    [InlineData("MT5:Password")]
    [InlineData("MT5:Server")]
    [InlineData("MT5:TerminalPath")]
    public void Enabled_mt5_requires_each_connection_setting(string missingKey)
    {
        var configuration = ValidEnabledMt5Configuration();
        configuration[missingKey] = "USER_PROVIDED_LATER";
        using var provider = BuildProvider(configuration);

        var exception = Assert.Throws<OptionsValidationException>(() => Get<Mt5Options>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains(missingKey, StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_mt5_rejects_non_executable_terminal_path()
    {
        var configuration = ValidEnabledMt5Configuration();
        configuration["MT5:TerminalPath"] = "terminal.txt";
        using var provider = BuildProvider(configuration);

        var exception = Assert.Throws<OptionsValidationException>(() => Get<Mt5Options>(provider));

        Assert.Contains(exception.Failures, failure =>
            failure.Contains("MT5:TerminalPath", StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_mt5_accepts_complete_configuration_without_connecting()
    {
        using var provider = BuildProvider(ValidEnabledMt5Configuration());

        var options = Get<Mt5Options>(provider);

        Assert.True(options.Enabled);
        Assert.Equal("XAUUSD.broker", options.Symbol);
    }

    [Fact]
    public void Enabled_alltick_requires_a_real_token()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["AllTick:Enabled"] = "true",
            ["AllTick:Token"] = "USER_PROVIDED_LATER"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => Get<AllTickOptions>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains("AllTick:Token", StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_alltick_accepts_docker_native_configuration_without_connecting()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["AllTick:Enabled"] = "true",
            ["AllTick:Token"] = "test-only-token",
            ["AllTick:Symbol"] = "GOLD"
        });

        var options = Get<AllTickOptions>(provider);

        Assert.True(options.Enabled);
        Assert.Equal("GOLD", options.Symbol);
        Assert.Equal(500, options.MaxBarsPerRequest);
    }

    [Fact]
    public void Enabled_twelve_data_requires_a_real_api_key()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["TwelveData:Enabled"] = "true",
            ["TwelveData:ApiKey"] = "USER_PROVIDED_LATER"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => Get<TwelveDataOptions>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains("TwelveData:ApiKey", StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_twelve_data_accepts_bounded_reference_configuration_without_connecting()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["TwelveData:Enabled"] = "true",
            ["TwelveData:ApiKey"] = "test-only-key",
            ["TwelveData:Symbol"] = "XAU/USD",
            ["TwelveData:ProviderKey"] = "twelvedata"
        });

        var options = Get<TwelveDataOptions>(provider);

        Assert.True(options.Enabled);
        Assert.Equal("XAU/USD", options.Symbol);
        Assert.Equal(5000, options.MaximumPointsPerRequest);
    }

    [Fact]
    public void Enabled_news_requires_supported_provider_url_and_real_key()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["News:Enabled"] = "true",
            ["News:Provider"] = "OtherNews",
            ["News:BaseUrl"] = "not-a-url",
            ["News:ApiKey"] = "USER_PROVIDED_LATER"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => Get<NewsOptions>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains("News:Provider", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("News:BaseUrl", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("News:ApiKey", StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_news_accepts_complete_configuration_without_calling_provider()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["News:Enabled"] = "true",
            ["News:Provider"] = "NewsData",
            ["News:BaseUrl"] = "https://newsdata.io/api/1/",
            ["News:ApiKey"] = "unit-test-key",
            ["News:MinimumRelevance"] = "High"
        });

        var options = Get<NewsOptions>(provider);

        Assert.True(options.Enabled);
        Assert.Equal("NewsData", options.Provider);
        Assert.Equal("High", options.MinimumRelevance);
    }

    [Fact]
    public void Enabled_economic_data_requires_fred_key_url_and_valid_series_configuration()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["EconomicData:Enabled"] = "true",
            ["EconomicData:Provider"] = "OtherProvider",
            ["EconomicData:ApiKey"] = "USER_PROVIDED_LATER",
            ["EconomicData:BaseUrl"] = "not-a-url",
            ["EconomicData:TrackedSeries"] = "BROKEN"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => Get<EconomicDataOptions>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains("EconomicData:Provider", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("EconomicData:ApiKey", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("EconomicData:BaseUrl", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("EconomicData:TrackedSeries", StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_economic_data_accepts_bounded_fred_configuration_without_calling_provider()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["EconomicData:Enabled"] = "true",
            ["EconomicData:Provider"] = "FRED",
            ["EconomicData:ProviderKey"] = "fred",
            ["EconomicData:ApiKey"] = "unit-test-fred-key",
            ["EconomicData:BaseUrl"] = "https://api.stlouisfed.org/fred/",
            ["EconomicData:TrackedSeries"] = "CPIAUCSL:Inflation,DGS10:Treasury",
            ["EconomicData:SyncIntervalMinutes"] = "360",
            ["EconomicData:ProviderPageSize"] = "1000"
        });

        var options = Get<EconomicDataOptions>(provider);

        Assert.True(options.Enabled);
        Assert.Equal("FRED", options.Provider);
        Assert.Equal("CPIAUCSL:Inflation,DGS10:Treasury", options.TrackedSeries);
    }

    [Fact]
    public void Market_data_rejects_unknown_timeframes_and_unsafe_limits()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["MarketData:Timeframes"] = "M5,H2",
            ["MarketData:BatchSize"] = "1",
            ["MarketData:MaxApiLimit"] = "10001"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => Get<MarketDataOptions>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains("MarketData:Timeframes", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("MarketData:BatchSize", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("MarketData:MaxApiLimit", StringComparison.Ordinal));
    }

    [Fact]
    public void Alltick_market_data_enforces_free_tier_batch_and_polling_limits()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["MarketData:Provider"] = "AllTick",
            ["MarketData:ProviderKey"] = "alltick",
            ["MarketData:SyncEnabled"] = "true",
            ["MarketData:SyncIntervalSeconds"] = "10",
            ["MarketData:BatchSize"] = "1000"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => Get<MarketDataOptions>(provider));

        Assert.Contains(exception.Failures, failure => failure.Contains("MarketData:SyncIntervalSeconds", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("MarketData:BatchSize", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_configuration_fails_with_keys_but_never_secret_values()
    {
        const string sentinelSecret = "unit-test-secret-must-not-leak";
        var configuration = ValidEnabledAiConfiguration();
        configuration["AI:ApiKey"] = sentinelSecret;
        configuration["AI:Model"] = string.Empty;
        configuration["AI:DailyBudgetUsd"] = "0";
        using var provider = BuildProvider(configuration);

        var exception = Assert.Throws<OptionsValidationException>(() => Get<AiOptions>(provider));
        var combinedFailures = string.Join(Environment.NewLine, exception.Failures);

        Assert.Contains("AI:Model", combinedFailures, StringComparison.Ordinal);
        Assert.Contains("AI:DailyBudgetUsd", combinedFailures, StringComparison.Ordinal);
        Assert.DoesNotContain(sentinelSecret, combinedFailures, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider(IEnumerable<KeyValuePair<string, string?>> overrides)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Application:Name"] = "XAUUSD-AI Tests",
            ["Application:Version"] = "1.0.0-test",
            ["Api:Cors:AllowedOrigins:0"] = "http://localhost"
        };

        foreach (var pair in overrides)
        {
            values[pair.Key] = pair.Value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static TOptions Get<TOptions>(IServiceProvider provider)
        where TOptions : class => provider.GetRequiredService<IOptions<TOptions>>().Value;

    private static Dictionary<string, string?> ValidEnabledAiConfiguration() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AI:Enabled"] = "true",
            ["AI:Provider"] = "TestProvider",
            ["AI:RequiresApiKey"] = "true",
            ["AI:ApiKey"] = "test-only-key",
            ["AI:Model"] = "test-model",
            ["AI:DailyBudgetUsd"] = "1.00",
            ["AI:DailyRequestLimit"] = "10"
        };

    private static Dictionary<string, string?> ValidEnabledMt5Configuration() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["MT5:Enabled"] = "true",
            ["MT5:Login"] = "12345678",
            ["MT5:Password"] = "unit-test-password",
            ["MT5:Server"] = "Broker-Demo",
            ["MT5:TerminalPath"] = Path.GetFullPath("terminal64.exe"),
            ["MT5:ApplicationSymbol"] = "XAUUSD",
            ["MT5:Symbol"] = "XAUUSD.broker",
            ["MT5:TimeZone"] = "UTC",
            ["MT5:PythonExecutable"] = "py"
        };
}
