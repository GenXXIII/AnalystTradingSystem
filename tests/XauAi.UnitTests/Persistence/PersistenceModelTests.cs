using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using XauAi.Domain.Analysts;
using XauAi.Domain.AI;
using XauAi.Domain.EconomicData;
using XauAi.Domain.Evidence;
using XauAi.Domain.Market;
using XauAi.Domain.News;
using XauAi.Domain.ReferenceData;
using XauAi.Infrastructure.Persistence;
using XauAi.Domain.TargetAnalysis;
using DomainTargetAnalysis = XauAi.Domain.TargetAnalysis.TargetAnalysis;

namespace XauAi.UnitTests.Persistence;

public sealed class PersistenceModelTests
{
    private readonly XauAiDbContext _dbContext = CreateDbContext();

    [Fact]
    public void Model_contains_the_complete_phase_three_foundation()
    {
        var tableNames = _dbContext.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        var expectedTables = new[]
        {
            "EvidenceRecords", "EvidenceRelations", "EvidenceClusters", "EvidenceClusterMembers",
            "EvidenceQuarantineRecords", "DataProviders", "Instruments", "Timeframes",
            "MarketCandles", "TechnicalObservations", "NewsArticles", "NewsArticleContents",
            "NewsCollectionStates", "NewsCollectionRuns",
            "EconomicEvents", "AnalystSources", "Analysts", "AnalystPublications", "AnalystStatements",
            "AnalystSyncStates", "AnalystSyncRuns", "AiAnalyses", "AiAnalysisEvidence",
            "EconomicSeries", "EconomicObservations", "EconomicObservationRevisions",
            "EconomicSyncStates", "EconomicSyncRuns",
            "Strategies", "StrategyVersions", "StrategyEvaluations", "StrategyEvaluationEvidence",
            "TradingSignals", "TradingSignalEvidence", "SignalOutcomes", "BacktestRuns",
            "BacktestTrades", "PerformanceStatistics", "TargetAnalyses", "TargetSpecialistResults",
            "TargetAnalysisEvidence", "TargetAnalysisLifecycleEvents"
        };

        Assert.True(tableNames.Count >= expectedTables.Length);
        Assert.All(expectedTables, table => Assert.Contains(table, tableNames));
    }

    [Fact]
    public void Target_analysis_model_preserves_one_result_graph_and_lifecycle_history()
    {
        AssertIndexes<DomainTargetAnalysis>(
            "IX_TargetAnalyses_Symbol_Status_AnalysisTimeUtc",
            "IX_TargetAnalyses_Instrument_Timeframe_AnalysisTimeUtc");
        AssertIndexes<TargetSpecialistResult>("UX_TargetSpecialistResults_Analysis_Workspace");
        AssertIndexes<TargetAnalysisLifecycleEvent>("IX_TargetAnalysisLifecycleEvents_Analysis_Time");

        var target = _dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DomainTargetAnalysis));
        Assert.Equal("decimal(19,8)", target?.FindProperty(nameof(DomainTargetAnalysis.TargetPrice))?.GetColumnType());
        Assert.Equal("decimal(9,6)", target?.FindProperty(nameof(DomainTargetAnalysis.Confidence))?.GetColumnType());
        Assert.Equal("nvarchar(max)", target?.FindProperty(nameof(DomainTargetAnalysis.SnapshotJson))?.GetColumnType());
    }

    [Fact]
    public void Candle_model_enforces_precision_identity_and_range_query_indexes()
    {
        var entity = _dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(MarketCandle));
        Assert.NotNull(entity);

        foreach (var propertyName in new[] { "Open", "High", "Low", "Close", "Spread" })
        {
            Assert.Equal("decimal(19,8)", entity.FindProperty(propertyName)?.GetColumnType());
        }

        var indexes = entity.GetIndexes().ToDictionary(
            index => index.GetDatabaseName()!,
            StringComparer.Ordinal);
        var identityIndex = indexes["UX_MarketCandles_Instrument_Timeframe_OpenTime_Provider"];

        Assert.True(identityIndex.IsUnique);
        Assert.True(identityIndex.IsClustered());
        Assert.Contains("IX_MarketCandles_Provider_Instrument_Timeframe_OpenTime", indexes.Keys);
    }

    [Fact]
    public void Utc_converter_normalizes_offsets_before_persistence()
    {
        var entity = _dbContext.Model.FindEntityType(typeof(MarketCandle));
        var property = entity?.FindProperty(nameof(MarketCandle.OpenTimeUtc));
        var converter = property?.GetValueConverter();
        var source = new DateTimeOffset(2026, 10, 1, 9, 30, 0, TimeSpan.FromHours(7));

        var converted = Assert.IsType<DateTimeOffset>(converter?.ConvertToProvider(source));

        Assert.Equal(TimeSpan.Zero, converted.Offset);
        Assert.Equal(source.UtcDateTime, converted.UtcDateTime);
        Assert.Equal("datetimeoffset(7)", property?.GetColumnType());
    }

    [Fact]
    public void Future_market_range_query_uses_the_indexed_identity_shape()
    {
        var instrumentId = Guid.NewGuid();
        var timeframeId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;

        var sql = _dbContext.MarketCandles
            .Where(candle =>
                candle.InstrumentId == instrumentId &&
                candle.TimeframeId == timeframeId &&
                candle.DataProviderId == providerId &&
                candle.OpenTimeUtc >= start &&
                candle.OpenTimeUtc < end)
            .OrderBy(candle => candle.OpenTimeUtc)
            .ToQueryString();

        Assert.Contains("[InstrumentId]", sql, StringComparison.Ordinal);
        Assert.Contains("[TimeframeId]", sql, StringComparison.Ordinal);
        Assert.Contains("[DataProviderId]", sql, StringComparison.Ordinal);
        Assert.Contains("[OpenTimeUtc]", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Source_query_indexes_cover_provider_external_id_and_time_filters()
    {
        AssertIndexes<Instrument>("UX_Instruments_Symbol");
        AssertIndexes<TimeframeDefinition>("UX_Timeframes_Code");
        AssertIndexes<NewsArticle>(
            "UX_NewsArticles_Provider_ExternalId",
            "UX_NewsArticles_CanonicalUrlHash",
            "UX_NewsArticles_ContentHash",
            "IX_NewsArticles_PublishedAtUtc",
            "IX_NewsArticles_Instrument_PublishedAtUtc",
            "IX_NewsArticles_Category_PublishedAtUtc",
            "IX_NewsArticles_Relevance_PublishedAtUtc",
            "IX_NewsArticles_Source_PublishedAtUtc");
        AssertIndexes<EconomicEvent>(
            "UX_EconomicEvents_Provider_ExternalId",
            "IX_EconomicEvents_ScheduledAtUtc",
            "IX_EconomicEvents_Currency_ScheduledAtUtc");
        AssertIndexes<EconomicSeries>(
            "UX_EconomicSeries_Provider_ExternalSeriesId",
            "IX_EconomicSeries_Category_Active",
            "IX_EconomicSeries_Frequency_Active");
        AssertIndexes<EconomicObservation>("UX_EconomicObservations_Series_ObservationDate");
        var observation = _dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(EconomicObservation));
        Assert.Equal("decimal(28,8)", observation?.FindProperty(nameof(EconomicObservation.Value))?.GetColumnType());
        AssertIndexes<AnalystStatement>(
            "UX_AnalystStatements_Publication_ExternalId",
            "UX_AnalystStatements_Publication_ClaimHash",
            "IX_AnalystStatements_Instrument_PublishedAtUtc",
            "IX_AnalystStatements_Source_PublishedAtUtc",
            "IX_AnalystStatements_Analyst_PublishedAtUtc",
            "IX_AnalystStatements_Direction_PublishedAtUtc");
        AssertIndexes<AnalystSource>("UX_AnalystSources_Provider_IdentityHash");
        AssertIndexes<Analyst>("UX_Analysts_Source_IdentityHash");
        AssertIndexes<AnalystPublication>(
            "UX_AnalystPublications_Provider_Identity_Version",
            "IX_AnalystPublications_ContentHash",
            "IX_AnalystPublications_Source_PublishedAtUtc");
        AssertIndexes<EvidenceRecord>(
            "UX_EvidenceRecords_IdentityHash",
            "IX_EvidenceRecords_Source_ExternalId",
            "IX_EvidenceRecords_ContentHash",
            "IX_EvidenceRecords_Instrument_AvailableAtUtc",
            "IX_EvidenceRecords_Type_AvailableAtUtc",
            "IX_EvidenceRecords_SourceType_AvailableAtUtc",
            "IX_EvidenceRecords_Timeframe_AvailableAtUtc");
        var evidence = _dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(EvidenceRecord));
        Assert.Equal("decimal(28,10)", evidence?.FindProperty(nameof(EvidenceRecord.NumericValue))?.GetColumnType());
        AssertIndexes<EvidenceCluster>(
            "UX_EvidenceClusters_Type_Key",
            "IX_EvidenceClusters_Type_EventTimeUtc");
        AssertIndexes<AiAnalysis>(
            "IX_AiAnalyses_Instrument_AnalysisTimeUtc",
            "IX_AiAnalyses_Specialist_Type_Lifecycle_AnalysisTimeUtc",
            "IX_AiAnalyses_CacheKey_Status_Lifecycle",
            "IX_AiAnalyses_Model_PromptVersion",
            "IX_AiAnalyses_Status_CreatedAtUtc");
        var interpretation = _dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(AiAnalysis));
        Assert.Equal("decimal(9,6)", interpretation?.FindProperty(nameof(AiAnalysis.Confidence))?.GetColumnType());
        Assert.Equal("nvarchar(max)", interpretation?.FindProperty(nameof(AiAnalysis.Output))?.GetColumnType());
        var cacheIndex = interpretation?.GetIndexes().Single(index =>
            index.GetDatabaseName() == "IX_AiAnalyses_CacheKey_Status_Lifecycle");
        Assert.True(cacheIndex?.IsUnique);
        Assert.Equal(
            "[Status] = 'Completed' AND [LifecycleStatus] = 'Current'",
            cacheIndex?.GetFilter());
    }

    private void AssertIndexes<TEntity>(params string[] expectedNames)
    {
        var entity = _dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        var actualNames = entity.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(expectedNames, name => Assert.Contains(name, actualNames));
    }

    private static XauAiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseSqlServer("Server=localhost;Database=XauAiModelTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new XauAiDbContext(options);
    }
}
