using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementEvidenceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvailableAtUtc",
                table: "EvidenceRecords",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "CanonicalSymbol",
                table: "EvidenceRecords",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "XAUUSD");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "EvidenceRecords",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CollectedAtUtc",
                table: "EvidenceRecords",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Completeness",
                table: "EvidenceRecords",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "EvidenceRecords",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "EvidenceRecords",
                type: "char(3)",
                unicode: false,
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DataProviderId",
                table: "EvidenceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "EvidenceRecords",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "EvidenceType",
                table: "EvidenceRecords",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Market");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "EvidenceRecords",
                type: "varchar(256)",
                unicode: false,
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityHash",
                table: "EvidenceRecords",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Importance",
                table: "EvidenceRecords",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<Guid>(
                name: "InstrumentId",
                table: "EvidenceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRelevant",
                table: "EvidenceRecords",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "EvidenceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NumericValue",
                table: "EvidenceRecords",
                type: "decimal(28,10)",
                precision: 28,
                scale: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalCategory",
                table: "EvidenceRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalDirection",
                table: "EvidenceRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalImportance",
                table: "EvidenceRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalSourceUrl",
                table: "EvidenceRecords",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalSymbol",
                table: "EvidenceRecords",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalUnit",
                table: "EvidenceRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalValue",
                table: "EvidenceRecords",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAtUtc",
                table: "EvidenceRecords",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Quality",
                table: "EvidenceRecords",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "RelevanceReason",
                table: "EvidenceRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "EvidenceRecords",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.AddColumn<string>(
                name: "SourceReliability",
                table: "EvidenceRecords",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "EvidenceRecords",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "EvidenceRecords",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeframeCode",
                table: "EvidenceRecords",
                type: "varchar(8)",
                unicode: false,
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TimeframeId",
                table: "EvidenceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimestampQuality",
                table: "EvidenceRecords",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "EvidenceRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "EvidenceRecords",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "EvidenceRecords",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidFromUtc",
                table: "EvidenceRecords",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidToUtc",
                table: "EvidenceRecords",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [EvidenceRecords]
                SET [IdentityHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varchar(36), [Id])), 2)),
                    [AvailableAtUtc] = [ObservedAtUtc],
                    [ValidFromUtc] = [ObservedAtUtc],
                    [UpdatedAtUtc] = COALESCE([CreatedAtUtc], SYSUTCDATETIME()),
                    [RelevanceReason] = 'Migrated from the existing attributed evidence foundation.';

                UPDATE evidence
                SET evidence.[DataProviderId] = candle.[DataProviderId],
                    evidence.[InstrumentId] = candle.[InstrumentId],
                    evidence.[TimeframeId] = candle.[TimeframeId],
                    evidence.[EvidenceType] = 'Market',
                    evidence.[SourceType] = 'InternalMarketData',
                    evidence.[SourceKey] = provider.[Key],
                    evidence.[ExternalId] = CONCAT(candle.[ProviderSymbol], ':', timeframe.[Code], ':', CONVERT(varchar(33), candle.[OpenTimeUtc], 127)),
                    evidence.[CanonicalSymbol] = instrument.[Symbol],
                    evidence.[OriginalSymbol] = candle.[ProviderSymbol],
                    evidence.[TimeframeCode] = timeframe.[Code],
                    evidence.[ObservedAtUtc] = candle.[OpenTimeUtc],
                    evidence.[AvailableAtUtc] = candle.[CloseTimeUtc],
                    evidence.[CollectedAtUtc] = candle.[FetchedAtUtc],
                    evidence.[ValidFromUtc] = candle.[CloseTimeUtc],
                    evidence.[Title] = CONCAT(instrument.[Symbol], ' ', timeframe.[Code], ' candle'),
                    evidence.[Summary] = CONCAT('OHLC ', candle.[Open], ' / ', candle.[High], ' / ', candle.[Low], ' / ', candle.[Close]),
                    evidence.[NumericValue] = candle.[Close],
                    evidence.[OriginalValue] = CONVERT(nvarchar(100), candle.[Close]),
                    evidence.[Unit] = 'Price',
                    evidence.[OriginalUnit] = 'price',
                    evidence.[Direction] = CASE WHEN candle.[Close] > candle.[Open] THEN 'Bullish' WHEN candle.[Close] < candle.[Open] THEN 'Bearish' ELSE 'Neutral' END,
                    evidence.[OriginalDirection] = CASE WHEN candle.[Close] > candle.[Open] THEN 'Bullish' WHEN candle.[Close] < candle.[Open] THEN 'Bearish' ELSE 'Neutral' END,
                    evidence.[Importance] = 'Unknown',
                    evidence.[Category] = 'Commodity',
                    evidence.[OriginalCategory] = 'MarketCandle',
                    evidence.[CurrencyCode] = 'USD',
                    evidence.[Quality] = CASE WHEN candle.[IsComplete] = 1 THEN 'High' ELSE 'Low' END,
                    evidence.[Completeness] = CASE WHEN candle.[IsComplete] = 1 THEN 'Complete' ELSE 'Partial' END,
                    evidence.[TimestampQuality] = 'Exact',
                    evidence.[SourceReliability] = 'Known',
                    evidence.[IsRelevant] = 1,
                    evidence.[RelevanceReason] = 'Canonical instrument is XAUUSD.'
                FROM [EvidenceRecords] evidence
                INNER JOIN [MarketCandles] candle ON candle.[Id] = evidence.[Id]
                INNER JOIN [DataProviders] provider ON provider.[Id] = candle.[DataProviderId]
                INNER JOIN [Instruments] instrument ON instrument.[Id] = candle.[InstrumentId]
                INNER JOIN [Timeframes] timeframe ON timeframe.[Id] = candle.[TimeframeId];

                UPDATE evidence
                SET evidence.[DataProviderId] = article.[DataProviderId],
                    evidence.[InstrumentId] = article.[InstrumentId],
                    evidence.[EvidenceType] = 'News',
                    evidence.[SourceType] = 'NewsProvider',
                    evidence.[SourceKey] = provider.[Key],
                    evidence.[ExternalId] = COALESCE(article.[ProviderArticleId], article.[CanonicalUrlHash]),
                    evidence.[ContentHash] = article.[ContentHash],
                    evidence.[CanonicalSymbol] = COALESCE(instrument.[Symbol], 'XAUUSD'),
                    evidence.[OriginalSymbol] = COALESCE(instrument.[Symbol], 'XAUUSD'),
                    evidence.[ObservedAtUtc] = article.[PublishedAtUtc],
                    evidence.[AvailableAtUtc] = article.[PublishedAtUtc],
                    evidence.[PublishedAtUtc] = article.[PublishedAtUtc],
                    evidence.[CollectedAtUtc] = article.[CollectedAtUtc],
                    evidence.[ValidFromUtc] = article.[PublishedAtUtc],
                    evidence.[Title] = article.[Title],
                    evidence.[Summary] = article.[Description],
                    evidence.[Direction] = 'Unknown',
                    evidence.[Importance] = 'Unknown',
                    evidence.[Category] = CASE
                        WHEN UPPER(article.[PrimaryCategory]) LIKE '%FED%' OR UPPER(article.[PrimaryCategory]) LIKE '%FOMC%' THEN 'Fed'
                        WHEN UPPER(article.[PrimaryCategory]) LIKE '%INFLATION%' OR UPPER(article.[PrimaryCategory]) LIKE '%CPI%' THEN 'Inflation'
                        WHEN UPPER(article.[PrimaryCategory]) LIKE '%EMPLOY%' OR UPPER(article.[PrimaryCategory]) LIKE '%NFP%' THEN 'Employment'
                        WHEN UPPER(article.[PrimaryCategory]) LIKE '%USD%' OR UPPER(article.[PrimaryCategory]) LIKE '%DOLLAR%' THEN 'Usd'
                        WHEN UPPER(article.[PrimaryCategory]) LIKE '%GOLD%' OR UPPER(article.[PrimaryCategory]) LIKE '%COMMOD%' THEN 'Commodity'
                        ELSE 'Other' END,
                    evidence.[OriginalCategory] = article.[PrimaryCategory],
                    evidence.[OriginalSourceUrl] = article.[SourceUrl],
                    evidence.[Quality] = CASE WHEN article.[ProviderArticleId] IS NOT NULL THEN 'High' ELSE 'Medium' END,
                    evidence.[Completeness] = CASE WHEN article.[ProviderArticleId] IS NOT NULL THEN 'Complete' ELSE 'Partial' END,
                    evidence.[TimestampQuality] = 'Exact',
                    evidence.[SourceReliability] = 'Known',
                    evidence.[IsRelevant] = 1,
                    evidence.[RelevanceReason] = CONCAT('Existing deterministic news relevance classification: ', article.[RelevanceLevel], '.')
                FROM [EvidenceRecords] evidence
                INNER JOIN [NewsArticles] article ON article.[Id] = evidence.[Id]
                INNER JOIN [DataProviders] provider ON provider.[Id] = article.[DataProviderId]
                LEFT JOIN [Instruments] instrument ON instrument.[Id] = article.[InstrumentId];

                UPDATE evidence
                SET evidence.[DataProviderId] = series.[DataProviderId],
                    evidence.[InstrumentId] = instrument.[Id],
                    evidence.[EvidenceType] = 'Economic',
                    evidence.[SourceType] = 'EconomicProvider',
                    evidence.[SourceKey] = provider.[Key],
                    evidence.[ExternalId] = CONCAT(series.[ExternalSeriesId], ':', CONVERT(varchar(10), observation.[ObservationDate], 23)),
                    evidence.[CanonicalSymbol] = 'XAUUSD',
                    evidence.[OriginalSymbol] = series.[ExternalSeriesId],
                    evidence.[ObservedAtUtc] = TODATETIMEOFFSET(CAST(observation.[ObservationDate] AS datetime2), '+00:00'),
                    evidence.[AvailableAtUtc] = observation.[FetchedAtUtc],
                    evidence.[CollectedAtUtc] = observation.[FetchedAtUtc],
                    evidence.[ValidFromUtc] = observation.[FetchedAtUtc],
                    evidence.[Title] = series.[Name],
                    evidence.[Summary] = CONCAT(series.[Name], ' observation for ', CONVERT(varchar(10), observation.[ObservationDate], 23), '.'),
                    evidence.[NumericValue] = observation.[Value],
                    evidence.[OriginalValue] = observation.[OriginalValue],
                    evidence.[Unit] = CASE
                        WHEN UPPER(series.[Units]) IN ('%', 'PERCENT', 'PERCENTAGE') THEN 'Percent'
                        WHEN UPPER(series.[Units]) LIKE '%INDEX%' THEN 'Index'
                        WHEN UPPER(series.[Units]) LIKE '%BILLION%' THEN 'UsdBillions'
                        WHEN UPPER(series.[Units]) LIKE '%TRILLION%' THEN 'UsdTrillions'
                        WHEN UPPER(series.[Units]) LIKE '%MILLION%' THEN 'Millions'
                        WHEN UPPER(series.[Units]) LIKE '%THOUSAND%' THEN 'Thousands'
                        ELSE 'Unknown' END,
                    evidence.[OriginalUnit] = series.[Units],
                    evidence.[Direction] = 'Unknown',
                    evidence.[Importance] = 'Unknown',
                    evidence.[Category] = CASE
                        WHEN UPPER(series.[Category]) LIKE '%INFLATION%' THEN 'Inflation'
                        WHEN UPPER(series.[Category]) LIKE '%EMPLOY%' THEN 'Employment'
                        WHEN UPPER(series.[Category]) LIKE '%INTEREST%' THEN 'InterestRates'
                        WHEN UPPER(series.[Category]) LIKE '%TREASURY%' THEN 'Treasury'
                        WHEN UPPER(series.[Category]) LIKE '%GDP%' OR UPPER(series.[Category]) LIKE '%GROWTH%' THEN 'Gdp'
                        ELSE 'Other' END,
                    evidence.[OriginalCategory] = series.[Category],
                    evidence.[CurrencyCode] = NULLIF(series.[CurrencyCode], ''),
                    evidence.[Quality] = 'Medium',
                    evidence.[Completeness] = 'Complete',
                    evidence.[TimestampQuality] = 'DateOnly',
                    evidence.[SourceReliability] = 'Known',
                    evidence.[IsRelevant] = 1,
                    evidence.[RelevanceReason] = CONCAT('Tracked economic series category: ', series.[Category], '.')
                FROM [EvidenceRecords] evidence
                INNER JOIN [EconomicObservations] observation ON observation.[Id] = evidence.[Id]
                INNER JOIN [EconomicSeries] series ON series.[Id] = observation.[EconomicSeriesId]
                INNER JOIN [DataProviders] provider ON provider.[Id] = series.[DataProviderId]
                LEFT JOIN [Instruments] instrument ON instrument.[Symbol] = 'XAUUSD' AND instrument.[IsActive] = 1;

                UPDATE evidence
                SET evidence.[DataProviderId] = event.[DataProviderId],
                    evidence.[InstrumentId] = instrument.[Id],
                    evidence.[EvidenceType] = 'Economic',
                    evidence.[SourceType] = 'EconomicProvider',
                    evidence.[SourceKey] = provider.[Key],
                    evidence.[ExternalId] = event.[ExternalId],
                    evidence.[CanonicalSymbol] = 'XAUUSD',
                    evidence.[OriginalSymbol] = event.[CurrencyCode],
                    evidence.[ObservedAtUtc] = event.[ScheduledAtUtc],
                    evidence.[AvailableAtUtc] = event.[FetchedAtUtc],
                    evidence.[CollectedAtUtc] = event.[FetchedAtUtc],
                    evidence.[ValidFromUtc] = event.[FetchedAtUtc],
                    evidence.[Title] = event.[Name],
                    evidence.[NumericValue] = event.[ActualValue],
                    evidence.[OriginalValue] = CONVERT(nvarchar(256), event.[ActualValue]),
                    evidence.[OriginalUnit] = event.[ValueUnit],
                    evidence.[Direction] = 'Unknown',
                    evidence.[Importance] = CASE WHEN event.[Importance] IN ('Low', 'Medium', 'High', 'Critical') THEN event.[Importance] ELSE 'Unknown' END,
                    evidence.[OriginalImportance] = event.[Importance],
                    evidence.[Category] = 'Other',
                    evidence.[OriginalCategory] = event.[Category],
                    evidence.[CurrencyCode] = event.[CurrencyCode],
                    evidence.[Quality] = 'Medium',
                    evidence.[Completeness] = 'Complete',
                    evidence.[TimestampQuality] = 'Exact',
                    evidence.[SourceReliability] = 'Known',
                    evidence.[IsRelevant] = 1
                FROM [EvidenceRecords] evidence
                INNER JOIN [EconomicEvents] event ON event.[Id] = evidence.[Id]
                INNER JOIN [DataProviders] provider ON provider.[Id] = event.[DataProviderId]
                LEFT JOIN [Instruments] instrument ON instrument.[Symbol] = 'XAUUSD' AND instrument.[IsActive] = 1;

                UPDATE evidence
                SET evidence.[DataProviderId] = statement.[DataProviderId],
                    evidence.[InstrumentId] = statement.[InstrumentId],
                    evidence.[EvidenceType] = 'Analyst',
                    evidence.[SourceType] = 'AnalystProvider',
                    evidence.[SourceKey] = provider.[Key],
                    evidence.[ExternalId] = COALESCE(statement.[ExternalId], statement.[ClaimHash]),
                    evidence.[ContentHash] = statement.[ClaimHash],
                    evidence.[CanonicalSymbol] = statement.[InstrumentCode],
                    evidence.[OriginalSymbol] = statement.[InstrumentCode],
                    evidence.[ObservedAtUtc] = statement.[PublishedAtUtc],
                    evidence.[AvailableAtUtc] = statement.[PublishedAtUtc],
                    evidence.[PublishedAtUtc] = statement.[PublishedAtUtc],
                    evidence.[CollectedAtUtc] = statement.[FetchedAtUtc],
                    evidence.[ValidFromUtc] = statement.[PublishedAtUtc],
                    evidence.[Title] = statement.[Title],
                    evidence.[Summary] = statement.[PermittedContent],
                    evidence.[NumericValue] = statement.[TargetPrice],
                    evidence.[OriginalValue] = CONVERT(nvarchar(256), statement.[TargetPrice]),
                    evidence.[Unit] = CASE WHEN statement.[TargetCurrency] = 'USD' THEN 'Usd' ELSE 'Price' END,
                    evidence.[OriginalUnit] = statement.[TargetCurrency],
                    evidence.[Direction] = COALESCE(statement.[Direction], 'Unknown'),
                    evidence.[OriginalDirection] = statement.[Direction],
                    evidence.[Importance] = 'Unknown',
                    evidence.[Category] = CASE
                        WHEN UPPER(statement.[Category]) LIKE '%FED%' THEN 'Fed'
                        WHEN UPPER(statement.[Category]) LIKE '%INFLATION%' THEN 'Inflation'
                        WHEN UPPER(statement.[Category]) LIKE '%USD%' THEN 'Usd'
                        WHEN UPPER(statement.[Category]) LIKE '%GOLD%' OR UPPER(statement.[Category]) LIKE '%COMMOD%' THEN 'Commodity'
                        ELSE 'Other' END,
                    evidence.[OriginalCategory] = statement.[Category],
                    evidence.[CurrencyCode] = statement.[TargetCurrency],
                    evidence.[OriginalSourceUrl] = statement.[SourceUrl],
                    evidence.[Quality] = 'High',
                    evidence.[Completeness] = 'Complete',
                    evidence.[TimestampQuality] = 'Exact',
                    evidence.[SourceReliability] = 'Known',
                    evidence.[IsRelevant] = 1,
                    evidence.[RelevanceReason] = 'Passed the deterministic analyst relevance filter.'
                FROM [EvidenceRecords] evidence
                INNER JOIN [AnalystStatements] statement ON statement.[Id] = evidence.[Id]
                INNER JOIN [DataProviders] provider ON provider.[Id] = statement.[DataProviderId];

                UPDATE evidence
                SET evidence.[InstrumentId] = observation.[InstrumentId],
                    evidence.[TimeframeId] = observation.[TimeframeId],
                    evidence.[EvidenceType] = 'Technical',
                    evidence.[SourceType] = 'InternalTechnicalEngine',
                    evidence.[SourceKey] = 'internal-technical',
                    evidence.[CanonicalSymbol] = instrument.[Symbol],
                    evidence.[OriginalSymbol] = instrument.[Symbol],
                    evidence.[TimeframeCode] = timeframe.[Code],
                    evidence.[ObservedAtUtc] = observation.[ObservedAtUtc],
                    evidence.[AvailableAtUtc] = observation.[CreatedAtUtc],
                    evidence.[CollectedAtUtc] = observation.[CreatedAtUtc],
                    evidence.[ValidFromUtc] = observation.[CreatedAtUtc],
                    evidence.[Title] = observation.[Name],
                    evidence.[Summary] = observation.[ValueText],
                    evidence.[NumericValue] = observation.[NumericValue],
                    evidence.[OriginalValue] = observation.[ValueText],
                    evidence.[Direction] = 'Unknown',
                    evidence.[Importance] = 'Unknown',
                    evidence.[Category] = 'Technical',
                    evidence.[OriginalCategory] = observation.[ObservationType],
                    evidence.[Quality] = 'High',
                    evidence.[Completeness] = 'Complete',
                    evidence.[TimestampQuality] = 'Exact',
                    evidence.[SourceReliability] = 'Known',
                    evidence.[IsRelevant] = 1,
                    evidence.[RelevanceReason] = 'Internal technical observation for XAUUSD.'
                FROM [EvidenceRecords] evidence
                INNER JOIN [TechnicalObservations] observation ON observation.[Id] = evidence.[Id]
                INNER JOIN [Instruments] instrument ON instrument.[Id] = observation.[InstrumentId]
                INNER JOIN [Timeframes] timeframe ON timeframe.[Id] = observation.[TimeframeId];
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IdentityHash",
                table: "EvidenceRecords",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(64)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "EvidenceClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClusterType = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    DeterministicKey = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EventTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceClusters", x => x.Id);
                    table.CheckConstraint("CK_EvidenceClusters_Type", "[ClusterType] IN ('NewsEvent', 'EconomicEvent', 'MarketEvent', 'Other')");
                });

            migrationBuilder.CreateTable(
                name: "EvidenceQuarantineRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SourceType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SourceKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    PayloadHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OriginalTimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuarantinedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceQuarantineRecords", x => x.Id);
                    table.CheckConstraint("CK_EvidenceQuarantineRecords_MetadataJson", "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "EvidenceRelations",
                columns: table => new
                {
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceRelations", x => new { x.EvidenceId, x.RelatedEvidenceId, x.RelationType });
                    table.CheckConstraint("CK_EvidenceRelations_NotSelf", "[EvidenceId] <> [RelatedEvidenceId]");
                    table.CheckConstraint("CK_EvidenceRelations_Type", "[RelationType] IN ('Duplicate', 'Republished', 'SameEvent', 'Contradicts', 'Supports', 'Updates', 'References')");
                    table.ForeignKey(
                        name: "FK_EvidenceRelations_EvidenceRecords_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceRelations_EvidenceRecords_RelatedEvidenceId",
                        column: x => x.RelatedEvidenceId,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceClusterMembers",
                columns: table => new
                {
                    EvidenceClusterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceClusterMembers", x => new { x.EvidenceClusterId, x.EvidenceId });
                    table.ForeignKey(
                        name: "FK_EvidenceClusterMembers_EvidenceClusters_EvidenceClusterId",
                        column: x => x.EvidenceClusterId,
                        principalTable: "EvidenceClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceClusterMembers_EvidenceRecords_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_ContentHash",
                table: "EvidenceRecords",
                column: "ContentHash",
                filter: "[ContentHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_DataProviderId",
                table: "EvidenceRecords",
                column: "DataProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_Direction_AvailableAtUtc",
                table: "EvidenceRecords",
                columns: new[] { "Direction", "AvailableAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_Instrument_AvailableAtUtc",
                table: "EvidenceRecords",
                columns: new[] { "CanonicalSymbol", "AvailableAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_InstrumentId",
                table: "EvidenceRecords",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_Source_ExternalId",
                table: "EvidenceRecords",
                columns: new[] { "SourceKey", "ExternalId" },
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_SourceType_AvailableAtUtc",
                table: "EvidenceRecords",
                columns: new[] { "SourceType", "AvailableAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_Timeframe_AvailableAtUtc",
                table: "EvidenceRecords",
                columns: new[] { "TimeframeCode", "AvailableAtUtc" },
                filter: "[TimeframeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_TimeframeId",
                table: "EvidenceRecords",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_Type_AvailableAtUtc",
                table: "EvidenceRecords",
                columns: new[] { "EvidenceType", "AvailableAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_EvidenceRecords_IdentityHash",
                table: "EvidenceRecords",
                column: "IdentityHash",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_Completeness",
                table: "EvidenceRecords",
                sql: "[Completeness] IN ('Unknown', 'Partial', 'Complete')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_Direction",
                table: "EvidenceRecords",
                sql: "[Direction] IN ('Unknown', 'Bullish', 'Bearish', 'Neutral')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_EvidenceType",
                table: "EvidenceRecords",
                sql: "[EvidenceType] IN ('Market', 'Technical', 'Candle', 'CandleFlow', 'Liquidity', 'OrderFlow', 'Session', 'News', 'Economic', 'Analyst')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_Importance",
                table: "EvidenceRecords",
                sql: "[Importance] IN ('Unknown', 'Low', 'Medium', 'High', 'Critical')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_MetadataJson",
                table: "EvidenceRecords",
                sql: "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_Quality",
                table: "EvidenceRecords",
                sql: "[Quality] IN ('Unknown', 'Low', 'Medium', 'High')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_SourceReliability",
                table: "EvidenceRecords",
                sql: "[SourceReliability] IN ('Unknown', 'Known')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_SourceType",
                table: "EvidenceRecords",
                sql: "[SourceType] IN ('InternalMarketData', 'InternalTechnicalEngine', 'NewsProvider', 'EconomicProvider', 'AnalystProvider', 'Manual', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_TimestampQuality",
                table: "EvidenceRecords",
                sql: "[TimestampQuality] IN ('Unknown', 'Approximate', 'DateOnly', 'Exact')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EvidenceRecords_Validity",
                table: "EvidenceRecords",
                sql: "[ValidToUtc] IS NULL OR [ValidFromUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceClusterMembers_EvidenceId",
                table: "EvidenceClusterMembers",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceClusters_Type_EventTimeUtc",
                table: "EvidenceClusters",
                columns: new[] { "ClusterType", "EventTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_EvidenceClusters_Type_Key",
                table: "EvidenceClusters",
                columns: new[] { "ClusterType", "DeterministicKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceQuarantine_PayloadHash",
                table: "EvidenceQuarantineRecords",
                column: "PayloadHash");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceQuarantine_Source_Time",
                table: "EvidenceQuarantineRecords",
                columns: new[] { "SourceKey", "QuarantinedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRelations_Related_Type",
                table: "EvidenceRelations",
                columns: new[] { "RelatedEvidenceId", "RelationType" });

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceRecords_DataProviders_DataProviderId",
                table: "EvidenceRecords",
                column: "DataProviderId",
                principalTable: "DataProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceRecords_Instruments_InstrumentId",
                table: "EvidenceRecords",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceRecords_Timeframes_TimeframeId",
                table: "EvidenceRecords",
                column: "TimeframeId",
                principalTable: "Timeframes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceRecords_DataProviders_DataProviderId",
                table: "EvidenceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceRecords_Instruments_InstrumentId",
                table: "EvidenceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceRecords_Timeframes_TimeframeId",
                table: "EvidenceRecords");

            migrationBuilder.DropTable(
                name: "EvidenceClusterMembers");

            migrationBuilder.DropTable(
                name: "EvidenceQuarantineRecords");

            migrationBuilder.DropTable(
                name: "EvidenceRelations");

            migrationBuilder.DropTable(
                name: "EvidenceClusters");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_ContentHash",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_DataProviderId",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_Direction_AvailableAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_Instrument_AvailableAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_InstrumentId",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_Source_ExternalId",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_SourceType_AvailableAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_Timeframe_AvailableAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_TimeframeId",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceRecords_Type_AvailableAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropIndex(
                name: "UX_EvidenceRecords_IdentityHash",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_Completeness",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_Direction",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_EvidenceType",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_Importance",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_MetadataJson",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_Quality",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_SourceReliability",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_SourceType",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_TimestampQuality",
                table: "EvidenceRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EvidenceRecords_Validity",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "AvailableAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "CanonicalSymbol",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "CollectedAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Completeness",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "DataProviderId",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "EvidenceType",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "IdentityHash",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "InstrumentId",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "IsRelevant",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "NumericValue",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "OriginalCategory",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "OriginalDirection",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "OriginalImportance",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "OriginalSourceUrl",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "OriginalSymbol",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "OriginalUnit",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "OriginalValue",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Quality",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "RelevanceReason",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "SourceReliability",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "TimeframeCode",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "TimeframeId",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "TimestampQuality",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "ValidFromUtc",
                table: "EvidenceRecords");

            migrationBuilder.DropColumn(
                name: "ValidToUtc",
                table: "EvidenceRecords");
        }
    }
}
