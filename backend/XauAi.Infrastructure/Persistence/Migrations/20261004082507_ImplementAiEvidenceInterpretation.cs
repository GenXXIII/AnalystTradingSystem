using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAiEvidenceInterpretation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiAnalyses_InstrumentId",
                table: "AiAnalyses");

            migrationBuilder.AddColumn<string>(
                name: "AffectedAssetsJson",
                table: "AiAnalyses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnalysisTimeUtc",
                table: "AiAnalyses",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CacheKey",
                table: "AiAnalyses",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Confidence",
                table: "AiAnalyses",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentRelevance",
                table: "AiAnalyses",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "AiAnalyses",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EvidenceUpdatedAtUtc",
                table: "AiAnalyses",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "EvidenceVersion",
                table: "AiAnalyses",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedEffect",
                table: "AiAnalyses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Impact",
                table: "AiAnalyses",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatus",
                table: "AiAnalyses",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Invalid");

            migrationBuilder.AddColumn<string>(
                name: "Mechanism",
                table: "AiAnalyses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ObservedReaction",
                table: "AiAnalyses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReactionAlignment",
                table: "AiAnalyses",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "Specialist",
                table: "AiAnalyses",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Master");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "AiAnalyses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Timeframe",
                table: "AiAnalyses",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uncertainty",
                table: "AiAnalyses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_CacheKey_Status_Lifecycle",
                table: "AiAnalyses",
                columns: new[] { "CacheKey", "Status", "LifecycleStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_Instrument_AnalysisTimeUtc",
                table: "AiAnalyses",
                columns: new[] { "InstrumentId", "AnalysisTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_Specialist_Type_Lifecycle_AnalysisTimeUtc",
                table: "AiAnalyses",
                columns: new[] { "Specialist", "AnalysisType", "LifecycleStatus", "AnalysisTimeUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiAnalyses_AffectedAssetsJson",
                table: "AiAnalyses",
                sql: "ISJSON([AffectedAssetsJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiAnalyses_Confidence",
                table: "AiAnalyses",
                sql: "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiAnalyses_CacheKey_Status_Lifecycle",
                table: "AiAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_AiAnalyses_Instrument_AnalysisTimeUtc",
                table: "AiAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_AiAnalyses_Specialist_Type_Lifecycle_AnalysisTimeUtc",
                table: "AiAnalyses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AiAnalyses_AffectedAssetsJson",
                table: "AiAnalyses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AiAnalyses_Confidence",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "AffectedAssetsJson",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "AnalysisTimeUtc",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "CacheKey",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "CurrentRelevance",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "EvidenceUpdatedAtUtc",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "EvidenceVersion",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "ExpectedEffect",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Impact",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Mechanism",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "ObservedReaction",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "ReactionAlignment",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Specialist",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Timeframe",
                table: "AiAnalyses");

            migrationBuilder.DropColumn(
                name: "Uncertainty",
                table: "AiAnalyses");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalyses_InstrumentId",
                table: "AiAnalyses",
                column: "InstrumentId");
        }
    }
}
