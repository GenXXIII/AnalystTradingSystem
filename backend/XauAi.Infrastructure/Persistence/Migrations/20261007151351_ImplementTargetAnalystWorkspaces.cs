using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementTargetAnalystWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TargetAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Timeframe = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    AnalysisTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    TargetPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    InvalidationPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    DirectionContext = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    MasterResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasoningSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Uncertainty = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NoTargetReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PromptVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ConfigurationVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetAnalyses", x => x.Id);
                    table.CheckConstraint("CK_TargetAnalyses_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_TargetAnalyses_CurrentPrice", "[CurrentPrice] IS NULL OR [CurrentPrice] > 0");
                    table.CheckConstraint("CK_TargetAnalyses_InvalidationPrice", "[InvalidationPrice] IS NULL OR [InvalidationPrice] > 0");
                    table.CheckConstraint("CK_TargetAnalyses_SnapshotJson", "ISJSON([SnapshotJson]) = 1");
                    table.CheckConstraint("CK_TargetAnalyses_TargetPrice", "[TargetPrice] IS NULL OR [TargetPrice] > 0");
                    table.ForeignKey(
                        name: "FK_TargetAnalyses_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TargetAnalysisEvidence",
                columns: table => new
                {
                    TargetAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetAnalysisEvidence", x => new { x.TargetAnalysisId, x.EvidenceRecordId });
                    table.ForeignKey(
                        name: "FK_TargetAnalysisEvidence_EvidenceRecords_EvidenceRecordId",
                        column: x => x.EvidenceRecordId,
                        principalTable: "EvidenceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TargetAnalysisEvidence_TargetAnalyses_TargetAnalysisId",
                        column: x => x.TargetAnalysisId,
                        principalTable: "TargetAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TargetAnalysisLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TargetAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_TargetAnalysisLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetAnalysisLifecycleEvents_TargetAnalyses_TargetAnalysisId",
                        column: x => x.TargetAnalysisId,
                        principalTable: "TargetAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TargetSpecialistResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Workspace = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    HasCandidate = table.Column<bool>(type: "bit", nullable: false),
                    CandidateTargetPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    CandidateInvalidationPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    DirectionContext = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    RiskAcceptable = table.Column<bool>(type: "bit", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Uncertainty = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PromptVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ConfigurationVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    LatencyMilliseconds = table.Column<int>(type: "int", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetSpecialistResults", x => x.Id);
                    table.CheckConstraint("CK_TargetSpecialistResults_Confidence", "[Confidence] IS NULL OR [Confidence] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_TargetSpecialistResults_EvidenceIdsJson", "ISJSON([EvidenceIdsJson]) = 1");
                    table.CheckConstraint("CK_TargetSpecialistResults_InvalidationPrice", "[CandidateInvalidationPrice] IS NULL OR [CandidateInvalidationPrice] > 0");
                    table.CheckConstraint("CK_TargetSpecialistResults_OutputJson", "ISJSON([OutputJson]) = 1");
                    table.CheckConstraint("CK_TargetSpecialistResults_TargetPrice", "[CandidateTargetPrice] IS NULL OR [CandidateTargetPrice] > 0");
                    table.CheckConstraint("CK_TargetSpecialistResults_Tokens", "([InputTokens] IS NULL OR [InputTokens] >= 0) AND ([OutputTokens] IS NULL OR [OutputTokens] >= 0)");
                    table.ForeignKey(
                        name: "FK_TargetSpecialistResults_TargetAnalyses_TargetAnalysisId",
                        column: x => x.TargetAnalysisId,
                        principalTable: "TargetAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TargetAnalyses_Instrument_Timeframe_AnalysisTimeUtc",
                table: "TargetAnalyses",
                columns: new[] { "InstrumentId", "Timeframe", "AnalysisTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TargetAnalyses_MasterResultId",
                table: "TargetAnalyses",
                column: "MasterResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetAnalyses_Symbol_Status_AnalysisTimeUtc",
                table: "TargetAnalyses",
                columns: new[] { "Symbol", "Status", "AnalysisTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TargetAnalysisEvidence_EvidenceRecordId",
                table: "TargetAnalysisEvidence",
                column: "EvidenceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetAnalysisLifecycleEvents_Analysis_Time",
                table: "TargetAnalysisLifecycleEvents",
                columns: new[] { "TargetAnalysisId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_TargetSpecialistResults_Analysis_Workspace",
                table: "TargetSpecialistResults",
                columns: new[] { "TargetAnalysisId", "Workspace" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TargetAnalyses_TargetSpecialistResults_MasterResultId",
                table: "TargetAnalyses",
                column: "MasterResultId",
                principalTable: "TargetSpecialistResults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TargetAnalyses_TargetSpecialistResults_MasterResultId",
                table: "TargetAnalyses");

            migrationBuilder.DropTable(
                name: "TargetAnalysisEvidence");

            migrationBuilder.DropTable(
                name: "TargetAnalysisLifecycleEvents");

            migrationBuilder.DropTable(
                name: "TargetSpecialistResults");

            migrationBuilder.DropTable(
                name: "TargetAnalyses");
        }
    }
}
