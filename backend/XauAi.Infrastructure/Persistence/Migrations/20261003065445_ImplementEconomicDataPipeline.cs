using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementEconomicDataPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EconomicSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalSeriesId = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Units = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SeasonalAdjustment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObservationStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ObservationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProviderUpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomicSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EconomicSeries_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EconomicObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EconomicSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    OriginalValue = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    RealtimeStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RealtimeEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomicObservations", x => x.Id);
                    table.CheckConstraint("CK_EconomicObservations_StatusValue", "([Status] = 'Missing' AND [Value] IS NULL) OR ([Status] = 'Available' AND [Value] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_EconomicObservations_EconomicSeries_EconomicSeriesId",
                        column: x => x.EconomicSeriesId,
                        principalTable: "EconomicSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EconomicObservations_EvidenceRecords_Id",
                        column: x => x.Id,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EconomicSyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EconomicSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    RequestedFromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RequestsMade = table.Column<int>(type: "int", nullable: false),
                    RateLimitResponses = table.Column<int>(type: "int", nullable: false),
                    RecordsReceived = table.Column<int>(type: "int", nullable: false),
                    RecordsInserted = table.Column<int>(type: "int", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomicSyncRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EconomicSyncRuns_EconomicSeries_EconomicSeriesId",
                        column: x => x.EconomicSeriesId,
                        principalTable: "EconomicSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EconomicSyncStates",
                columns: table => new
                {
                    EconomicSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastSuccessfulSyncAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastObservationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    RequestsMade = table.Column<int>(type: "int", nullable: false),
                    RateLimitResponses = table.Column<int>(type: "int", nullable: false),
                    RecordsReceived = table.Column<int>(type: "int", nullable: false),
                    RecordsInserted = table.Column<int>(type: "int", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    LastErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomicSyncStates", x => x.EconomicSeriesId);
                    table.ForeignKey(
                        name: "FK_EconomicSyncStates_EconomicSeries_EconomicSeriesId",
                        column: x => x.EconomicSeriesId,
                        principalTable: "EconomicSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EconomicObservationRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EconomicObservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    OriginalValue = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    RealtimeStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RealtimeEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    SupersededAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomicObservationRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EconomicObservationRevisions_EconomicObservations_EconomicObservationId",
                        column: x => x.EconomicObservationId,
                        principalTable: "EconomicObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DataProviders",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "Name", "ProviderType", "UpdatedAtUtc" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "fred", "Federal Reserve Economic Data (FRED)", "Economic", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_EconomicObservationRevisions_Observation_SupersededAtUtc",
                table: "EconomicObservationRevisions",
                columns: new[] { "EconomicObservationId", "SupersededAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_EconomicObservations_Series_ObservationDate",
                table: "EconomicObservations",
                columns: new[] { "EconomicSeriesId", "ObservationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EconomicSeries_Category_Active",
                table: "EconomicSeries",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomicSeries_Frequency_Active",
                table: "EconomicSeries",
                columns: new[] { "Frequency", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_EconomicSeries_Provider_ExternalSeriesId",
                table: "EconomicSeries",
                columns: new[] { "DataProviderId", "ExternalSeriesId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EconomicSyncRuns_Series_StartedAtUtc",
                table: "EconomicSyncRuns",
                columns: new[] { "EconomicSeriesId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomicSyncRuns_Status_StartedAtUtc",
                table: "EconomicSyncRuns",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomicSyncStates_LastSuccessful",
                table: "EconomicSyncStates",
                column: "LastSuccessfulSyncAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EconomicObservationRevisions");

            migrationBuilder.DropTable(
                name: "EconomicSyncRuns");

            migrationBuilder.DropTable(
                name: "EconomicSyncStates");

            migrationBuilder.DropTable(
                name: "EconomicObservations");

            migrationBuilder.DropTable(
                name: "EconomicSeries");

            migrationBuilder.DeleteData(
                table: "DataProviders",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"));
        }
    }
}
