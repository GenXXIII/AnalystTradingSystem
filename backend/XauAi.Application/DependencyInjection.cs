using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application.MarketData;
using XauAi.Application.News;
using XauAi.Application.EconomicData;
using XauAi.Application.SystemStatus;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.TryAddSingleton(new MarketDataPipelineSettings());
        services.TryAddSingleton(new TechnicalAnalysisSettings());
        services.TryAddSingleton(new NewsSettings());
        services.TryAddSingleton(new EconomicDataSettings());
        services.AddSingleton<IMarketSessionCalendar, DefaultMarketSessionCalendar>();
        services.AddScoped<IPlatformStatusService, PlatformStatusService>();
        services.AddScoped<IMarketDataSynchronizationService, MarketDataSynchronizationService>();
        services.AddScoped<IMarketDataIngestionService, MarketDataIngestionService>();
        services.AddScoped<IMarketDataQueryService, MarketDataQueryService>();
        services.AddScoped<IIndicatorCalculator, IndicatorCalculator>();
        services.AddScoped<ICandlestickAnalyzer, CandlestickAnalyzer>();
        services.AddScoped<IMarketStructureAnalyzer, MarketStructureAnalyzer>();
        services.AddScoped<ISupportResistanceAnalyzer, SupportResistanceAnalyzer>();
        services.AddScoped<IPriceActionAnalyzer, PriceActionAnalyzer>();
        services.AddScoped<IVolatilityAnalyzer, VolatilityAnalyzer>();
        services.AddScoped<ITechnicalAnalysisService, TechnicalAnalysisService>();
        services.AddScoped<INewsRelevanceClassifier, NewsRelevanceClassifier>();
        services.AddScoped<INewsArticleNormalizer, NewsArticleNormalizer>();
        services.AddScoped<INewsCollectionService, NewsCollectionService>();
        services.AddScoped<INewsQueryService, NewsQueryService>();
        services.AddSingleton<INewsRetryDelay, NewsRetryDelay>();
        services.AddScoped<IEconomicDataSynchronizationService, EconomicDataSynchronizationService>();
        services.AddScoped<IEconomicDataQueryService, EconomicDataQueryService>();
        services.AddSingleton<IEconomicRetryDelay, EconomicRetryDelay>();

        return services;
    }
}
