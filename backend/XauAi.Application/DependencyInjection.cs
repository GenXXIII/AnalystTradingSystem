using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application.AI;
using XauAi.Application.Analysts;
using XauAi.Application.Evidence;
using XauAi.Application.MarketData;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.News;
using XauAi.Application.EconomicData;
using XauAi.Application.SystemStatus;
using XauAi.Application.TechnicalAnalysis;
using XauAi.Application.TargetAnalysis;
using XauAi.Application.FullAnalysis;

namespace XauAi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.TryAddSingleton(new MarketDataPipelineSettings());
        services.TryAddSingleton(new TechnicalAnalysisSettings());
        services.TryAddSingleton(new LocalAnalystSettings());
        services.TryAddSingleton(new NewsSettings());
        services.TryAddSingleton(new EconomicDataSettings());
        services.TryAddSingleton(new AnalystSettings());
        services.TryAddSingleton(new EvidenceSettings());
        services.TryAddSingleton(new AiInterpretationSettings());
        services.TryAddSingleton(new TargetAnalystSettings());
        services.TryAddSingleton(new FullAnalystSettings());
        services.AddSingleton<IMarketSessionCalendar, DefaultMarketSessionCalendar>();
        services.AddScoped<IPlatformStatusService, PlatformStatusService>();
        services.AddScoped<IMarketDataSynchronizationService, MarketDataSynchronizationService>();
        services.AddScoped<IMarketDataIngestionService, MarketDataIngestionService>();
        services.AddScoped<IMarketDataQueryService, MarketDataQueryService>();
        services.AddScoped<IMarketDataQualityService, MarketDataQualityService>();
        services.AddScoped<IIndicatorCalculator, IndicatorCalculator>();
        services.AddScoped<ICandlestickAnalyzer, CandlestickAnalyzer>();
        services.AddScoped<IMarketStructureAnalyzer, MarketStructureAnalyzer>();
        services.AddScoped<ISupportResistanceAnalyzer, SupportResistanceAnalyzer>();
        services.AddScoped<IPriceActionAnalyzer, PriceActionAnalyzer>();
        services.AddScoped<IVolatilityAnalyzer, VolatilityAnalyzer>();
        services.AddScoped<ITechnicalAnalysisService, TechnicalAnalysisService>();
        services.AddSingleton<ILocalSignalEngine, LocalSignalEngine>();
        services.AddScoped<ILocalAnalystService, LocalAnalystService>();
        services.AddScoped<INewsRelevanceClassifier, NewsRelevanceClassifier>();
        services.AddScoped<INewsArticleNormalizer, NewsArticleNormalizer>();
        services.AddScoped<INewsCollectionService, NewsCollectionService>();
        services.AddScoped<INewsQueryService, NewsQueryService>();
        services.AddSingleton<INewsRetryDelay, NewsRetryDelay>();
        services.AddScoped<IEconomicDataSynchronizationService, EconomicDataSynchronizationService>();
        services.AddScoped<IEconomicDataQueryService, EconomicDataQueryService>();
        services.AddSingleton<IEconomicRetryDelay, EconomicRetryDelay>();
        services.AddScoped<IAnalystRelevanceFilter, AnalystRelevanceFilter>();
        services.AddScoped<IAnalystItemNormalizer, AnalystItemNormalizer>();
        services.AddScoped<IAnalystSynchronizationService, AnalystSynchronizationService>();
        services.AddScoped<IAnalystQueryService, AnalystQueryService>();
        services.AddSingleton<IAnalystRetryDelay, AnalystRetryDelay>();
        services.AddScoped<IEvidenceNormalizer, EvidenceNormalizer>();
        services.AddScoped<IEvidenceIngestionService, EvidenceIngestionService>();
        services.AddScoped<IEvidenceQueryService, EvidenceQueryService>();
        services.AddSingleton<IAiEvidenceSelector, AiEvidenceSelector>();
        services.AddSingleton<IAiEvidenceCompressor, AiEvidenceCompressor>();
        services.AddSingleton<IAiResponseValidator, AiResponseValidator>();
        services.AddSingleton<IAiRequestGate, AiRequestGate>();
        services.AddSingleton<IAiInterpretationExecutionGate, AiInterpretationExecutionGate>();
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.News,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.Candle,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.Structure,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.Liquidity,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.Flow,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.Ktr,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.Risk,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddSingleton<IAiSpecialist>(serviceProvider => new ConfiguredAiSpecialist(
            AiSpecialist.Master,
            serviceProvider.GetRequiredService<AiSpecialistCatalog>(),
            serviceProvider.GetRequiredService<IAiProviderFactory>(),
            serviceProvider.GetRequiredService<IAiRequestGate>()));
        services.AddScoped<IAiSpecialistRunner, AiSpecialistRunner>();
        services.AddScoped<IAiInterpretationService, AiInterpretationService>();
        services.AddSingleton<ITargetAiResponseValidator, TargetAiResponseValidator>();
        services.AddSingleton<ITargetResultValidator, TargetResultValidator>();
        services.AddSingleton<ITargetAiRequestGate, TargetAiRequestGate>();
        services.AddScoped<ITargetWorkspaceRunner, TargetWorkspaceRunner>();
        services.AddScoped<ITargetAnalystService, TargetAnalystService>();
        services.AddSingleton<IFullAiResponseValidator, FullAiResponseValidator>();
        services.AddSingleton<IFullResultValidator, FullResultValidator>();
        services.AddSingleton<IFullAiRequestGate, FullAiRequestGate>();
        services.AddSingleton<IFullWorkspaceRunner, FullWorkspaceRunner>();
        services.AddScoped<IFullAnalystService, FullAnalystService>();

        return services;
    }
}
