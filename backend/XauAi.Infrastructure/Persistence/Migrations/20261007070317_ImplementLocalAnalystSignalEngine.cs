using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementLocalAnalystSignalEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CandleState",
                table: "TradingSignals",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfigurationVersion",
                table: "TradingSignals",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAtUtc",
                table: "TradingSignals",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExplanationJson",
                table: "TradingSignals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvalidationReason",
                table: "TradingSignals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KtrState",
                table: "TradingSignals",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastEvaluatedCandleTimeUtc",
                table: "TradingSignals",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiquidityState",
                table: "TradingSignals",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "TradingSignals",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MomentumState",
                table: "TradingSignals",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "TradingSignals",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignalCandleId",
                table: "TradingSignals",
                type: "varchar(160)",
                unicode: false,
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignalCandleTimeUtc",
                table: "TradingSignals",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignalKey",
                table: "TradingSignals",
                type: "varchar(160)",
                unicode: false,
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "TradingSignals",
                type: "varchar(48)",
                unicode: false,
                maxLength: 48,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructureState",
                table: "TradingSignals",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "TradingSignals",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidUntilUtc",
                table: "TradingSignals",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VolatilityState",
                table: "TradingSignals",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocalAnalystProcessingStates",
                columns: table => new
                {
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeframeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastProcessedCandleTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastStrategyEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentTradingSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastResult = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LastReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalAnalystProcessingStates", x => new { x.InstrumentId, x.TimeframeId });
                    table.CheckConstraint("CK_LocalAnalystProcessingStates_SnapshotJson", "[LastSnapshotJson] IS NULL OR ISJSON([LastSnapshotJson]) = 1");
                    table.ForeignKey(
                        name: "FK_LocalAnalystProcessingStates_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalAnalystProcessingStates_StrategyEvaluations_LastStrategyEvaluationId",
                        column: x => x.LastStrategyEvaluationId,
                        principalTable: "StrategyEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalAnalystProcessingStates_Timeframes_TimeframeId",
                        column: x => x.TimeframeId,
                        principalTable: "Timeframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalAnalystProcessingStates_TradingSignals_CurrentTradingSignalId",
                        column: x => x.CurrentTradingSignalId,
                        principalTable: "TradingSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradingSignalLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TradingSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrategyEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PreviousStatus = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Direction = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CandleTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    Score = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingSignalLifecycleEvents", x => x.Id);
                    table.CheckConstraint("CK_TradingSignalLifecycleEvents_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_TradingSignalLifecycleEvents_DetailsJson", "[DetailsJson] IS NULL OR ISJSON([DetailsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_TradingSignalLifecycleEvents_StrategyEvaluations_StrategyEvaluationId",
                        column: x => x.StrategyEvaluationId,
                        principalTable: "StrategyEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSignalLifecycleEvents_TradingSignals_TradingSignalId",
                        column: x => x.TradingSignalId,
                        principalTable: "TradingSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Strategies",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "Description", "Key", "Name" },
                values: new object[] { new Guid("40000000-0000-0000-0000-000000000001"), "DeterministicSignal", new DateTimeOffset(new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Phase 12 deterministic, AI-independent local analyst and signal engine.", "local-analyst-xauusd", "Local XAUUSD Analyst" });

            migrationBuilder.InsertData(
                table: "StrategyVersions",
                columns: new[] { "Id", "CreatedAtUtc", "DefinitionHash", "DefinitionJson", "EffectiveFromUtc", "RetiredAtUtc", "Status", "StrategyId", "Version" },
                values: new object[] { new Guid("41000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "05aee2de1eedebe1cc22d748166ed8660012214084f332bad184fef69272d62b", "{\"engine\":\"local-analyst\",\"version\":\"phase12-v1\",\"symbol\":\"XAUUSD\",\"states\":[\"NOTHING\",\"BUY\",\"SELL\",\"STOP\"],\"usesAi\":false}", new DateTimeOffset(new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Active", new Guid("40000000-0000-0000-0000-000000000001"), "phase12-v1" });

            migrationBuilder.CreateIndex(
                name: "UX_TradingSignals_LocalAnalyst_Active",
                table: "TradingSignals",
                columns: new[] { "InstrumentId", "TimeframeId", "Source" },
                unique: true,
                filter: "[Status] = 'ACTIVE' AND [Source] = 'LocalAnalyst'");

            migrationBuilder.CreateIndex(
                name: "UX_TradingSignals_SignalKey",
                table: "TradingSignals",
                column: "SignalKey",
                unique: true,
                filter: "[SignalKey] IS NOT NULL AND [SignalKey] <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TradingSignals_ExplanationJson",
                table: "TradingSignals",
                sql: "[ExplanationJson] IS NULL OR ISJSON([ExplanationJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TradingSignals_MaxScore",
                table: "TradingSignals",
                sql: "[MaxScore] IS NULL OR [MaxScore] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TradingSignals_Score",
                table: "TradingSignals",
                sql: "[Score] IS NULL OR [Score] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAnalystProcessingStates_CurrentTradingSignalId",
                table: "LocalAnalystProcessingStates",
                column: "CurrentTradingSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAnalystProcessingStates_LastStrategyEvaluationId",
                table: "LocalAnalystProcessingStates",
                column: "LastStrategyEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAnalystProcessingStates_TimeframeId",
                table: "LocalAnalystProcessingStates",
                column: "TimeframeId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAnalystProcessingStates_UpdatedAtUtc",
                table: "LocalAnalystProcessingStates",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignalLifecycleEvents_Signal_Time",
                table: "TradingSignalLifecycleEvents",
                columns: new[] { "TradingSignalId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignalLifecycleEvents_StrategyEvaluationId",
                table: "TradingSignalLifecycleEvents",
                column: "StrategyEvaluationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalAnalystProcessingStates");

            migrationBuilder.DropTable(
                name: "TradingSignalLifecycleEvents");

            migrationBuilder.DropIndex(
                name: "UX_TradingSignals_LocalAnalyst_Active",
                table: "TradingSignals");

            migrationBuilder.DropIndex(
                name: "UX_TradingSignals_SignalKey",
                table: "TradingSignals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TradingSignals_ExplanationJson",
                table: "TradingSignals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TradingSignals_MaxScore",
                table: "TradingSignals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TradingSignals_Score",
                table: "TradingSignals");

            migrationBuilder.DeleteData(
                table: "StrategyVersions",
                keyColumn: "Id",
                keyValue: new Guid("41000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Strategies",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"));

            migrationBuilder.DropColumn(
                name: "CandleState",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "ConfigurationVersion",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "EndedAtUtc",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "ExplanationJson",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "InvalidationReason",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "KtrState",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedCandleTimeUtc",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "LiquidityState",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "MomentumState",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "SignalCandleId",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "SignalCandleTimeUtc",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "SignalKey",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "StructureState",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "ValidUntilUtc",
                table: "TradingSignals");

            migrationBuilder.DropColumn(
                name: "VolatilityState",
                table: "TradingSignals");
        }
    }
}
