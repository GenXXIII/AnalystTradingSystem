using Microsoft.Extensions.Configuration;

namespace XauAi.Infrastructure.Configuration;

public static class XauAiEnvironmentVariables
{
    private static readonly IReadOnlyDictionary<string, string> VariableMappings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["APP_NAME"] = "Application:Name",
            ["APP_VERSION"] = "Application:Version",
            ["API_ENABLE_SWAGGER"] = "Api:EnableSwagger",
            ["LOG_LEVEL_DEFAULT"] = "Logging:LogLevel:Default",
            ["DATABASE_ENABLED"] = "Database:Enabled",
            ["DATABASE_CONNECTION_STRING"] = "Database:ConnectionString",
            ["DATABASE_COMMAND_TIMEOUT_SECONDS"] = "Database:CommandTimeoutSeconds",
            ["DATABASE_APPLY_MIGRATIONS_ON_STARTUP"] = "Database:ApplyMigrationsOnStartup",
            ["REDIS_ENABLED"] = "Redis:Enabled",
            ["REDIS_CONNECTION_STRING"] = "Redis:ConnectionString",
            ["REDIS_INSTANCE_NAME"] = "Redis:InstanceName",
            ["REDIS_DEFAULT_EXPIRATION_SECONDS"] = "Redis:DefaultExpirationSeconds",
            ["AI_ENABLED"] = "AI:Enabled",
            ["AI_PROVIDER"] = "AI:Provider",
            ["AI_REQUIRES_API_KEY"] = "AI:RequiresApiKey",
            ["AI_BASE_URL"] = "AI:BaseUrl",
            ["AI_TEMPERATURE"] = "AI:Temperature",
            ["AI_TIMEOUT_SECONDS"] = "AI:TimeoutSeconds",
            ["AI_MAX_RETRIES"] = "AI:MaxRetries",
            ["AI_MAX_OUTPUT_TOKENS"] = "AI:MaxOutputTokens",
            ["AI_DAILY_BUDGET_USD"] = "AI:DailyBudgetUsd",
            ["AI_DAILY_REQUEST_LIMIT"] = "AI:DailyRequestLimit",
            ["MT5_ENABLED"] = "MT5:Enabled",
            ["MT5_LOGIN"] = "MT5:Login",
            ["MT5_PASSWORD"] = "MT5:Password",
            ["MT5_SERVER"] = "MT5:Server",
            ["MT5_TERMINAL_PATH"] = "MT5:TerminalPath",
            ["MT5_APPLICATION_SYMBOL"] = "MT5:ApplicationSymbol",
            ["MT5_SYMBOL"] = "MT5:Symbol",
            ["MT5_TIMEZONE"] = "MT5:TimeZone",
            ["MT5_CONNECTION_TIMEOUT_SECONDS"] = "MT5:ConnectionTimeoutSeconds",
            ["MT5_REQUEST_TIMEOUT_SECONDS"] = "MT5:RequestTimeoutSeconds",
            ["MT5_RECONNECT_DELAY_SECONDS"] = "MT5:ReconnectDelaySeconds",
            ["MT5_MAX_BARS_PER_REQUEST"] = "MT5:MaxBarsPerRequest",
            ["MT5_PYTHON_EXECUTABLE"] = "MT5:PythonExecutable",
            ["MT5_BRIDGE_SCRIPT_PATH"] = "MT5:BridgeScriptPath",
            ["MARKET_DATA_SYNC_ENABLED"] = "MarketData:SyncEnabled",
            ["MARKET_DATA_SYMBOL"] = "MarketData:Symbol",
            ["MARKET_DATA_TIMEFRAMES"] = "MarketData:Timeframes",
            ["MARKET_DATA_INITIAL_HISTORY_DAYS"] = "MarketData:InitialHistoryDays",
            ["MARKET_DATA_SYNC_INTERVAL_SECONDS"] = "MarketData:SyncIntervalSeconds",
            ["MARKET_DATA_BATCH_SIZE"] = "MarketData:BatchSize",
            ["MARKET_DATA_MAX_API_LIMIT"] = "MarketData:MaxApiLimit",
            ["MARKET_DATA_MAX_QUERY_RANGE_DAYS"] = "MarketData:MaxQueryRangeDays",
            ["MARKET_DATA_MAX_RETRIES"] = "MarketData:MaxRetries",
            ["MARKET_DATA_RETRY_BASE_DELAY_SECONDS"] = "MarketData:RetryBaseDelaySeconds",
            ["MARKET_DATA_MAX_GAP_RESULTS"] = "MarketData:MaxGapResults",
            ["MARKET_DATA_INCLUDE_FORMING_CANDLE"] = "MarketData:IncludeFormingCandle",
            ["TECHNICAL_ANALYSIS_ENABLED"] = "TechnicalAnalysis:Enabled",
            ["TECHNICAL_ANALYSIS_SYMBOL"] = "TechnicalAnalysis:Symbol",
            ["TECHNICAL_ANALYSIS_TIMEFRAMES"] = "TechnicalAnalysis:Timeframes",
            ["TECHNICAL_ANALYSIS_HISTORY_LIMIT"] = "TechnicalAnalysis:HistoryLimit",
            ["SMA_PERIODS"] = "TechnicalAnalysis:SmaPeriods",
            ["EMA_PERIODS"] = "TechnicalAnalysis:EmaPeriods",
            ["RSI_PERIOD"] = "TechnicalAnalysis:RsiPeriod",
            ["MACD_FAST_PERIOD"] = "TechnicalAnalysis:MacdFastPeriod",
            ["MACD_SLOW_PERIOD"] = "TechnicalAnalysis:MacdSlowPeriod",
            ["MACD_SIGNAL_PERIOD"] = "TechnicalAnalysis:MacdSignalPeriod",
            ["ATR_PERIOD"] = "TechnicalAnalysis:AtrPeriod",
            ["ADX_PERIOD"] = "TechnicalAnalysis:AdxPeriod",
            ["BOLLINGER_PERIOD"] = "TechnicalAnalysis:BollingerPeriod",
            ["BOLLINGER_STANDARD_DEVIATION"] = "TechnicalAnalysis:BollingerStandardDeviations",
            ["STOCHASTIC_K_PERIOD"] = "TechnicalAnalysis:StochasticKPeriod",
            ["STOCHASTIC_D_PERIOD"] = "TechnicalAnalysis:StochasticDPeriod",
            ["SWING_WINDOW"] = "TechnicalAnalysis:SwingWindow",
            ["LEVEL_TOLERANCE_PERCENT"] = "TechnicalAnalysis:LevelTolerancePercent",
            ["MINIMUM_LEVEL_TOUCHES"] = "TechnicalAnalysis:MinimumLevelTouches",
            ["MAXIMUM_LEVEL_ZONES"] = "TechnicalAnalysis:MaximumLevelZones",
            ["PRICE_ACTION_LOOKBACK"] = "TechnicalAnalysis:PriceActionLookback",
            ["VOLATILITY_LOOKBACK"] = "TechnicalAnalysis:VolatilityLookback",
            ["VERY_LOW_VOLATILITY_RATIO"] = "TechnicalAnalysis:VeryLowVolatilityRatio",
            ["LOW_VOLATILITY_RATIO"] = "TechnicalAnalysis:LowVolatilityRatio",
            ["HIGH_VOLATILITY_RATIO"] = "TechnicalAnalysis:HighVolatilityRatio",
            ["VERY_HIGH_VOLATILITY_RATIO"] = "TechnicalAnalysis:VeryHighVolatilityRatio",
            ["NEWS_ENABLED"] = "News:Enabled",
            ["NEWS_PROVIDER"] = "News:Provider",
            ["NEWS_REQUIRES_API_KEY"] = "News:RequiresApiKey",
            ["NEWS_API_KEY"] = "News:ApiKey",
            ["NEWS_BASE_URL"] = "News:BaseUrl",
            ["NEWS_TIMEOUT_SECONDS"] = "News:TimeoutSeconds",
            ["NEWS_MAX_RETRIES"] = "News:MaxRetries",
            ["NEWS_RATE_LIMIT_PER_MINUTE"] = "News:RateLimitPerMinute",
            ["NEWS_USE_TIMEFRAME_PARAMETER"] = "News:UseTimeframeParameter",
            ["NEWS_PROVIDER_KEY"] = "News:ProviderKey",
            ["NEWS_SYMBOL"] = "News:Symbol",
            ["NEWS_LANGUAGE"] = "News:Language",
            ["NEWS_PROVIDER_QUERY"] = "News:ProviderQuery",
            ["NEWS_INITIAL_LOOKBACK_HOURS"] = "News:InitialLookbackHours",
            ["NEWS_COLLECTION_OVERLAP_MINUTES"] = "News:CollectionOverlapMinutes",
            ["NEWS_COLLECTION_INTERVAL_SECONDS"] = "News:CollectionIntervalSeconds",
            ["NEWS_PAGE_SIZE"] = "News:PageSize",
            ["NEWS_MAXIMUM_PAGES_PER_COLLECTION"] = "News:MaximumPagesPerCollection",
            ["NEWS_MAXIMUM_PAGE_SIZE"] = "News:MaximumPageSize",
            ["NEWS_MAXIMUM_COLLECTION_RANGE_DAYS"] = "News:MaximumCollectionRangeDays",
            ["NEWS_RETRY_BASE_DELAY_SECONDS"] = "News:RetryBaseDelaySeconds",
            ["NEWS_ARCHIVE_ENABLED"] = "News:ArchiveEnabled",
            ["NEWS_MINIMUM_RELEVANCE"] = "News:MinimumRelevance",
            ["NEWS_GOLD_KEYWORDS"] = "News:GoldKeywords",
            ["NEWS_USD_KEYWORDS"] = "News:UsdKeywords",
            ["NEWS_FED_KEYWORDS"] = "News:FedKeywords",
            ["NEWS_INFLATION_KEYWORDS"] = "News:InflationKeywords",
            ["NEWS_EMPLOYMENT_KEYWORDS"] = "News:EmploymentKeywords",
            ["NEWS_RATES_KEYWORDS"] = "News:RatesKeywords",
            ["NEWS_ECONOMY_KEYWORDS"] = "News:EconomyKeywords",
            ["NEWS_CENTRAL_BANK_KEYWORDS"] = "News:CentralBankKeywords",
            ["NEWS_GEOPOLITICS_KEYWORDS"] = "News:GeopoliticsKeywords",
            ["NEWS_COMMODITY_KEYWORDS"] = "News:CommodityKeywords",
            ["ECONOMIC_DATA_ENABLED"] = "EconomicData:Enabled",
            ["ECONOMIC_DATA_PROVIDER"] = "EconomicData:Provider",
            ["ECONOMIC_DATA_PROVIDER_KEY"] = "EconomicData:ProviderKey",
            ["ECONOMIC_DATA_REQUIRES_API_KEY"] = "EconomicData:RequiresApiKey",
            ["ECONOMIC_DATA_API_KEY"] = "EconomicData:ApiKey",
            ["ECONOMIC_DATA_BASE_URL"] = "EconomicData:BaseUrl",
            ["ECONOMIC_DATA_TIMEOUT_SECONDS"] = "EconomicData:TimeoutSeconds",
            ["ECONOMIC_DATA_MAX_RETRIES"] = "EconomicData:MaxRetries",
            ["ECONOMIC_DATA_RATE_LIMIT_PER_MINUTE"] = "EconomicData:RateLimitPerMinute",
            ["ECONOMIC_DATA_INITIAL_HISTORY_YEARS"] = "EconomicData:InitialHistoryYears",
            ["ECONOMIC_DATA_REVISION_LOOKBACK_DAYS"] = "EconomicData:RevisionLookbackDays",
            ["ECONOMIC_DATA_SYNC_INTERVAL_MINUTES"] = "EconomicData:SyncIntervalMinutes",
            ["ECONOMIC_DATA_PROVIDER_PAGE_SIZE"] = "EconomicData:ProviderPageSize",
            ["ECONOMIC_DATA_MAXIMUM_PAGES_PER_SERIES"] = "EconomicData:MaximumPagesPerSeries",
            ["ECONOMIC_DATA_MAXIMUM_PAGE_SIZE"] = "EconomicData:MaximumPageSize",
            ["ECONOMIC_DATA_MAXIMUM_QUERY_RANGE_YEARS"] = "EconomicData:MaximumQueryRangeYears",
            ["ECONOMIC_DATA_RETRY_BASE_DELAY_SECONDS"] = "EconomicData:RetryBaseDelaySeconds",
            ["ECONOMIC_DATA_TRACKED_SERIES"] = "EconomicData:TrackedSeries",
            ["ANALYSTS_ENABLED"] = "Analysts:Enabled",
            ["ANALYST_PROVIDER"] = "Analysts:Provider",
            ["ANALYST_SOURCE_TYPE"] = "Analysts:SourceType",
            ["ANALYST_REQUIRES_API_KEY"] = "Analysts:RequiresApiKey",
            ["ANALYST_PROVIDER_API_KEY"] = "Analysts:ApiKey",
            ["ANALYST_BASE_URL"] = "Analysts:BaseUrl",
            ["ANALYST_TIMEOUT_SECONDS"] = "Analysts:TimeoutSeconds",
            ["ANALYST_MAX_RETRIES"] = "Analysts:MaxRetries",
            ["ANALYST_RATE_LIMIT_PER_MINUTE"] = "Analysts:RateLimitPerMinute"
        };

    public static IConfigurationBuilder AddXauAiEnvironmentVariables(
        this IConfigurationBuilder configuration,
        Func<string, string?>? readVariable = null)
    {
        readVariable ??= Environment.GetEnvironmentVariable;
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in VariableMappings)
        {
            AddIfPresent(values, mapping.Value, readVariable(mapping.Key));
        }

        AddFirstPresent(values, "AI:ApiKey", readVariable, "AI_API_KEY", "OPENAI_API_KEY");
        AddFirstPresent(values, "AI:Model", readVariable, "AI_MODEL", "OPENAI_MODEL");
        AddFirstPresent(values, "EconomicData:ApiKey", readVariable, "ECONOMIC_DATA_API_KEY", "FRED_API_KEY");

        var corsOrigins = readVariable("CORS_ALLOWED_ORIGINS");
        if (!string.IsNullOrWhiteSpace(corsOrigins))
        {
            var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var index = 0; index < origins.Length; index++)
            {
                values[$"Api:Cors:AllowedOrigins:{index}"] = origins[index];
            }
        }

        configuration.AddInMemoryCollection(values);
        return configuration;
    }

    private static void AddFirstPresent(
        Dictionary<string, string?> values,
        string configurationKey,
        Func<string, string?> readVariable,
        params string[] variableNames)
    {
        foreach (var variableName in variableNames)
        {
            var value = readVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values[configurationKey] = value;
                return;
            }
        }
    }

    private static void AddIfPresent(
        Dictionary<string, string?> values,
        string configurationKey,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[configurationKey] = value;
        }
    }
}
