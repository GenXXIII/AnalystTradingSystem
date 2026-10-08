using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementFullAnalystWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FullAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Timeframe = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    AnalysisTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    Decision = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Agreement = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    ConflictsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyEvidenceIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    InvalidationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uncertainty = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    FutureAvailable = table.Column<bool>(type: "bit", nullable: false),
                    MasterResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PromptVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ConfigurationVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SnapshotHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkspaceResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FullAnalyses", x => x.Id);
                    table.CheckConstraint("CK_FullAnalyses_Agreement", "[Agreement] IS NULL OR [Agreement] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_FullAnalyses_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_FullAnalyses_CurrentPrice", "[CurrentPrice] IS NULL OR [CurrentPrice] > 0");
                    table.CheckConstraint("CK_FullAnalyses_Json", "ISJSON([ConflictsJson]) = 1 AND ISJSON([KeyEvidenceIdsJson]) = 1 AND ISJSON([InvalidationJson]) = 1 AND ISJSON([SnapshotJson]) = 1 AND ISJSON([WorkspaceResultsJson]) = 1");
                    table.CheckConstraint("CK_FullAnalyses_Tokens", "[InputTokens] >= 0 AND [OutputTokens] >= 0");
                    table.ForeignKey(
                        name: "FK_FullAnalyses_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FullAnalysisLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    FullAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PreviousStatus = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FullAnalysisLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FullAnalysisLifecycleEvents_FullAnalyses_FullAnalysisId",
                        column: x => x.FullAnalysisId,
                        principalTable: "FullAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FullAnalyses_Instrument_Timeframe_AnalysisTimeUtc",
                table: "FullAnalyses",
                columns: new[] { "InstrumentId", "Timeframe", "AnalysisTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FullAnalyses_SnapshotHash",
                table: "FullAnalyses",
                column: "SnapshotHash");

            migrationBuilder.CreateIndex(
                name: "IX_FullAnalyses_Symbol_Status_AnalysisTimeUtc",
                table: "FullAnalyses",
                columns: new[] { "Symbol", "Status", "AnalysisTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FullAnalysisLifecycleEvents_Analysis_Time",
                table: "FullAnalysisLifecycleEvents",
                columns: new[] { "FullAnalysisId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FullAnalysisLifecycleEvents");

            migrationBuilder.DropTable(
                name: "FullAnalyses");
        }
    }
}
