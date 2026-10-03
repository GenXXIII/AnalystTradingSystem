using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Configuration.Validation;
using XauAi.Application.MarketData;
using XauAi.Application.News;
using XauAi.Application.EconomicData;
using XauAi.Application.TechnicalAnalysis;
using XauAi.Infrastructure.MarketData;
using XauAi.Infrastructure.MarketData.Mt5;
using XauAi.Infrastructure.MarketData.Persistence;
using XauAi.Infrastructure.News;
using XauAi.Infrastructure.News.NewsData;
using XauAi.Infrastructure.News.Persistence;
using XauAi.Infrastructure.EconomicData;
using XauAi.Infrastructure.EconomicData.Fred;
using XauAi.Infrastructure.EconomicData.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers validated configuration, SQL Server persistence, and read-only MT5 market data.
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
        AddValidatedOptions<Mt5Options, Mt5OptionsValidator>(
            services, configuration, Mt5Options.SectionName);
        AddValidatedOptions<MarketDataOptions, MarketDataOptionsValidator>(
            services, configuration, MarketDataOptions.SectionName);
        AddValidatedOptions<TechnicalAnalysisOptions, TechnicalAnalysisOptionsValidator>(
            services, configuration, TechnicalAnalysisOptions.SectionName);
        AddValidatedOptions<NewsOptions, NewsOptionsValidator>(
            services, configuration, NewsOptions.SectionName);
        AddValidatedOptions<EconomicDataOptions, EconomicDataOptionsValidator>(
            services, configuration, EconomicDataOptions.SectionName);
        AddValidatedOptions<AnalystOptions, AnalystOptionsValidator>(
            services, configuration, AnalystOptions.SectionName);
        AddValidatedOptions<ApiOptions, ApiOptionsValidator>(
            services, configuration, ApiOptions.SectionName);

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();
        var marketDataOptions = configuration
            .GetSection(MarketDataOptions.SectionName)
            .Get<MarketDataOptions>() ?? new MarketDataOptions();
        var marketDataSettings = new MarketDataPipelineSettings
        {
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
            IncludeFormingCandle = marketDataOptions.IncludeFormingCandle
        };
        services.AddSingleton(marketDataSettings);
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
            services.AddScoped<NewsReferenceResolver>();
            services.AddScoped<INewsArticleStore, EfNewsArticleStore>();
            services.AddScoped<INewsQueryStore, EfNewsQueryStore>();
            services.AddScoped<INewsCollectionStateStore, EfNewsCollectionStateStore>();
            services.AddScoped<EconomicReferenceResolver>();
            services.AddScoped<IEconomicSeriesStore, EfEconomicSeriesStore>();
            services.AddScoped<IEconomicObservationStore, EfEconomicObservationStore>();
            services.AddScoped<IEconomicSyncStateStore, EfEconomicSyncStateStore>();
        }
        else
        {
            services.AddSingleton<IMarketCandleStore, DisabledMarketCandleStore>();
            services.AddSingleton<IMarketDataQueryStore, DisabledMarketDataQueryStore>();
            services.AddSingleton<IMarketDataSyncStateStore, DisabledMarketDataSyncStateStore>();
            services.AddSingleton<INewsArticleStore, DisabledNewsArticleStore>();
            services.AddSingleton<INewsQueryStore, DisabledNewsQueryStore>();
            services.AddSingleton<INewsCollectionStateStore, DisabledNewsCollectionStateStore>();
            services.AddSingleton<IEconomicSeriesStore, DisabledEconomicSeriesStore>();
            services.AddSingleton<IEconomicObservationStore, DisabledEconomicObservationStore>();
            services.AddSingleton<IEconomicSyncStateStore, DisabledEconomicSyncStateStore>();
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

        services.AddSingleton<IMt5BridgeClient, Mt5PythonBridgeClient>();
        services.AddSingleton<IMt5HostEnvironment, Mt5HostEnvironment>();
        services.AddSingleton<IMarketDataProvider, Mt5MarketDataProvider>();
        services.AddHostedService<Mt5TerminalProcessMonitor>();
        services.AddHostedService<Mt5StartupProbe>();
        services.AddHostedService<MarketDataSynchronizationWorker>();
        services.AddHealthChecks()
            .AddCheck<Mt5HealthCheck>(
                "mt5",
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
}
