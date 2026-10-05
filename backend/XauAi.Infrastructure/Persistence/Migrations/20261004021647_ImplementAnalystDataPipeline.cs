using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAnalystDataPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_Instrument_PublishedAtUtc",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "UX_AnalystStatements_Provider_ExternalId",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "UX_AnalystStatements_SourceUrlHash",
                table: "AnalystStatements");

            migrationBuilder.AddColumn<Guid>(
                name: "AnalystId",
                table: "AnalystStatements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AnalystSourceId",
                table: "AnalystStatements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetClass",
                table: "AnalystStatements",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "AnalystStatements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "ClaimHash",
                table: "AnalystStatements",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Confidence",
                table: "AnalystStatements",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "AnalystStatements",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "HorizonUnit",
                table: "AnalystStatements",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<int>(
                name: "HorizonValue",
                table: "AnalystStatements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstrumentCode",
                table: "AnalystStatements",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "AnalystStatements",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "und");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicationId",
                table: "AnalystStatements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AnalystStatements",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AnalystStatements",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<string>(
                name: "TargetCurrency",
                table: "AnalystStatements",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetRangeHigh",
                table: "AnalystStatements",
                type: "decimal(19,8)",
                precision: 19,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetRangeLow",
                table: "AnalystStatements",
                type: "decimal(19,8)",
                precision: 19,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "AnalystStatements",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "AnalystSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    IdentityHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Website = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    CountryCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalystSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalystSources_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalystSyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    RequestedFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RequestedToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RequestsMade = table.Column<int>(type: "int", nullable: false),
                    RateLimitResponses = table.Column<int>(type: "int", nullable: false),
                    ItemsReceived = table.Column<int>(type: "int", nullable: false),
                    PublicationsInserted = table.Column<int>(type: "int", nullable: false),
                    PredictionsInserted = table.Column<int>(type: "int", nullable: false),
                    ItemsSkipped = table.Column<int>(type: "int", nullable: false),
                    ItemsRejected = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalystSyncRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalystSyncRuns_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalystSyncStates",
                columns: table => new
                {
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastSuccessfulSyncAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastPublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    RequestsMade = table.Column<int>(type: "int", nullable: false),
                    RateLimitResponses = table.Column<int>(type: "int", nullable: false),
                    ItemsReceived = table.Column<int>(type: "int", nullable: false),
                    PublicationsInserted = table.Column<int>(type: "int", nullable: false),
                    PredictionsInserted = table.Column<int>(type: "int", nullable: false),
                    ItemsSkipped = table.Column<int>(type: "int", nullable: false),
                    ItemsRejected = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    LastErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalystSyncStates", x => x.DataProviderId);
                    table.ForeignKey(
                        name: "FK_AnalystSyncStates_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalystPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalystSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalPublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedPublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    IdentityHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    SourceUrlHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    ContentHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelationshipType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CollectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ProviderUpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalystPublications", x => x.Id);
                    table.CheckConstraint("CK_AnalystPublications_RelationshipType", "[RelationshipType] IN ('Original', 'Revision', 'Republished', 'Related')");
                    table.CheckConstraint("CK_AnalystPublications_Version", "[Version] >= 1");
                    table.ForeignKey(
                        name: "FK_AnalystPublications_AnalystPublications_OriginalPublicationId",
                        column: x => x.OriginalPublicationId,
                        principalTable: "AnalystPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalystPublications_AnalystPublications_RelatedPublicationId",
                        column: x => x.RelatedPublicationId,
                        principalTable: "AnalystPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalystPublications_AnalystSources_AnalystSourceId",
                        column: x => x.AnalystSourceId,
                        principalTable: "AnalystSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalystPublications_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Analysts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalystSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    IdentityHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProfileUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analysts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Analysts_AnalystSources_AnalystSourceId",
                        column: x => x.AnalystSourceId,
                        principalTable: "AnalystSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DataProviders",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "Name", "ProviderType", "UpdatedAtUtc" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "analyst-rss", "Configured Analyst RSS/Atom", "Analyst", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.Sql("""
                UPDATE statements
                SET statements.InstrumentCode = COALESCE(instruments.Symbol, 'Unknown'),
                    statements.AssetClass = COALESCE(instruments.AssetClass, 'Other'),
                    statements.Category = 'Other',
                    statements.HorizonUnit = 'Unknown',
                    statements.Language = 'und',
                    statements.Status = 'Published',
                    statements.CreatedAtUtc = statements.FetchedAtUtc,
                    statements.UpdatedAtUtc = statements.FetchedAtUtc,
                    statements.Direction = CASE
                        WHEN statements.Direction IN ('Bullish', 'Bearish', 'Neutral', 'Unknown') THEN statements.Direction
                        ELSE 'Unknown'
                    END
                FROM AnalystStatements AS statements
                LEFT JOIN Instruments AS instruments ON instruments.Id = statements.InstrumentId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_Analyst_PublishedAtUtc",
                table: "AnalystStatements",
                columns: new[] { "AnalystId", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_DataProviderId",
                table: "AnalystStatements",
                column: "DataProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_Direction_PublishedAtUtc",
                table: "AnalystStatements",
                columns: new[] { "Direction", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_Instrument_PublishedAtUtc",
                table: "AnalystStatements",
                columns: new[] { "InstrumentCode", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_InstrumentId",
                table: "AnalystStatements",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_Source_PublishedAtUtc",
                table: "AnalystStatements",
                columns: new[] { "AnalystSourceId", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystStatements_SourceUrlHash",
                table: "AnalystStatements",
                column: "SourceUrlHash",
                filter: "[SourceUrlHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AnalystStatements_Publication_ClaimHash",
                table: "AnalystStatements",
                columns: new[] { "PublicationId", "ClaimHash" },
                unique: true,
                filter: "[PublicationId] IS NOT NULL AND [ClaimHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AnalystStatements_Publication_ExternalId",
                table: "AnalystStatements",
                columns: new[] { "PublicationId", "ExternalId" },
                unique: true,
                filter: "[PublicationId] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnalystStatements_Confidence",
                table: "AnalystStatements",
                sql: "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 100)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnalystStatements_Direction",
                table: "AnalystStatements",
                sql: "[Direction] IS NULL OR [Direction] IN ('Unknown', 'Bullish', 'Bearish', 'Neutral')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnalystStatements_HorizonUnit",
                table: "AnalystStatements",
                sql: "[HorizonUnit] IN ('Unknown', 'Intraday', 'Days', 'Weeks', 'Months', 'Years', 'LongTerm')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnalystStatements_TargetRange",
                table: "AnalystStatements",
                sql: "[TargetRangeLow] IS NULL OR [TargetRangeHigh] IS NULL OR [TargetRangeLow] <= [TargetRangeHigh]");

            migrationBuilder.CreateIndex(
                name: "IX_AnalystPublications_ContentHash",
                table: "AnalystPublications",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_AnalystPublications_OriginalPublicationId",
                table: "AnalystPublications",
                column: "OriginalPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalystPublications_Provider_PublishedAtUtc",
                table: "AnalystPublications",
                columns: new[] { "DataProviderId", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystPublications_RelatedPublicationId",
                table: "AnalystPublications",
                column: "RelatedPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalystPublications_Source_PublishedAtUtc",
                table: "AnalystPublications",
                columns: new[] { "AnalystSourceId", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystPublications_SourceUrlHash",
                table: "AnalystPublications",
                column: "SourceUrlHash",
                filter: "[SourceUrlHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AnalystPublications_Provider_Identity_Version",
                table: "AnalystPublications",
                columns: new[] { "DataProviderId", "IdentityHash", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Analysts_Source_Active",
                table: "Analysts",
                columns: new[] { "AnalystSourceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_Analysts_Source_IdentityHash",
                table: "Analysts",
                columns: new[] { "AnalystSourceId", "IdentityHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalystSources_Type_Active",
                table: "AnalystSources",
                columns: new[] { "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_AnalystSources_Provider_IdentityHash",
                table: "AnalystSources",
                columns: new[] { "DataProviderId", "IdentityHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalystSyncRuns_Provider_StartedAtUtc",
                table: "AnalystSyncRuns",
                columns: new[] { "DataProviderId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystSyncRuns_Status_StartedAtUtc",
                table: "AnalystSyncRuns",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystSyncStates_LastSuccessful",
                table: "AnalystSyncStates",
                column: "LastSuccessfulSyncAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalystStatements_AnalystPublications_PublicationId",
                table: "AnalystStatements",
                column: "PublicationId",
                principalTable: "AnalystPublications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalystStatements_AnalystSources_AnalystSourceId",
                table: "AnalystStatements",
                column: "AnalystSourceId",
                principalTable: "AnalystSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalystStatements_Analysts_AnalystId",
                table: "AnalystStatements",
                column: "AnalystId",
                principalTable: "Analysts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalystStatements_AnalystPublications_PublicationId",
                table: "AnalystStatements");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalystStatements_AnalystSources_AnalystSourceId",
                table: "AnalystStatements");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalystStatements_Analysts_AnalystId",
                table: "AnalystStatements");

            migrationBuilder.DropTable(
                name: "AnalystPublications");

            migrationBuilder.DropTable(
                name: "Analysts");

            migrationBuilder.DropTable(
                name: "AnalystSyncRuns");

            migrationBuilder.DropTable(
                name: "AnalystSyncStates");

            migrationBuilder.DropTable(
                name: "AnalystSources");

            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_Analyst_PublishedAtUtc",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_DataProviderId",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_Direction_PublishedAtUtc",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_Instrument_PublishedAtUtc",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_InstrumentId",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_Source_PublishedAtUtc",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "IX_AnalystStatements_SourceUrlHash",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "UX_AnalystStatements_Publication_ClaimHash",
                table: "AnalystStatements");

            migrationBuilder.DropIndex(
                name: "UX_AnalystStatements_Publication_ExternalId",
                table: "AnalystStatements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnalystStatements_Confidence",
                table: "AnalystStatements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnalystStatements_Direction",
                table: "AnalystStatements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnalystStatements_HorizonUnit",
                table: "AnalystStatements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnalystStatements_TargetRange",
                table: "AnalystStatements");

            migrationBuilder.DeleteData(
                table: "DataProviders",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000006"));

            migrationBuilder.DropColumn(
                name: "AnalystId",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "AnalystSourceId",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "AssetClass",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "ClaimHash",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "HorizonUnit",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "HorizonValue",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "InstrumentCode",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "PublicationId",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "TargetCurrency",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "TargetRangeHigh",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "TargetRangeLow",
                table: "AnalystStatements");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AnalystStatements");

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
        }
    }
}
