using Microsoft.Extensions.Configuration;
using XauAi.Infrastructure.Configuration;

namespace XauAi.UnitTests.Configuration;

public sealed class EnvironmentVariableConfigurationTests
{
    [Fact]
    public void Friendly_environment_variables_map_to_hierarchical_configuration()
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["AI_ENABLED"] = "true",
            ["AI_PROVIDER"] = "TestProvider",
            ["OPENAI_API_KEY"] = "test-key",
            ["AI_MAX_OUTPUT_TOKENS"] = "1234",
            ["CORS_ALLOWED_ORIGINS"] = "https://app.example.test, https://admin.example.test",
            ["DATABASE_CONNECTION_STRING"] = "test-connection",
            ["DATABASE_APPLY_MIGRATIONS_ON_STARTUP"] = "true",
            ["MT5_REQUEST_TIMEOUT_SECONDS"] = "45",
            ["MT5_MAX_BARS_PER_REQUEST"] = "5000",
            ["MT5_APPLICATION_SYMBOL"] = "XAUUSD",
            ["MT5_SYMBOL"] = "GOLD.a",
            ["ALLTICK_ENABLED"] = "true",
            ["ALLTICK_TOKEN"] = "test-alltick-token",
            ["ALLTICK_SYMBOL"] = "GOLD",
            ["TWELVE_DATA_ENABLED"] = "true",
            ["TWELVE_DATA_API_KEY"] = "test-twelve-data-key",
            ["TWELVE_DATA_SYMBOL"] = "XAU/USD",
            ["TWELVE_DATA_REFERENCE_SYNC_INTERVAL_SECONDS"] = "900",
            ["MARKET_DATA_PROVIDER"] = "AllTick",
            ["MARKET_DATA_PROVIDER_KEY"] = "alltick",
            ["MARKET_DATA_SYNC_ENABLED"] = "true",
            ["MARKET_DATA_TIMEFRAMES"] = "M5,H1,D1",
            ["MARKET_DATA_INITIAL_HISTORY_DAYS"] = "30",
            ["MARKET_DATA_BATCH_SIZE"] = "750",
            ["MARKET_DATA_MAX_API_LIMIT"] = "2500",
            ["TECHNICAL_ANALYSIS_ENABLED"] = "true",
            ["TECHNICAL_ANALYSIS_TIMEFRAMES"] = "M15,H1,H4",
            ["TECHNICAL_ANALYSIS_HISTORY_LIMIT"] = "1200",
            ["EMA_PERIODS"] = "8,21,55",
            ["RSI_PERIOD"] = "12",
            ["BOLLINGER_STANDARD_DEVIATION"] = "2.5"
            ,
            ["NEWS_ENABLED"] = "true"
            ,
            ["NEWS_PROVIDER"] = "NewsData"
            ,
            ["NEWS_API_KEY"] = "test-news-key"
            ,
            ["NEWS_BASE_URL"] = "https://newsdata.test/api/1/"
            ,
            ["NEWS_PROVIDER_QUERY"] = "gold OR FOMC"
            ,
            ["NEWS_COLLECTION_INTERVAL_SECONDS"] = "600"
            ,
            ["NEWS_MINIMUM_RELEVANCE"] = "High"
            ,
            ["ECONOMIC_DATA_ENABLED"] = "true"
            ,
            ["ECONOMIC_DATA_PROVIDER"] = "FRED"
            ,
            ["FRED_API_KEY"] = "test-fred-key"
            ,
            ["ECONOMIC_DATA_BASE_URL"] = "https://fred.test/fred/"
            ,
            ["ECONOMIC_DATA_TRACKED_SERIES"] = "TEST_CPI:Inflation"
        };

        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => environment.GetValueOrDefault(name))
            .Build();

        Assert.Equal("true", configuration["AI:Enabled"]);
        Assert.Equal("TestProvider", configuration["AI:Provider"]);
        Assert.Equal("test-key", configuration["AI:ApiKey"]);
        Assert.Equal("1234", configuration["AI:MaxOutputTokens"]);
        Assert.Equal("https://app.example.test", configuration["Api:Cors:AllowedOrigins:0"]);
        Assert.Equal("https://admin.example.test", configuration["Api:Cors:AllowedOrigins:1"]);
        Assert.Equal("test-connection", configuration["Database:ConnectionString"]);
        Assert.Equal("true", configuration["Database:ApplyMigrationsOnStartup"]);
        Assert.Equal("45", configuration["MT5:RequestTimeoutSeconds"]);
        Assert.Equal("5000", configuration["MT5:MaxBarsPerRequest"]);
        Assert.Equal("XAUUSD", configuration["MT5:ApplicationSymbol"]);
        Assert.Equal("GOLD.a", configuration["MT5:Symbol"]);
        Assert.Equal("true", configuration["AllTick:Enabled"]);
        Assert.Equal("test-alltick-token", configuration["AllTick:Token"]);
        Assert.Equal("GOLD", configuration["AllTick:Symbol"]);
        Assert.Equal("true", configuration["TwelveData:Enabled"]);
        Assert.Equal("test-twelve-data-key", configuration["TwelveData:ApiKey"]);
        Assert.Equal("XAU/USD", configuration["TwelveData:Symbol"]);
        Assert.Equal("900", configuration["TwelveData:ReferenceSyncIntervalSeconds"]);
        Assert.Equal("AllTick", configuration["MarketData:Provider"]);
        Assert.Equal("alltick", configuration["MarketData:ProviderKey"]);
        Assert.Equal("true", configuration["MarketData:SyncEnabled"]);
        Assert.Equal("M5,H1,D1", configuration["MarketData:Timeframes"]);
        Assert.Equal("30", configuration["MarketData:InitialHistoryDays"]);
        Assert.Equal("750", configuration["MarketData:BatchSize"]);
        Assert.Equal("2500", configuration["MarketData:MaxApiLimit"]);
        Assert.Equal("true", configuration["TechnicalAnalysis:Enabled"]);
        Assert.Equal("M15,H1,H4", configuration["TechnicalAnalysis:Timeframes"]);
        Assert.Equal("1200", configuration["TechnicalAnalysis:HistoryLimit"]);
        Assert.Equal("8,21,55", configuration["TechnicalAnalysis:EmaPeriods"]);
        Assert.Equal("12", configuration["TechnicalAnalysis:RsiPeriod"]);
        Assert.Equal("2.5", configuration["TechnicalAnalysis:BollingerStandardDeviations"]);
        Assert.Equal("true", configuration["News:Enabled"]);
        Assert.Equal("NewsData", configuration["News:Provider"]);
        Assert.Equal("test-news-key", configuration["News:ApiKey"]);
        Assert.Equal("https://newsdata.test/api/1/", configuration["News:BaseUrl"]);
        Assert.Equal("gold OR FOMC", configuration["News:ProviderQuery"]);
        Assert.Equal("600", configuration["News:CollectionIntervalSeconds"]);
        Assert.Equal("High", configuration["News:MinimumRelevance"]);
        Assert.Equal("true", configuration["EconomicData:Enabled"]);
        Assert.Equal("FRED", configuration["EconomicData:Provider"]);
        Assert.Equal("test-fred-key", configuration["EconomicData:ApiKey"]);
        Assert.Equal("https://fred.test/fred/", configuration["EconomicData:BaseUrl"]);
        Assert.Equal("TEST_CPI:Inflation", configuration["EconomicData:TrackedSeries"]);
    }

    [Fact]
    public void Provider_neutral_ai_key_takes_precedence_over_provider_alias()
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["AI_API_KEY"] = "provider-neutral-key",
            ["OPENAI_API_KEY"] = "provider-specific-key"
        };

        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => environment.GetValueOrDefault(name))
            .Build();

        Assert.Equal("provider-neutral-key", configuration["AI:ApiKey"]);
    }
}
