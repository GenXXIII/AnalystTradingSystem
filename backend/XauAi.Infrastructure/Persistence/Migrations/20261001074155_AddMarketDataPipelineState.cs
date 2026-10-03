using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketDataPipelineState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketDataSyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RequestedFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RequestedToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    BatchesProcessed = table.Column<int>(type: "int", nullable: false),
                    RecordsReceived = table.Column<int>(type: "int", nullable: false),
                    RecordsAccepted = table.Column<int>(type: "int", nullable: false),
                    RecordsInserted = table.Column<int>(type: "int", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    RecordsRejected = table.Column<int>(type: "int", nullable: false),
                    DetectedGapCount = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataSyncRuns", x => x.Id);
                    table.CheckConstraint("CK_MarketDataSyncRuns_Counts", "[BatchesProcessed] >= 0 AND [RecordsReceived] >= 0 AND [RecordsAccepted] >= 0 AND [RecordsInserted] >= 0 AND [RecordsUpdated] >= 0 AND [RecordsSkipped] >= 0 AND [RecordsRejected] >= 0 AND [DetectedGapCount] >= 0 AND [DurationMilliseconds] >= 0");
                    table.CheckConstraint("CK_MarketDataSyncRuns_TimeRange", "[RequestedToUtc] > [RequestedFromUtc]");
                    table.ForeignKey(
                        name: "FK_MarketDataSyncRuns_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketDataSyncRuns_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketDataSyncRuns_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketDataSyncStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastSuccessfulSyncAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastRequestedFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastRequestedToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastStoredCandleOpenTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    DetectedGapCount = table.Column<int>(type: "int", nullable: false),
                    LastErrorCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataSyncStates", x => x.Id);
                    table.CheckConstraint("CK_MarketDataSyncStates_Failures", "[ConsecutiveFailures] >= 0");
                    table.CheckConstraint("CK_MarketDataSyncStates_Gaps", "[DetectedGapCount] >= 0");
                    table.ForeignKey(
                        name: "FK_MarketDataSyncStates_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketDataSyncStates_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketDataSyncStates_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataSyncRuns_DataProviderId",
                table: "MarketDataSyncRuns",
                column: "DataProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataSyncRuns_Instrument_Timeframe_StartedAtUtc",
                table: "MarketDataSyncRuns",
                columns: new[] { "InstrumentId", "TimeframeId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataSyncRuns_Status_StartedAtUtc",
                table: "MarketDataSyncRuns",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataSyncRuns_TimeframeId",
                table: "MarketDataSyncRuns",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataSyncStates_DataProviderId",
                table: "MarketDataSyncStates",
                column: "DataProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataSyncStates_TimeframeId",
                table: "MarketDataSyncStates",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "UX_MarketDataSyncStates_Instrument_Timeframe_Provider",
                table: "MarketDataSyncStates",
                columns: new[] { "InstrumentId", "TimeframeId", "DataProviderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketDataSyncRuns");

            migrationBuilder.DropTable(
                name: "MarketDataSyncStates");
        }
    }
}
