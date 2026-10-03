using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Kind = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Symbol = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AssetClass = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    BaseAsset = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    QuoteAsset = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    PriceScale = table.Column<short>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                    table.CheckConstraint("CK_Instruments_PriceScale", "[PriceScale] BETWEEN 0 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "Strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timeframes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Code = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timeframes", x => x.Id);
                    table.CheckConstraint("CK_Timeframes_Duration", "[DurationSeconds] > 0");
                });

            migrationBuilder.CreateTable(
                name: "EconomicEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CountryCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Importance = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    PreviousValue = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    ForecastValue = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    ActualValue = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    ValueUnit = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    RawValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomicEvents", x => x.Id);
                    table.CheckConstraint("CK_EconomicEvents_RawValuesJson", "[RawValuesJson] IS NULL OR ISJSON([RawValuesJson]) = 1");
                    table.ForeignKey(
                        name: "FK_EconomicEvents_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EconomicEvents_EvidenceRecords_Id",
                        column: x => x.Id,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnalysisType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptIdentifier = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    PromptVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    AnalysisVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ApplicationVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    InputDigest = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    Output = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    CostUsd = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    LatencyMilliseconds = table.Column<int>(type: "int", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalyses", x => x.Id);
                    table.CheckConstraint("CK_AiAnalyses_Cost", "[CostUsd] IS NULL OR [CostUsd] >= 0");
                    table.CheckConstraint("CK_AiAnalyses_Latency", "[LatencyMilliseconds] IS NULL OR [LatencyMilliseconds] >= 0");
                    table.CheckConstraint("CK_AiAnalyses_Tokens", "([InputTokens] IS NULL OR [InputTokens] >= 0) AND ([OutputTokens] IS NULL OR [OutputTokens] >= 0)");
                    table.ForeignKey(
                        name: "FK_AiAnalyses_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalystStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    AnalystName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PermittedContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    SourceUrlHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    Direction = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    TargetPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    TimeHorizon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalystStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalystStatements_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalystStatements_EvidenceRecords_Id",
                        column: x => x.Id,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalystStatements_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Author = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    CanonicalUrlHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    ContentHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    Language = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SourceCategory = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsArticles_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsArticles_EvidenceRecords_Id",
                        column: x => x.Id,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsArticles_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    StrategyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinitionHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RetiredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyVersions", x => x.Id);
                    table.CheckConstraint("CK_StrategyVersions_DefinitionJson", "ISJSON([DefinitionJson]) = 1");
                    table.ForeignKey(
                        name: "FK_StrategyVersions_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketCandles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderSymbol = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    OpenTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CloseTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    TickVolume = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    RealVolume = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    Spread = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    SourceTimeZone = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketCandles", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.CheckConstraint("CK_MarketCandles_NonNegativeMeasurements", "([TickVolume] IS NULL OR [TickVolume] >= 0) AND ([RealVolume] IS NULL OR [RealVolume] >= 0) AND ([Spread] IS NULL OR [Spread] >= 0)");
                    table.CheckConstraint("CK_MarketCandles_Prices", "[High] >= [Low] AND [Open] BETWEEN [Low] AND [High] AND [Close] BETWEEN [Low] AND [High]");
                    table.CheckConstraint("CK_MarketCandles_TimeRange", "[CloseTimeUtc] > [OpenTimeUtc]");
                    table.ForeignKey(
                        name: "FK_MarketCandles_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketCandles_EvidenceRecords_Id",
                        column: x => x.Id,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketCandles_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketCandles_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservationType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CalculationVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ParametersHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    ValueText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalObservations", x => x.Id);
                    table.CheckConstraint("CK_TechnicalObservations_ParametersJson", "ISJSON([ParametersJson]) = 1");
                    table.CheckConstraint("CK_TechnicalObservations_ValuesJson", "[ValuesJson] IS NULL OR ISJSON([ValuesJson]) = 1");
                    table.ForeignKey(
                        name: "FK_TechnicalObservations_EvidenceRecords_Id",
                        column: x => x.Id,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalObservations_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalObservations_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiAnalysisEvidence",
                columns: table => new
                {
                    AiAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalysisEvidence", x => new { x.AiAnalysisId, x.EvidenceRecordId });
                    table.ForeignKey(
                        name: "FK_AiAnalysisEvidence_AiAnalyses_AiAnalysisId",
                        column: x => x.AiAnalysisId,
                        principalTable: "AiAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiAnalysisEvidence_EvidenceRecords_EvidenceRecordId",
                        column: x => x.EvidenceRecordId,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticleContents",
                columns: table => new
                {
                    NewsArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermittedContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StoragePermission = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PermissionCheckedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticleContents", x => x.NewsArticleId);
                    table.ForeignKey(
                        name: "FK_NewsArticleContents_NewsArticles_NewsArticleId",
                        column: x => x.NewsArticleId,
                        principalTable: "NewsArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BacktestRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    StrategyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EngineVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParametersHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    RangeStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RangeEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    StartingCapital = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    TradeCount = table.Column<int>(type: "int", nullable: true),
                    WinningTrades = table.Column<int>(type: "int", nullable: true),
                    LosingTrades = table.Column<int>(type: "int", nullable: true),
                    GrossProfit = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    GrossLoss = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    MaximumDrawdown = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    WinRate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Expectancy = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    ResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestRuns", x => x.Id);
                    table.CheckConstraint("CK_BacktestRuns_DateRange", "[RangeEndUtc] > [RangeStartUtc]");
                    table.CheckConstraint("CK_BacktestRuns_ParametersJson", "ISJSON([ParametersJson]) = 1");
                    table.CheckConstraint("CK_BacktestRuns_ResultsJson", "[ResultsJson] IS NULL OR ISJSON([ResultsJson]) = 1");
                    table.CheckConstraint("CK_BacktestRuns_StartingCapital", "[StartingCapital] >= 0");
                    table.CheckConstraint("CK_BacktestRuns_TradeCounts", "([TradeCount] IS NULL OR [TradeCount] >= 0) AND ([WinningTrades] IS NULL OR [WinningTrades] >= 0) AND ([LosingTrades] IS NULL OR [LosingTrades] >= 0)");
                    table.ForeignKey(
                        name: "FK_BacktestRuns_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BacktestRuns_StrategyVersions_StrategyVersionId",
                        column: x => x.StrategyVersionId,
                        principalTable: "StrategyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BacktestRuns_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    StrategyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Result = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyEvaluations", x => x.Id);
                    table.CheckConstraint("CK_StrategyEvaluations_DetailsJson", "[DetailsJson] IS NULL OR ISJSON([DetailsJson]) = 1");
                    table.CheckConstraint("CK_StrategyEvaluations_Score", "[Score] IS NULL OR [Score] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_StrategyEvaluations_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyEvaluations_StrategyVersions_StrategyVersionId",
                        column: x => x.StrategyVersionId,
                        principalTable: "StrategyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyEvaluations_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BacktestTrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BacktestRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    EnteredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ExitedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    EntryPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    ExitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    ProfitLossAmount = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    ProfitLossR = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    AuditJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestTrades", x => x.Id);
                    table.CheckConstraint("CK_BacktestTrades_AuditJson", "[AuditJson] IS NULL OR ISJSON([AuditJson]) = 1");
                    table.ForeignKey(
                        name: "FK_BacktestTrades_BacktestRuns_BacktestRunId",
                        column: x => x.BacktestRunId,
                        principalTable: "BacktestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    StrategyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BacktestRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Scope = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    MarketRegime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RangeStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RangeEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    SampleSize = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    LossRate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Expectancy = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    ProfitFactor = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    MaximumDrawdown = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    AverageR = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    AverageFavorableExcursion = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    AverageAdverseExcursion = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    CalculationVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceStatistics", x => x.Id);
                    table.CheckConstraint("CK_PerformanceStatistics_DateRange", "[RangeEndUtc] > [RangeStartUtc]");
                    table.CheckConstraint("CK_PerformanceStatistics_LossRate", "[LossRate] IS NULL OR [LossRate] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_PerformanceStatistics_SampleSize", "[SampleSize] >= 0");
                    table.CheckConstraint("CK_PerformanceStatistics_WinRate", "[WinRate] IS NULL OR [WinRate] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_PerformanceStatistics_BacktestRuns_BacktestRunId",
                        column: x => x.BacktestRunId,
                        principalTable: "BacktestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceStatistics_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceStatistics_StrategyVersions_StrategyVersionId",
                        column: x => x.StrategyVersionId,
                        principalTable: "StrategyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceStatistics_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyEvaluationEvidence",
                columns: table => new
                {
                    StrategyEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyEvaluationEvidence", x => new { x.StrategyEvaluationId, x.EvidenceRecordId });
                    table.ForeignKey(
                        name: "FK_StrategyEvaluationEvidence_EvidenceRecords_EvidenceRecordId",
                        column: x => x.EvidenceRecordId,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyEvaluationEvidence_StrategyEvaluations_StrategyEvaluationId",
                        column: x => x.StrategyEvaluationId,
                        principalTable: "StrategyEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradingSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StrategyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrategyEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AiAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Direction = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    EntryPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    StopLossPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    TakeProfitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    TimeHorizonSeconds = table.Column<int>(type: "int", nullable: true),
                    ModelConfidence = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    RiskConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SignalAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingSignals", x => x.Id);
                    table.CheckConstraint("CK_TradingSignals_ModelConfidence", "[ModelConfidence] IS NULL OR [ModelConfidence] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_TradingSignals_RiskConditionsJson", "[RiskConditionsJson] IS NULL OR ISJSON([RiskConditionsJson]) = 1");
                    table.CheckConstraint("CK_TradingSignals_TimeHorizon", "[TimeHorizonSeconds] IS NULL OR [TimeHorizonSeconds] > 0");
                    table.ForeignKey(
                        name: "FK_TradingSignals_AiAnalyses_AiAnalysisId",
                        column: x => x.AiAnalysisId,
                        principalTable: "AiAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSignals_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSignals_StrategyEvaluations_StrategyEvaluationId",
                        column: x => x.StrategyEvaluationId,
                        principalTable: "StrategyEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSignals_StrategyVersions_StrategyVersionId",
                        column: x => x.StrategyVersionId,
                        principalTable: "StrategyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSignals_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignalOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TradingSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutcomeStatus = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ExitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ProfitLossAmount = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    ProfitLossR = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    MaximumFavorableExcursion = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    MaximumAdverseExcursion = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    MeasurementDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MeasuredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalOutcomes", x => x.Id);
                    table.CheckConstraint("CK_SignalOutcomes_MeasurementDetailsJson", "[MeasurementDetailsJson] IS NULL OR ISJSON([MeasurementDetailsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_SignalOutcomes_TradingSignals_TradingSignalId",
                        column: x => x.TradingSignalId,
                        principalTable: "TradingSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradingSignalEvidence",
                columns: table => new
                {
                    TradingSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingSignalEvidence", x => new { x.TradingSignalId, x.EvidenceRecordId });
                    table.ForeignKey(
                        name: "FK_TradingSignalEvidence_EvidenceRecords_EvidenceRecordId",
                        column: x => x.EvidenceRecordId,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSignalEvidence_TradingSignals_TradingSignalId",
                        column: x => x.TradingSignalId,
                        principalTable: "TradingSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Instruments",
                columns: new[] { "Id", "AssetClass", "BaseAsset", "CreatedAtUtc", "DisplayName", "IsActive", "PriceScale", "QuoteAsset", "Symbol", "UpdatedAtUtc" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000001"), "Commodity", "XAU", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gold / US Dollar", true, (short)8, "USD", "XAUUSD", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "Timeframes",
                columns: new[] { "Id", "Code", "DurationSeconds", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "M1", 60, true, "1 minute", 1 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "M5", 300, true, "5 minutes", 2 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "M15", 900, true, "15 minutes", 3 },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "M30", 1800, true, "30 minutes", 4 },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "H1", 3600, true, "1 hour", 5 },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "H4", 14400, true, "4 hours", 6 },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "D1", 86400, true, "1 day", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_InstrumentId",
                table: "AiAnalyses",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_Model_PromptVersion",
                table: "AiAnalyses",
                columns: new[] { "Provider", "Model", "ModelVersion", "PromptIdentifier", "PromptVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_Status_CreatedAtUtc",
                table: "AiAnalyses",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_Type_CreatedAtUtc",
                table: "AiAnalyses",
                columns: new[] { "AnalysisType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisEvidence_EvidenceRecordId",
                table: "AiAnalysisEvidence",
                column: "EvidenceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_Instrument_PublishedAtUtc",
                table: "AnalystStatements",
                columns: new[] { "InstrumentId", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_AnalystStatements_Provider_ExternalId",
                table: "AnalystStatements",
                columns: new[] { "DataProviderId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AnalystStatements_SourceUrlHash",
                table: "AnalystStatements",
                column: "SourceUrlHash",
                unique: true,
                filter: "[SourceUrlHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_Market_DateRange",
                table: "BacktestRuns",
                columns: new[] { "InstrumentId", "TimeframeId", "RangeStartUtc", "RangeEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_Reproducibility",
                table: "BacktestRuns",
                columns: new[] { "StrategyVersionId", "InstrumentId", "TimeframeId", "RangeStartUtc", "RangeEndUtc", "ParametersHash", "EngineVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_StrategyVersion_StartedAtUtc",
                table: "BacktestRuns",
                columns: new[] { "StrategyVersionId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_TimeframeId",
                table: "BacktestRuns",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestTrades_Run_EnteredAtUtc",
                table: "BacktestTrades",
                columns: new[] { "BacktestRunId", "EnteredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_BacktestTrades_Run_Sequence",
                table: "BacktestTrades",
                columns: new[] { "BacktestRunId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataProviders_Type_Active",
                table: "DataProviders",
                columns: new[] { "ProviderType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_DataProviders_Key",
                table: "DataProviders",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EconomicEvents_Currency_ScheduledAtUtc",
                table: "EconomicEvents",
                columns: new[] { "CurrencyCode", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomicEvents_ScheduledAtUtc",
                table: "EconomicEvents",
                column: "ScheduledAtUtc");

            migrationBuilder.CreateIndex(
                name: "UX_EconomicEvents_Provider_ExternalId",
                table: "EconomicEvents",
                columns: new[] { "DataProviderId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRecords_Kind_ObservedAtUtc",
                table: "EvidenceRecords",
                columns: new[] { "Kind", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Instruments_Symbol",
                table: "Instruments",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketCandles_Provider_Instrument_Timeframe_OpenTime",
                table: "MarketCandles",
                columns: new[] { "DataProviderId", "InstrumentId", "TimeframeId", "OpenTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketCandles_TimeframeId",
                table: "MarketCandles",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "UX_MarketCandles_Instrument_Timeframe_OpenTime_Provider",
                table: "MarketCandles",
                columns: new[] { "InstrumentId", "TimeframeId", "OpenTimeUtc", "DataProviderId" },
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_ContentHash",
                table: "NewsArticles",
                column: "ContentHash",
                filter: "[ContentHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Instrument_PublishedAtUtc",
                table: "NewsArticles",
                columns: new[] { "InstrumentId", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_PublishedAtUtc",
                table: "NewsArticles",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "UX_NewsArticles_CanonicalUrlHash",
                table: "NewsArticles",
                column: "CanonicalUrlHash",
                unique: true,
                filter: "[CanonicalUrlHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_NewsArticles_Provider_ExternalId",
                table: "NewsArticles",
                columns: new[] { "DataProviderId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceStatistics_BacktestRunId",
                table: "PerformanceStatistics",
                column: "BacktestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceStatistics_Dimensions_CalculatedAtUtc",
                table: "PerformanceStatistics",
                columns: new[] { "InstrumentId", "TimeframeId", "MarketRegime", "CalculatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceStatistics_StrategyVersion_CalculatedAtUtc",
                table: "PerformanceStatistics",
                columns: new[] { "StrategyVersionId", "CalculatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceStatistics_TimeframeId",
                table: "PerformanceStatistics",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutcomes_Status_MeasuredAtUtc",
                table: "SignalOutcomes",
                columns: new[] { "OutcomeStatus", "MeasuredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SignalOutcomes_TradingSignalId",
                table: "SignalOutcomes",
                column: "TradingSignalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_Category",
                table: "Strategies",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "UX_Strategies_Key",
                table: "Strategies",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategyEvaluationEvidence_EvidenceRecordId",
                table: "StrategyEvaluationEvidence",
                column: "EvidenceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyEvaluations_Instrument_Timeframe_EvaluatedAtUtc",
                table: "StrategyEvaluations",
                columns: new[] { "InstrumentId", "TimeframeId", "EvaluatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategyEvaluations_StrategyVersion_EvaluatedAtUtc",
                table: "StrategyEvaluations",
                columns: new[] { "StrategyVersionId", "EvaluatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategyEvaluations_TimeframeId",
                table: "StrategyEvaluations",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyVersions_DefinitionHash",
                table: "StrategyVersions",
                column: "DefinitionHash");

            migrationBuilder.CreateIndex(
                name: "UX_StrategyVersions_Strategy_Version",
                table: "StrategyVersions",
                columns: new[] { "StrategyId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalObservations_Instrument_Timeframe_Time",
                table: "TechnicalObservations",
                columns: new[] { "InstrumentId", "TimeframeId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalObservations_TimeframeId",
                table: "TechnicalObservations",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "UX_TechnicalObservations_Identity",
                table: "TechnicalObservations",
                columns: new[] { "InstrumentId", "TimeframeId", "Name", "CalculationVersion", "ObservedAtUtc", "ParametersHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Timeframes_Code",
                table: "Timeframes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Timeframes_Duration",
                table: "Timeframes",
                column: "DurationSeconds",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignalEvidence_EvidenceRecordId",
                table: "TradingSignalEvidence",
                column: "EvidenceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignals_AiAnalysisId",
                table: "TradingSignals",
                column: "AiAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignals_Instrument_SignalAtUtc",
                table: "TradingSignals",
                columns: new[] { "InstrumentId", "SignalAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignals_Status_SignalAtUtc",
                table: "TradingSignals",
                columns: new[] { "Status", "SignalAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignals_StrategyEvaluationId",
                table: "TradingSignals",
                column: "StrategyEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignals_StrategyVersion_SignalAtUtc",
                table: "TradingSignals",
                columns: new[] { "StrategyVersionId", "SignalAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignals_TimeframeId",
                table: "TradingSignals",
                column: "TimeframeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAnalysisEvidence");

            migrationBuilder.DropTable(
                name: "AnalystStatements");

            migrationBuilder.DropTable(
                name: "BacktestTrades");

            migrationBuilder.DropTable(
                name: "EconomicEvents");

            migrationBuilder.DropTable(
                name: "MarketCandles");

            migrationBuilder.DropTable(
                name: "NewsArticleContents");

            migrationBuilder.DropTable(
                name: "PerformanceStatistics");

            migrationBuilder.DropTable(
                name: "SignalOutcomes");

            migrationBuilder.DropTable(
                name: "StrategyEvaluationEvidence");

            migrationBuilder.DropTable(
                name: "TechnicalObservations");

            migrationBuilder.DropTable(
                name: "TradingSignalEvidence");

            migrationBuilder.DropTable(
                name: "NewsArticles");

            migrationBuilder.DropTable(
                name: "BacktestRuns");

            migrationBuilder.DropTable(
                name: "TradingSignals");

            migrationBuilder.DropTable(
                name: "DataProviders");

            migrationBuilder.DropTable(
                name: "EvidenceRecords");

            migrationBuilder.DropTable(
                name: "AiAnalyses");

            migrationBuilder.DropTable(
                name: "StrategyEvaluations");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropTable(
                name: "StrategyVersions");

            migrationBuilder.DropTable(
                name: "Timeframes");

            migrationBuilder.DropTable(
                name: "Strategies");
        }
    }
}
