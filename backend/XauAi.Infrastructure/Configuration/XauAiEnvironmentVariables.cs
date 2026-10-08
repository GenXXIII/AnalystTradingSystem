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
            ["ALLTICK_ENABLED"] = "AllTick:Enabled",
            ["ALLTICK_TOKEN"] = "AllTick:Token",
            ["ALLTICK_APPLICATION_SYMBOL"] = "AllTick:ApplicationSymbol",
            ["ALLTICK_SYMBOL"] = "AllTick:Symbol",
            ["ALLTICK_HTTP_BASE_URL"] = "AllTick:HttpBaseUrl",
            ["ALLTICK_WEBSOCKET_URL"] = "AllTick:WebSocketUrl",
            ["ALLTICK_REQUEST_TIMEOUT_SECONDS"] = "AllTick:RequestTimeoutSeconds",
            ["ALLTICK_RECONNECT_DELAY_SECONDS"] = "AllTick:ReconnectDelaySeconds",
            ["ALLTICK_HEARTBEAT_INTERVAL_SECONDS"] = "AllTick:HeartbeatIntervalSeconds",
            ["ALLTICK_QUOTE_MAX_AGE_SECONDS"] = "AllTick:QuoteMaxAgeSeconds",
            ["ALLTICK_REALTIME_PERSIST_INTERVAL_SECONDS"] = "AllTick:RealtimePersistIntervalSeconds",
            ["ALLTICK_MAX_BARS_PER_REQUEST"] = "AllTick:MaxBarsPerRequest",
            ["ALLTICK_MINIMUM_HTTP_REQUEST_INTERVAL_SECONDS"] = "AllTick:MinimumHttpRequestIntervalSeconds",
            ["TWELVE_DATA_ENABLED"] = "TwelveData:Enabled",
            ["TWELVE_DATA_API_KEY"] = "TwelveData:ApiKey",
            ["TWELVE_DATA_APPLICATION_SYMBOL"] = "TwelveData:ApplicationSymbol",
            ["TWELVE_DATA_SYMBOL"] = "TwelveData:Symbol",
            ["TWELVE_DATA_BASE_URL"] = "TwelveData:BaseUrl",
            ["TWELVE_DATA_REQUEST_TIMEOUT_SECONDS"] = "TwelveData:RequestTimeoutSeconds",
            ["TWELVE_DATA_MINIMUM_REQUEST_INTERVAL_SECONDS"] = "TwelveData:MinimumRequestIntervalSeconds",
            ["TWELVE_DATA_REFERENCE_SYNC_INTERVAL_SECONDS"] = "TwelveData:ReferenceSyncIntervalSeconds",
            ["TWELVE_DATA_INITIAL_HISTORY_DAYS"] = "TwelveData:InitialHistoryDays",
            ["TWELVE_DATA_INCREMENTAL_LOOKBACK_MINUTES"] = "TwelveData:IncrementalLookbackMinutes",
            ["TWELVE_DATA_MAXIMUM_POINTS_PER_REQUEST"] = "TwelveData:MaximumPointsPerRequest",
            ["TWELVE_DATA_MAXIMUM_CLOSE_DEVIATION_BPS"] = "TwelveData:MaximumCloseDeviationBps",
            ["MARKET_DATA_PROVIDER"] = "MarketData:Provider",
            ["MARKET_DATA_PROVIDER_KEY"] = "MarketData:ProviderKey",
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
            ["LOCAL_ANALYST_ENABLED"] = "LocalAnalyst:Enabled",
            ["LOCAL_ANALYST_SYMBOL"] = "LocalAnalyst:Symbol",
            ["LOCAL_ANALYST_TIMEFRAMES"] = "LocalAnalyst:Timeframes",
            ["LOCAL_ANALYST_EVALUATION_INTERVAL_SECONDS"] = "LocalAnalyst:EvaluationIntervalSeconds",
            ["LOCAL_ANALYST_HISTORY_LIMIT"] = "LocalAnalyst:HistoryLimit",
            ["LOCAL_ANALYST_MINIMUM_CANDLES"] = "LocalAnalyst:MinimumCandles",
            ["LOCAL_ANALYST_STALE_AFTER_INTERVALS"] = "LocalAnalyst:StaleAfterIntervals",
            ["LOCAL_ANALYST_MAXIMUM_ALLOWED_GAPS"] = "LocalAnalyst:MaximumAllowedGaps",
            ["LOCAL_ANALYST_ENTRY_SCORE_THRESHOLD"] = "LocalAnalyst:EntryScoreThreshold",
            ["LOCAL_ANALYST_MINIMUM_DIRECTIONAL_LEAD"] = "LocalAnalyst:MinimumDirectionalLead",
            ["LOCAL_ANALYST_STOP_OPPOSING_SCORE_THRESHOLD"] = "LocalAnalyst:StopOpposingScoreThreshold",
            ["LOCAL_ANALYST_RSI_BULLISH_MINIMUM"] = "LocalAnalyst:RsiBullishMinimum",
            ["LOCAL_ANALYST_RSI_BULLISH_MAXIMUM"] = "LocalAnalyst:RsiBullishMaximum",
            ["LOCAL_ANALYST_RSI_BEARISH_MINIMUM"] = "LocalAnalyst:RsiBearishMinimum",
            ["LOCAL_ANALYST_RSI_BEARISH_MAXIMUM"] = "LocalAnalyst:RsiBearishMaximum",
            ["LOCAL_ANALYST_ALLOW_VERY_HIGH_VOLATILITY"] = "LocalAnalyst:AllowVeryHighVolatility",
            ["LOCAL_ANALYST_STRUCTURE_WEIGHT"] = "LocalAnalyst:StructureWeight",
            ["LOCAL_ANALYST_TREND_WEIGHT"] = "LocalAnalyst:TrendWeight",
            ["LOCAL_ANALYST_LIQUIDITY_WEIGHT"] = "LocalAnalyst:LiquidityWeight",
            ["LOCAL_ANALYST_CANDLE_WEIGHT"] = "LocalAnalyst:CandleWeight",
            ["LOCAL_ANALYST_MOMENTUM_WEIGHT"] = "LocalAnalyst:MomentumWeight",
            ["LOCAL_ANALYST_KTR_WEIGHT"] = "LocalAnalyst:KtrWeight",
            ["LOCAL_ANALYST_INVALIDATION_ATR_MULTIPLIER"] = "LocalAnalyst:InvalidationAtrMultiplier",
            ["LOCAL_ANALYST_TARGET_ATR_MULTIPLIER"] = "LocalAnalyst:TargetAtrMultiplier",
            ["LOCAL_ANALYST_EQUAL_LEVEL_TOLERANCE_ATR"] = "LocalAnalyst:EqualLevelToleranceAtr",
            ["LOCAL_ANALYST_IMPORTANT_LEVEL_DISTANCE_ATR"] = "LocalAnalyst:ImportantLevelDistanceAtr",
            ["LOCAL_ANALYST_VALIDITY_CANDLES"] = "LocalAnalyst:ValidityCandles",
            ["LOCAL_ANALYST_CONFIGURATION_VERSION"] = "LocalAnalyst:ConfigurationVersion",
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
            ["ANALYST_PROVIDER_KEY"] = "Analysts:ProviderKey",
            ["ANALYST_SOURCE_TYPE"] = "Analysts:SourceType",
            ["ANALYST_REQUIRES_API_KEY"] = "Analysts:RequiresApiKey",
            ["ANALYST_PROVIDER_API_KEY"] = "Analysts:ApiKey",
            ["ANALYST_BASE_URL"] = "Analysts:BaseUrl",
            ["ANALYST_TIMEOUT_SECONDS"] = "Analysts:TimeoutSeconds",
            ["ANALYST_MAX_RETRIES"] = "Analysts:MaxRetries",
            ["ANALYST_RATE_LIMIT_PER_MINUTE"] = "Analysts:RateLimitPerMinute",
            ["ANALYST_SYMBOL"] = "Analysts:Symbol",
            ["ANALYST_INITIAL_LOOKBACK_DAYS"] = "Analysts:InitialLookbackDays",
            ["ANALYST_COLLECTION_OVERLAP_MINUTES"] = "Analysts:CollectionOverlapMinutes",
            ["ANALYST_SYNC_INTERVAL_MINUTES"] = "Analysts:SyncIntervalMinutes",
            ["ANALYST_PROVIDER_PAGE_SIZE"] = "Analysts:ProviderPageSize",
            ["ANALYST_MAXIMUM_PAGES_PER_SYNC"] = "Analysts:MaximumPagesPerSync",
            ["ANALYST_MAXIMUM_PAGE_SIZE"] = "Analysts:MaximumPageSize",
            ["ANALYST_MAXIMUM_COLLECTION_RANGE_DAYS"] = "Analysts:MaximumCollectionRangeDays",
            ["ANALYST_RETRY_BASE_DELAY_SECONDS"] = "Analysts:RetryBaseDelaySeconds",
            ["ANALYST_RELEVANCE_KEYWORDS"] = "Analysts:RelevanceKeywords",
            ["EVIDENCE_MAXIMUM_PAGE_SIZE"] = "Evidence:MaximumPageSize",
            ["EVIDENCE_MAXIMUM_QUERY_RANGE_DAYS"] = "Evidence:MaximumQueryRangeDays",
            ["EVIDENCE_DEFAULT_PACK_LOOKBACK_DAYS"] = "Evidence:DefaultPackLookbackDays",
            ["EVIDENCE_MAXIMUM_PACK_LOOKBACK_DAYS"] = "Evidence:MaximumPackLookbackDays",
            ["EVIDENCE_MAXIMUM_PACK_ITEMS_PER_TYPE"] = "Evidence:MaximumPackItemsPerType",
            ["EVIDENCE_INGESTION_BATCH_SIZE"] = "Evidence:IngestionBatchSize",
            ["EVIDENCE_CONFLICT_WINDOW_HOURS"] = "Evidence:ConflictWindowHours",
            ["EVIDENCE_MAXIMUM_CONFLICTS"] = "Evidence:MaximumConflicts",
            ["AI_INTERPRETATION_PROMPT_VERSION"] = "AiInterpretation:PromptVersion",
            ["AI_INTERPRETATION_DEFAULT_LOOKBACK_HOURS"] = "AiInterpretation:DefaultLookbackHours",
            ["AI_INTERPRETATION_MAXIMUM_LOOKBACK_HOURS"] = "AiInterpretation:MaximumLookbackHours",
            ["AI_INTERPRETATION_MAXIMUM_EVIDENCE_ITEMS"] = "AiInterpretation:MaximumEvidenceItems",
            ["AI_INTERPRETATION_MAXIMUM_COMPRESSED_CHARACTERS"] = "AiInterpretation:MaximumCompressedCharacters",
            ["AI_INTERPRETATION_CURRENT_CONTEXT_CACHE_MINUTES"] = "AiInterpretation:CurrentContextCacheMinutes",
            ["AI_INTERPRETATION_MAXIMUM_PAGE_SIZE"] = "AiInterpretation:MaximumPageSize",
            ["TARGET_ANALYST_ENABLED"] = "TargetAnalyst:Enabled",
            ["TARGET_ANALYST_SYMBOL"] = "TargetAnalyst:Symbol",
            ["TARGET_ANALYST_TIMEFRAMES"] = "TargetAnalyst:Timeframes",
            ["TARGET_ANALYST_EVIDENCE_LOOKBACK_HOURS"] = "TargetAnalyst:EvidenceLookbackHours",
            ["TARGET_ANALYST_MAXIMUM_EVIDENCE_ITEMS_PER_WORKSPACE"] = "TargetAnalyst:MaximumEvidenceItemsPerWorkspace",
            ["TARGET_ANALYST_MAXIMUM_COMPRESSED_CHARACTERS"] = "TargetAnalyst:MaximumCompressedCharacters",
            ["TARGET_ANALYST_MINIMUM_MARKET_TIMEFRAMES"] = "TargetAnalyst:MinimumMarketTimeframes",
            ["TARGET_ANALYST_STALE_AFTER_INTERVALS"] = "TargetAnalyst:StaleAfterIntervals",
            ["TARGET_ANALYST_MINIMUM_CONFIDENCE"] = "TargetAnalyst:MinimumConfidence",
            ["TARGET_ANALYST_MINIMUM_TARGET_DISTANCE_ATR"] = "TargetAnalyst:MinimumTargetDistanceAtr",
            ["TARGET_ANALYST_DEFAULT_VALIDITY_MINUTES"] = "TargetAnalyst:DefaultValidityMinutes",
            ["TARGET_ANALYST_MAXIMUM_VALIDITY_MINUTES"] = "TargetAnalyst:MaximumValidityMinutes",
            ["TARGET_ANALYST_MONITOR_INTERVAL_SECONDS"] = "TargetAnalyst:MonitorIntervalSeconds",
            ["TARGET_ANALYST_MAXIMUM_PAGE_SIZE"] = "TargetAnalyst:MaximumPageSize",
            ["TARGET_ANALYST_CONFIGURATION_VERSION"] = "TargetAnalyst:ConfigurationVersion",
            ["FULL_ANALYST_ENABLED"] = "FullAnalyst:Enabled",
            ["FULL_ANALYST_SYMBOL"] = "FullAnalyst:Symbol",
            ["FULL_ANALYST_TIMEFRAMES"] = "FullAnalyst:Timeframes",
            ["FULL_ANALYST_EVIDENCE_LOOKBACK_HOURS"] = "FullAnalyst:EvidenceLookbackHours",
            ["FULL_ANALYST_MAXIMUM_EVIDENCE_ITEMS_PER_WORKSPACE"] = "FullAnalyst:MaximumEvidenceItemsPerWorkspace",
            ["FULL_ANALYST_MAXIMUM_COMPRESSED_CHARACTERS"] = "FullAnalyst:MaximumCompressedCharacters",
            ["FULL_ANALYST_MINIMUM_MARKET_TIMEFRAMES"] = "FullAnalyst:MinimumMarketTimeframes",
            ["FULL_ANALYST_STALE_AFTER_INTERVALS"] = "FullAnalyst:StaleAfterIntervals",
            ["FULL_ANALYST_MINIMUM_CONFIDENCE"] = "FullAnalyst:MinimumConfidence",
            ["FULL_ANALYST_DEFAULT_VALIDITY_MINUTES"] = "FullAnalyst:DefaultValidityMinutes",
            ["FULL_ANALYST_MAXIMUM_VALIDITY_MINUTES"] = "FullAnalyst:MaximumValidityMinutes",
            ["FULL_ANALYST_CACHE_MINUTES"] = "FullAnalyst:CacheMinutes",
            ["FULL_ANALYST_MONITOR_INTERVAL_SECONDS"] = "FullAnalyst:MonitorIntervalSeconds",
            ["FULL_ANALYST_MAXIMUM_PAGE_SIZE"] = "FullAnalyst:MaximumPageSize",
            ["FULL_ANALYST_CONFIGURATION_VERSION"] = "FullAnalyst:ConfigurationVersion"
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

        AddAiSpecialist(values, readVariable, "NEWS", "News");
        AddAiSpecialist(values, readVariable, "CANDLE", "Candle");
        AddAiSpecialist(values, readVariable, "STRUCTURE", "Structure");
        AddAiSpecialist(values, readVariable, "LIQUIDITY", "Liquidity");
        AddAiSpecialist(values, readVariable, "FLOW", "Flow");
        AddAiSpecialist(values, readVariable, "KTR", "Ktr");
        AddAiSpecialist(values, readVariable, "RISK", "Risk");
        AddAiSpecialist(values, readVariable, "MASTER", "Master");
        AddTargetWorkspace(values, readVariable, "TARGET_STRUCTURE", "Structure");
        AddTargetWorkspace(values, readVariable, "TARGET_LIQUIDITY", "Liquidity");
        AddTargetWorkspace(values, readVariable, "TARGET_CANDLE", "Candle");
        AddTargetWorkspace(values, readVariable, "TARGET_FLOW", "Flow");
        AddTargetWorkspace(values, readVariable, "TARGET_KTR", "Ktr");
        AddTargetWorkspace(values, readVariable, "TARGET_NEWS", "News");
        AddTargetWorkspace(values, readVariable, "TARGET_RISK", "Risk");
        AddTargetWorkspace(values, readVariable, "TARGET_MASTER", "Master");
        AddTargetApiKey(values, readVariable, "TARGET_STRUCTURE", "Structure");
        AddTargetApiKey(values, readVariable, "TARGET_LIQUIDITY", "Liquidity");
        AddTargetApiKey(values, readVariable, "TARGET_CANDLE", "Candle");
        AddTargetApiKey(values, readVariable, "TARGET_FLOW", "Flow");
        AddTargetApiKey(values, readVariable, "TARGET_KTR", "Ktr");
        AddTargetApiKey(values, readVariable, "TARGET_NEWS", "News");
        AddTargetApiKey(values, readVariable, "TARGET_RISK", "Risk");
        AddTargetApiKey(values, readVariable, "TARGET_MASTER", "Master");
        AddFullWorkspace(values, readVariable, "FULL_STRUCTURE", "Structure");
        AddFullWorkspace(values, readVariable, "FULL_LIQUIDITY", "Liquidity");
        AddFullWorkspace(values, readVariable, "FULL_CANDLE", "Candle");
        AddFullWorkspace(values, readVariable, "FULL_FLOW", "Flow");
        AddFullWorkspace(values, readVariable, "FULL_KTR", "Ktr");
        AddFullWorkspace(values, readVariable, "FULL_NEWS", "News");
        AddFullWorkspace(values, readVariable, "FULL_RISK", "Risk");
        AddFullWorkspace(values, readVariable, "FULL_MASTER", "Master");

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

    private static void AddAiSpecialist(
        Dictionary<string, string?> values,
        Func<string, string?> readVariable,
        string environmentPrefix,
        string configurationName)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ENABLED"] = "Enabled",
            ["PROVIDER"] = "Provider",
            ["ADAPTER"] = "Adapter",
            ["REQUIRES_API_KEY"] = "RequiresApiKey",
            ["API_KEY"] = "ApiKey",
            ["MODEL"] = "Model",
            ["BASE_URL"] = "BaseUrl",
            ["TEMPERATURE"] = "Temperature",
            ["TIMEOUT_SECONDS"] = "TimeoutSeconds",
            ["MAX_OUTPUT_TOKENS"] = "MaxOutputTokens",
            ["MAX_RETRIES"] = "MaxRetries",
            ["REQUESTS_PER_MINUTE"] = "RequestsPerMinute"
        };
        foreach (var field in fields)
        {
            AddIfPresent(
                values,
                $"AiSpecialists:{configurationName}:{field.Value}",
                readVariable($"{environmentPrefix}_AI_{field.Key}"));
        }
    }

    private static void AddTargetWorkspace(
        Dictionary<string, string?> values,
        Func<string, string?> readVariable,
        string environmentPrefix,
        string configurationName)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ENABLED"] = "Enabled",
            ["PROVIDER"] = "Provider",
            ["ADAPTER"] = "Adapter",
            ["REQUIRES_API_KEY"] = "RequiresApiKey",
            ["API_KEY"] = "ApiKey",
            ["MODEL"] = "Model",
            ["BASE_URL"] = "BaseUrl",
            ["TEMPERATURE"] = "Temperature",
            ["TIMEOUT_SECONDS"] = "TimeoutSeconds",
            ["MAX_OUTPUT_TOKENS"] = "MaxOutputTokens",
            ["MAX_RETRIES"] = "MaxRetries",
            ["REQUESTS_PER_MINUTE"] = "RequestsPerMinute",
            ["PROMPT_VERSION"] = "PromptVersion",
            ["CONFIGURATION_VERSION"] = "ConfigurationVersion"
        };
        foreach (var field in fields)
        {
            AddIfPresent(
                values,
                $"TargetAiWorkspaces:{configurationName}:{field.Value}",
                readVariable($"{environmentPrefix}_AI_{field.Key}"));
        }
    }

    private static void AddTargetApiKey(
        Dictionary<string, string?> values,
        Func<string, string?> readVariable,
        string environmentPrefix,
        string configurationName) =>
        AddFirstPresent(
            values,
            $"TargetAiWorkspaces:{configurationName}:ApiKey",
            readVariable,
            $"{environmentPrefix}_AI_API_KEY",
            "TARGET_AI_API_KEY",
            "OPENROUTER_API_KEY",
            "AI_API_KEY",
            "OPENAI_API_KEY");

    private static void AddFullWorkspace(
        Dictionary<string, string?> values,
        Func<string, string?> readVariable,
        string environmentPrefix,
        string configurationName)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ENABLED"] = "Enabled",
            ["PROVIDER"] = "Provider",
            ["ADAPTER"] = "Adapter",
            ["REQUIRES_API_KEY"] = "RequiresApiKey",
            ["API_KEY"] = "ApiKey",
            ["MODEL"] = "Model",
            ["BASE_URL"] = "BaseUrl",
            ["TEMPERATURE"] = "Temperature",
            ["TIMEOUT_SECONDS"] = "TimeoutSeconds",
            ["MAX_OUTPUT_TOKENS"] = "MaxOutputTokens",
            ["MAX_RETRIES"] = "MaxRetries",
            ["REQUESTS_PER_MINUTE"] = "RequestsPerMinute",
            ["PROMPT_VERSION"] = "PromptVersion",
            ["CONFIGURATION_VERSION"] = "ConfigurationVersion"
        };
        foreach (var field in fields)
        {
            AddIfPresent(
                values,
                $"FullAiWorkspaces:{configurationName}:{field.Value}",
                readVariable($"{environmentPrefix}_AI_{field.Key}"));
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
