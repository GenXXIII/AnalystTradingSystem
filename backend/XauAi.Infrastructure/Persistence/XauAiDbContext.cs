using Microsoft.EntityFrameworkCore;
using XauAi.Domain.AI;
using XauAi.Domain.Analysts;
using XauAi.Domain.Backtesting;
using XauAi.Domain.EconomicData;
using XauAi.Domain.Evidence;
using XauAi.Domain.Market;
using XauAi.Domain.News;
using XauAi.Domain.ReferenceData;
using XauAi.Domain.Signals;
using XauAi.Domain.Statistics;
using XauAi.Domain.Strategies;

namespace XauAi.Infrastructure.Persistence;

public sealed class XauAiDbContext(DbContextOptions<XauAiDbContext> options) : DbContext(options)
{
    public DbSet<EvidenceRecord> EvidenceRecords => Set<EvidenceRecord>();

    public DbSet<EvidenceRelation> EvidenceRelations => Set<EvidenceRelation>();

    public DbSet<EvidenceCluster> EvidenceClusters => Set<EvidenceCluster>();

    public DbSet<EvidenceClusterMember> EvidenceClusterMembers => Set<EvidenceClusterMember>();

    public DbSet<EvidenceQuarantineRecord> EvidenceQuarantineRecords => Set<EvidenceQuarantineRecord>();

    public DbSet<DataProvider> DataProviders => Set<DataProvider>();

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<TimeframeDefinition> Timeframes => Set<TimeframeDefinition>();

    public DbSet<MarketCandle> MarketCandles => Set<MarketCandle>();

    public DbSet<MarketDataSyncState> MarketDataSyncStates => Set<MarketDataSyncState>();

    public DbSet<MarketDataSyncRun> MarketDataSyncRuns => Set<MarketDataSyncRun>();

    public DbSet<TechnicalObservation> TechnicalObservations => Set<TechnicalObservation>();

    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    public DbSet<NewsArticleContent> NewsArticleContents => Set<NewsArticleContent>();

    public DbSet<NewsCollectionState> NewsCollectionStates => Set<NewsCollectionState>();

    public DbSet<NewsCollectionRun> NewsCollectionRuns => Set<NewsCollectionRun>();

    public DbSet<EconomicEvent> EconomicEvents => Set<EconomicEvent>();

    public DbSet<EconomicSeries> EconomicSeries => Set<EconomicSeries>();

    public DbSet<EconomicObservation> EconomicObservations => Set<EconomicObservation>();

    public DbSet<EconomicObservationRevision> EconomicObservationRevisions => Set<EconomicObservationRevision>();

    public DbSet<EconomicSyncState> EconomicSyncStates => Set<EconomicSyncState>();

    public DbSet<EconomicSyncRun> EconomicSyncRuns => Set<EconomicSyncRun>();

    public DbSet<AnalystStatement> AnalystStatements => Set<AnalystStatement>();

    public DbSet<AnalystSource> AnalystSources => Set<AnalystSource>();

    public DbSet<Analyst> Analysts => Set<Analyst>();

    public DbSet<AnalystPublication> AnalystPublications => Set<AnalystPublication>();

    public DbSet<AnalystSyncState> AnalystSyncStates => Set<AnalystSyncState>();

    public DbSet<AnalystSyncRun> AnalystSyncRuns => Set<AnalystSyncRun>();

    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();

    public DbSet<AiAnalysisEvidence> AiAnalysisEvidence => Set<AiAnalysisEvidence>();

    public DbSet<Strategy> Strategies => Set<Strategy>();

    public DbSet<StrategyVersion> StrategyVersions => Set<StrategyVersion>();

    public DbSet<StrategyEvaluation> StrategyEvaluations => Set<StrategyEvaluation>();

    public DbSet<StrategyEvaluationEvidence> StrategyEvaluationEvidence => Set<StrategyEvaluationEvidence>();

    public DbSet<TradingSignal> TradingSignals => Set<TradingSignal>();

    public DbSet<TradingSignalLifecycleEvent> TradingSignalLifecycleEvents => Set<TradingSignalLifecycleEvent>();

    public DbSet<LocalAnalystProcessingState> LocalAnalystProcessingStates => Set<LocalAnalystProcessingState>();

    public DbSet<TradingSignalEvidence> TradingSignalEvidence => Set<TradingSignalEvidence>();

    public DbSet<SignalOutcome> SignalOutcomes => Set<SignalOutcome>();

    public DbSet<BacktestRun> BacktestRuns => Set<BacktestRun>();

    public DbSet<BacktestTrade> BacktestTrades => Set<BacktestTrade>();

    public DbSet<PerformanceStatistic> PerformanceStatistics => Set<PerformanceStatistic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(XauAiDbContext).Assembly);
    }
}
