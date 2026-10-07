using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Configuration.Validation;
using XauAi.Application.MarketData;
using XauAi.Application.Analysts;
using XauAi.Application.News;
using XauAi.Application.EconomicData;
using XauAi.Application.Evidence;
using XauAi.Application.TechnicalAnalysis;
using XauAi.Application.LocalAnalysis;
using XauAi.Infrastructure.MarketData;
using XauAi.Infrastructure.MarketData.AllTick;
using XauAi.Infrastructure.MarketData.Persistence;
using XauAi.Infrastructure.MarketData.TwelveData;
using XauAi.Infrastructure.News;
using XauAi.Infrastructure.News.NewsData;
using XauAi.Infrastructure.News.Persistence;
using XauAi.Infrastructure.EconomicData;
using XauAi.Infrastructure.EconomicData.Fred;
using XauAi.Infrastructure.EconomicData.Persistence;
using XauAi.Infrastructure.Persistence;
using XauAi.Infrastructure.Analysts;
using XauAi.Infrastructure.Analysts.Persistence;
using XauAi.Infrastructure.Analysts.Rss;
using XauAi.Infrastructure.Evidence.Persistence;
using XauAi.Application.AI;
using XauAi.Infrastructure.AI;
using XauAi.Infrastructure.AI.Persistence;
using XauAi.Infrastructure.LocalAnalysis;
using XauAi.Infrastructure.LocalAnalysis.Persistence;

namespace XauAi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers validated configuration, SQL Server persistence, and provider-backed market data.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddValidatedOptions<ApplicationOptions, ApplicationOptionsValidator>(
            services, configuration, ApplicationOptions.SectionName);
        AddValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(
            services, configuration, DatabaseOptions.SectionName);
        AddValidatedOptions<RedisOptions, RedisOptionsValidator>(
            services, configuration, RedisOptions.SectionName);
        AddValidatedOptions<AiOptions, AiOptionsValidator>(
            services, configuration, AiOptions.SectionName);
        AddValidatedOptions<AiInterpretationOptions, AiInterpretationOptionsValidator>(
            services, configuration, AiInterpretationOptions.SectionName);
        AddValidatedOptions<AiSpecialistsOptions, AiSpecialistsOptionsValidator>(
            services, configuration, AiSpecialistsOptions.SectionName);
        AddValidatedOptions<AllTickOptions, AllTickOptionsValidator>(
            services, configuration, AllTickOptions.SectionName);
        AddValidatedOptions<TwelveDataOptions, TwelveDataOptionsValidator>(
            services, configuration, TwelveDataOptions.SectionName);
        AddValidatedOptions<MarketDataOptions, MarketDataOptionsValidator>(
            services, configuration, MarketDataOptions.SectionName);
        AddValidatedOptions<TechnicalAnalysisOptions, TechnicalAnalysisOptionsValidator>(
            services, configuration, TechnicalAnalysisOptions.SectionName);
        AddValidatedOptions<LocalAnalystOptions, LocalAnalystOptionsValidator>(
            services, configuration, LocalAnalystOptions.SectionName);
        AddValidatedOptions<NewsOptions, NewsOptionsValidator>(
            services, configuration, NewsOptions.SectionName);
        AddValidatedOptions<EconomicDataOptions, EconomicDataOptionsValidator>(
            services, configuration, EconomicDataOptions.SectionName);
        AddValidatedOptions<AnalystOptions, AnalystOptionsValidator>(
            services, configuration, AnalystOptions.SectionName);
        AddValidatedOptions<EvidenceOptions, EvidenceOptionsValidator>(
            services, configuration, EvidenceOptions.SectionName);
        AddValidatedOptions<ApiOptions, ApiOptionsValidator>(
            services, configuration, ApiOptions.SectionName);

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();
        var marketDataOptions = configuration
            .GetSection(MarketDataOptions.SectionName)
            .Get<MarketDataOptions>() ?? new MarketDataOptions();
        var twelveDataOptions = configuration
            .GetSection(TwelveDataOptions.SectionName)
            .Get<TwelveDataOptions>() ?? new TwelveDataOptions();
        var marketDataSettings = new MarketDataPipelineSettings
        {
            Provider = marketDataOptions.Provider,
            ProviderKey = marketDataOptions.ProviderKey,
            SyncEnabled = marketDataOptions.SyncEnabled,
            Symbol = marketDataOptions.Symbol,
            Timeframes = [.. marketDataOptions.Timeframes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => MarketTimeframes.TryParse(value, out var timeframe)
                    ? (MarketTimeframe?)timeframe
                    : null)
                .Where(timeframe => timeframe.HasValue)
                .Select(timeframe => timeframe!.Value)
                .Distinct()],
            InitialHistoryDays = marketDataOptions.InitialHistoryDays,
            SyncIntervalSeconds = marketDataOptions.SyncIntervalSeconds,
            BatchSize = marketDataOptions.BatchSize,
            MaxApiLimit = marketDataOptions.MaxApiLimit,
            MaxQueryRangeDays = marketDataOptions.MaxQueryRangeDays,
            MaxRetries = marketDataOptions.MaxRetries,
            RetryBaseDelaySeconds = marketDataOptions.RetryBaseDelaySeconds,
            MaxGapResults = marketDataOptions.MaxGapResults,
            IncludeFormingCandle = marketDataOptions.IncludeFormingCandle,
            ReferenceDataEnabled = twelveDataOptions.Enabled,
            ReferenceProviderKey = twelveDataOptions.ProviderKey,
            ReferenceMaximumCloseDeviationBps = twelveDataOptions.MaximumCloseDeviationBps
        };
        services.AddSingleton(marketDataSettings);
        var allTickOptions = configuration
            .GetSection(AllTickOptions.SectionName)
            .Get<AllTickOptions>() ?? new AllTickOptions();
        services.AddSingleton(allTickOptions);
        services.AddSingleton(twelveDataOptions);
        var technicalOptions = configuration
            .GetSection(TechnicalAnalysisOptions.SectionName)
            .Get<TechnicalAnalysisOptions>() ?? new TechnicalAnalysisOptions();
        services.AddSingleton(new TechnicalAnalysisSettings
        {
            Enabled = technicalOptions.Enabled,
            Symbol = technicalOptions.Symbol,
            Timeframes = ParseTimeframes(technicalOptions.Timeframes),
            HistoryLimit = technicalOptions.HistoryLimit,
            SmaPeriods = ParsePeriods(technicalOptions.SmaPeriods),
            EmaPeriods = ParsePeriods(technicalOptions.EmaPeriods),
            RsiPeriod = technicalOptions.RsiPeriod,
            MacdFastPeriod = technicalOptions.MacdFastPeriod,
            MacdSlowPeriod = technicalOptions.MacdSlowPeriod,
            MacdSignalPeriod = technicalOptions.MacdSignalPeriod,
            AtrPeriod = technicalOptions.AtrPeriod,
            AdxPeriod = technicalOptions.AdxPeriod,
            BollingerPeriod = technicalOptions.BollingerPeriod,
            BollingerStandardDeviations = technicalOptions.BollingerStandardDeviations,
            StochasticKPeriod = technicalOptions.StochasticKPeriod,
            StochasticDPeriod = technicalOptions.StochasticDPeriod,
            SwingWindow = technicalOptions.SwingWindow,
            LevelTolerancePercent = technicalOptions.LevelTolerancePercent,
            MinimumLevelTouches = technicalOptions.MinimumLevelTouches,
            MaximumLevelZones = technicalOptions.MaximumLevelZones,
            PriceActionLookback = technicalOptions.PriceActionLookback,
            VolatilityLookback = technicalOptions.VolatilityLookback,
            VeryLowVolatilityRatio = technicalOptions.VeryLowVolatilityRatio,
            LowVolatilityRatio = technicalOptions.LowVolatilityRatio,
            HighVolatilityRatio = technicalOptions.HighVolatilityRatio,
            VeryHighVolatilityRatio = technicalOptions.VeryHighVolatilityRatio
        });
        var localAnalystOptions = configuration
            .GetSection(LocalAnalystOptions.SectionName)
            .Get<LocalAnalystOptions>() ?? new LocalAnalystOptions();
        LocalAnalystOptionsValidator.TryParseValidity(localAnalystOptions.ValidityCandles, out var validityCandles);
        services.AddSingleton(new LocalAnalystSettings
        {
            Enabled = localAnalystOptions.Enabled,
            Symbol = localAnalystOptions.Symbol,
            Timeframes = ParseTimeframes(localAnalystOptions.Timeframes),
            EvaluationIntervalSeconds = localAnalystOptions.EvaluationIntervalSeconds,
            HistoryLimit = localAnalystOptions.HistoryLimit,
            MinimumCandles = localAnalystOptions.MinimumCandles,
            StaleAfterIntervals = localAnalystOptions.StaleAfterIntervals,
            MaximumAllowedGaps = localAnalystOptions.MaximumAllowedGaps,
            EntryScoreThreshold = localAnalystOptions.EntryScoreThreshold,
            MinimumDirectionalLead = localAnalystOptions.MinimumDirectionalLead,
            StopOpposingScoreThreshold = localAnalystOptions.StopOpposingScoreThreshold,
            RsiBullishMinimum = localAnalystOptions.RsiBullishMinimum,
            RsiBullishMaximum = localAnalystOptions.RsiBullishMaximum,
            RsiBearishMinimum = localAnalystOptions.RsiBearishMinimum,
            RsiBearishMaximum = localAnalystOptions.RsiBearishMaximum,
            AllowVeryHighVolatility = localAnalystOptions.AllowVeryHighVolatility,
            StructureWeight = localAnalystOptions.StructureWeight,
            TrendWeight = localAnalystOptions.TrendWeight,
            LiquidityWeight = localAnalystOptions.LiquidityWeight,
            CandleWeight = localAnalystOptions.CandleWeight,
            MomentumWeight = localAnalystOptions.MomentumWeight,
            KtrWeight = localAnalystOptions.KtrWeight,
            InvalidationAtrMultiplier = localAnalystOptions.InvalidationAtrMultiplier,
            TargetAtrMultiplier = localAnalystOptions.TargetAtrMultiplier,
            EqualLevelToleranceAtr = localAnalystOptions.EqualLevelToleranceAtr,
            ImportantLevelDistanceAtr = localAnalystOptions.ImportantLevelDistanceAtr,
            ConfigurationVersion = localAnalystOptions.ConfigurationVersion,
            ValidityCandles = validityCandles
        });
        var newsOptions = configuration
            .GetSection(NewsOptions.SectionName)
            .Get<NewsOptions>() ?? new NewsOptions();
        services.AddSingleton(newsOptions);
        services.AddSingleton(new NewsSettings
        {
            Enabled = newsOptions.Enabled,
            Provider = newsOptions.Provider,
            ProviderKey = newsOptions.ProviderKey,
            Symbol = newsOptions.Symbol,
            Language = newsOptions.Language,
            ProviderQuery = newsOptions.ProviderQuery,
            InitialLookbackHours = newsOptions.InitialLookbackHours,
            CollectionOverlapMinutes = newsOptions.CollectionOverlapMinutes,
            CollectionIntervalSeconds = newsOptions.CollectionIntervalSeconds,
            PageSize = newsOptions.PageSize,
            MaximumPagesPerCollection = newsOptions.MaximumPagesPerCollection,
            MaximumPageSize = newsOptions.MaximumPageSize,
            MaximumCollectionRangeDays = newsOptions.MaximumCollectionRangeDays,
            MaxRetries = newsOptions.MaxRetries,
            RetryBaseDelaySeconds = newsOptions.RetryBaseDelaySeconds,
            ArchiveEnabled = newsOptions.ArchiveEnabled,
            MinimumRelevance = ParseRelevance(newsOptions.MinimumRelevance),
            GoldKeywords = ParseValues(newsOptions.GoldKeywords),
            UsdKeywords = ParseValues(newsOptions.UsdKeywords),
            FedKeywords = ParseValues(newsOptions.FedKeywords),
            InflationKeywords = ParseValues(newsOptions.InflationKeywords),
            EmploymentKeywords = ParseValues(newsOptions.EmploymentKeywords),
            RatesKeywords = ParseValues(newsOptions.RatesKeywords),
            EconomyKeywords = ParseValues(newsOptions.EconomyKeywords),
            CentralBankKeywords = ParseValues(newsOptions.CentralBankKeywords),
            GeopoliticsKeywords = ParseValues(newsOptions.GeopoliticsKeywords),
            CommodityKeywords = ParseValues(newsOptions.CommodityKeywords)
        });
        var economicOptions = configuration
            .GetSection(EconomicDataOptions.SectionName)
            .Get<EconomicDataOptions>() ?? new EconomicDataOptions();
        services.AddSingleton(economicOptions);
        services.AddSingleton(new EconomicDataSettings
        {
            Enabled = economicOptions.Enabled,
            Provider = economicOptions.Provider,
            ProviderKey = economicOptions.ProviderKey,
            InitialHistoryYears = economicOptions.InitialHistoryYears,
            RevisionLookbackDays = economicOptions.RevisionLookbackDays,
            SyncIntervalMinutes = economicOptions.SyncIntervalMinutes,
            ProviderPageSize = economicOptions.ProviderPageSize,
            MaximumPagesPerSeries = economicOptions.MaximumPagesPerSeries,
            MaximumPageSize = economicOptions.MaximumPageSize,
            MaximumQueryRangeYears = economicOptions.MaximumQueryRangeYears,
            MaxRetries = economicOptions.MaxRetries,
            RetryBaseDelaySeconds = economicOptions.RetryBaseDelaySeconds,
            Series = ParseEconomicSeries(economicOptions.TrackedSeries)
        });
        var analystOptions = configuration
            .GetSection(AnalystOptions.SectionName)
            .Get<AnalystOptions>() ?? new AnalystOptions();
        services.AddSingleton(analystOptions);
        services.AddSingleton(new AnalystSettings
        {
            Enabled = analystOptions.Enabled,
            Provider = analystOptions.Provider,
            ProviderKey = analystOptions.ProviderKey,
            Symbol = analystOptions.Symbol,
            InitialLookbackDays = analystOptions.InitialLookbackDays,
            CollectionOverlapMinutes = analystOptions.CollectionOverlapMinutes,
            SyncIntervalMinutes = analystOptions.SyncIntervalMinutes,
            ProviderPageSize = analystOptions.ProviderPageSize,
            MaximumPagesPerSync = analystOptions.MaximumPagesPerSync,
            MaximumPageSize = analystOptions.MaximumPageSize,
            MaximumCollectionRangeDays = analystOptions.MaximumCollectionRangeDays,
            MaxRetries = analystOptions.MaxRetries,
            RetryBaseDelaySeconds = analystOptions.RetryBaseDelaySeconds,
            RelevanceKeywords = ParseValues(analystOptions.RelevanceKeywords)
        });
        var evidenceOptions = configuration
            .GetSection(EvidenceOptions.SectionName)
            .Get<EvidenceOptions>() ?? new EvidenceOptions();
        services.AddSingleton(new EvidenceSettings
        {
            MaximumPageSize = evidenceOptions.MaximumPageSize,
            MaximumQueryRangeDays = evidenceOptions.MaximumQueryRangeDays,
            DefaultPackLookbackDays = evidenceOptions.DefaultPackLookbackDays,
            MaximumPackLookbackDays = evidenceOptions.MaximumPackLookbackDays,
            MaximumPackItemsPerType = evidenceOptions.MaximumPackItemsPerType,
            IngestionBatchSize = evidenceOptions.IngestionBatchSize,
            ConflictWindowHours = evidenceOptions.ConflictWindowHours,
            MaximumConflicts = evidenceOptions.MaximumConflicts
        });
        var aiInterpretationOptions = configuration
            .GetSection(AiInterpretationOptions.SectionName)
            .Get<AiInterpretationOptions>() ?? new AiInterpretationOptions();
        services.AddSingleton(new AiInterpretationSettings
        {
            PromptVersion = aiInterpretationOptions.PromptVersion,
            DefaultLookbackHours = aiInterpretationOptions.DefaultLookbackHours,
            MaximumLookbackHours = aiInterpretationOptions.MaximumLookbackHours,
            MaximumEvidenceItems = aiInterpretationOptions.MaximumEvidenceItems,
            MaximumCompressedCharacters = aiInterpretationOptions.MaximumCompressedCharacters,
            CurrentContextCacheMinutes = aiInterpretationOptions.CurrentContextCacheMinutes,
            MaximumPageSize = aiInterpretationOptions.MaximumPageSize
        });
        var aiSpecialistOptions = configuration
            .GetSection(AiSpecialistsOptions.SectionName)
            .Get<AiSpecialistsOptions>() ?? new AiSpecialistsOptions();
        services.AddSingleton(new AiSpecialistCatalog(
        [
            Map(AiSpecialist.News, aiSpecialistOptions.News),
            Map(AiSpecialist.Candle, aiSpecialistOptions.Candle),
            Map(AiSpecialist.Structure, aiSpecialistOptions.Structure),
            Map(AiSpecialist.Liquidity, aiSpecialistOptions.Liquidity),
            Map(AiSpecialist.Flow, aiSpecialistOptions.Flow),
            Map(AiSpecialist.Ktr, aiSpecialistOptions.Ktr),
            Map(AiSpecialist.Risk, aiSpecialistOptions.Risk),
            Map(AiSpecialist.Master, aiSpecialistOptions.Master)
        ]));

        if (databaseOptions.Enabled)
        {
            services.AddDbContext<XauAiDbContext>(options =>
                options.UseSqlServer(
                    databaseOptions.ConnectionString,
                    sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                        sqlOptions.MigrationsAssembly(typeof(XauAiDbContext).Assembly.FullName);
                    }));

            services.AddHealthChecks()
                .AddDbContextCheck<XauAiDbContext>("sqlserver", tags: ["database", "ready"]);

            services.AddScoped<IMarketCandleStore, EfMarketCandleStore>();
            services.AddScoped<MarketDataReferenceResolver>();
            services.AddScoped<IMarketDataQueryStore, EfMarketDataQueryStore>();
            services.AddScoped<IMarketDataSyncStateStore, EfMarketDataSyncStateStore>();
            services.AddScoped<IMarketDataSourceComparisonStore, EfMarketDataSourceComparisonStore>();
            services.AddScoped<NewsReferenceResolver>();
            services.AddScoped<INewsArticleStore, EfNewsArticleStore>();
            services.AddScoped<INewsQueryStore, EfNewsQueryStore>();
            services.AddScoped<INewsCollectionStateStore, EfNewsCollectionStateStore>();
            services.AddScoped<EconomicReferenceResolver>();
            services.AddScoped<IEconomicSeriesStore, EfEconomicSeriesStore>();
            services.AddScoped<IEconomicObservationStore, EfEconomicObservationStore>();
            services.AddScoped<IEconomicSyncStateStore, EfEconomicSyncStateStore>();
            services.AddScoped<AnalystReferenceResolver>();
            services.AddScoped<IAnalystIngestionStore, EfAnalystIngestionStore>();
            services.AddScoped<IAnalystQueryStore, EfAnalystQueryStore>();
            services.AddScoped<IAnalystSyncStateStore, EfAnalystSyncStateStore>();
            services.AddScoped<IEvidenceStore, EfEvidenceStore>();
            services.AddScoped<IAiInterpretationStore, EfAiInterpretationStore>();
            services.AddScoped<ILocalSignalStore, EfLocalSignalStore>();
        }
        else
        {
            services.AddSingleton<IMarketCandleStore, DisabledMarketCandleStore>();
            services.AddSingleton<IMarketDataQueryStore, DisabledMarketDataQueryStore>();
            services.AddSingleton<IMarketDataSyncStateStore, DisabledMarketDataSyncStateStore>();
            services.AddSingleton<IMarketDataSourceComparisonStore, DisabledMarketDataSourceComparisonStore>();
            services.AddSingleton<INewsArticleStore, DisabledNewsArticleStore>();
            services.AddSingleton<INewsQueryStore, DisabledNewsQueryStore>();
            services.AddSingleton<INewsCollectionStateStore, DisabledNewsCollectionStateStore>();
            services.AddSingleton<IEconomicSeriesStore, DisabledEconomicSeriesStore>();
            services.AddSingleton<IEconomicObservationStore, DisabledEconomicObservationStore>();
            services.AddSingleton<IEconomicSyncStateStore, DisabledEconomicSyncStateStore>();
            services.AddSingleton<IAnalystIngestionStore, DisabledAnalystIngestionStore>();
            services.AddSingleton<IAnalystQueryStore, DisabledAnalystQueryStore>();
            services.AddSingleton<IAnalystSyncStateStore, DisabledAnalystSyncStateStore>();
            services.AddSingleton<IEvidenceStore, DisabledEvidenceStore>();
            services.AddSingleton<IAiInterpretationStore, DisabledAiInterpretationStore>();
            services.AddSingleton<ILocalSignalStore, DisabledLocalSignalStore>();
        }

        services.AddSingleton(new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            ConnectTimeout = TimeSpan.FromSeconds(Math.Min(newsOptions.TimeoutSeconds, 30))
        })
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
        services.AddSingleton<INewsProvider, NewsDataProvider>();
        services.AddSingleton<IAiProvider, OpenAiCompatibleProvider>();
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();
        services.AddHostedService<NewsCollectionWorker>();
        services.AddHealthChecks()
            .AddCheck<NewsProviderHealthCheck>(
                "news-provider",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["news", "ready"]);

        services.AddSingleton<IEconomicDataProvider>(serviceProvider =>
            new FredEconomicDataProvider(
                new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    ConnectTimeout = TimeSpan.FromSeconds(Math.Min(economicOptions.TimeoutSeconds, 30))
                })
                {
                    Timeout = Timeout.InfiniteTimeSpan
                },
                economicOptions,
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddHostedService<EconomicDataWorker>();
        services.AddHealthChecks()
            .AddCheck<EconomicProviderHealthCheck>(
                "economic-provider",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["economic-data", "ready"]);

        services.AddSingleton<IAnalystDataProvider>(serviceProvider =>
            new RssAtomAnalystDataProvider(
                new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    ConnectTimeout = TimeSpan.FromSeconds(Math.Min(analystOptions.TimeoutSeconds, 30))
                })
                {
                    Timeout = Timeout.InfiniteTimeSpan
                },
                analystOptions,
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddHostedService<AnalystSynchronizationWorker>();
        services.AddHealthChecks()
            .AddCheck<AnalystProviderHealthCheck>(
                "analyst-provider",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["analyst-data", "ready"]);

        services.AddSingleton<AllTickRealtimeState>();
        services.AddSingleton(serviceProvider =>
            new AllTickHttpClient(
                new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    ConnectTimeout = TimeSpan.FromSeconds(Math.Min(allTickOptions.RequestTimeoutSeconds, 30))
                })
                {
                    BaseAddress = new Uri(allTickOptions.HttpBaseUrl),
                    Timeout = Timeout.InfiniteTimeSpan
                },
                allTickOptions,
                serviceProvider.GetRequiredService<TimeProvider>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AllTickHttpClient>>()));
        services.AddSingleton<AllTickMarketDataProvider>();
        services.AddSingleton(serviceProvider =>
            new TwelveDataHttpClient(
                new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    ConnectTimeout = TimeSpan.FromSeconds(Math.Min(twelveDataOptions.RequestTimeoutSeconds, 30))
                })
                {
                    BaseAddress = new Uri(twelveDataOptions.BaseUrl),
                    Timeout = Timeout.InfiniteTimeSpan
                },
                twelveDataOptions,
                serviceProvider.GetRequiredService<TimeProvider>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TwelveDataHttpClient>>()));
        services.AddSingleton<TwelveDataMarketDataProvider>();
        services.AddSingleton<IReferenceMarketDataProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<TwelveDataMarketDataProvider>());
        services.AddSingleton<IMarketDataProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<AllTickMarketDataProvider>());
        services.AddHostedService<AllTickRealtimeWorker>();
        services.AddHostedService<TwelveDataReferenceWorker>();
        services.AddHostedService<MarketDataSynchronizationWorker>();
        services.AddHostedService<LocalAnalystWorker>();
        services.AddHealthChecks()
            .AddCheck<MarketDataProviderHealthCheck>(
                "market-data-provider",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["market-data", "ready"]);

        return services;
    }

    private static void AddValidatedOptions<TOptions, TValidator>(
        IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName), binder =>
                binder.ErrorOnUnknownConfiguration = true)
            .ValidateOnStart();
    }

    private static IReadOnlyList<MarketTimeframe> ParseTimeframes(string values) =>
        [.. values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => MarketTimeframes.TryParse(value, out var timeframe) ? (MarketTimeframe?)timeframe : null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()];

    private static IReadOnlyList<int> ParsePeriods(string values) =>
        [.. values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var period) ? (int?)period : null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .Order()];

    private static IReadOnlyList<string> ParseValues(string values) =>
        [.. values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static NewsRelevanceLevel ParseRelevance(string value) =>
        Enum.TryParse<NewsRelevanceLevel>(value, true, out var parsed)
            ? parsed
            : NewsRelevanceLevel.Medium;

    private static IReadOnlyList<EconomicSeriesDefinition> ParseEconomicSeries(string values) =>
        [.. values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && parts.All(part => !string.IsNullOrWhiteSpace(part)))
            .Select(parts => new EconomicSeriesDefinition(
                parts[0].ToUpperInvariant(),
                parts[1]))
            .DistinctBy(definition => definition.ExternalSeriesId, StringComparer.OrdinalIgnoreCase)];

    private static AiSpecialistConfiguration Map(AiSpecialist specialist, AiSpecialistOptions options) => new(
        specialist,
        options.Enabled,
        options.Provider,
        options.Adapter,
        options.RequiresApiKey,
        options.ApiKey,
        options.Model,
        options.BaseUrl,
        options.Temperature,
        options.TimeoutSeconds,
        options.MaxOutputTokens,
        options.MaxRetries,
        options.RequestsPerMinute);
}
