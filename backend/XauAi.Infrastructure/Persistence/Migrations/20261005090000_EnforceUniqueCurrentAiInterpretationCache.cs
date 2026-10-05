using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations;

public partial class EnforceUniqueCurrentAiInterpretationCache : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            WITH RankedCurrent AS
            (
                SELECT [Id],
                       ROW_NUMBER() OVER (
                           PARTITION BY [CacheKey], [Status], [LifecycleStatus]
                           ORDER BY [CreatedAtUtc] DESC, [Id] DESC) AS [RowNumber]
                FROM [AiAnalyses]
                WHERE [Status] = 'Completed' AND [LifecycleStatus] = 'Current'
            )
            UPDATE analysis
            SET [LifecycleStatus] = 'Superseded'
            FROM [AiAnalyses] AS analysis
            INNER JOIN RankedCurrent AS ranked ON ranked.[Id] = analysis.[Id]
            WHERE ranked.[RowNumber] > 1;
            """);

        migrationBuilder.DropIndex(
            name: "IX_AiAnalyses_CacheKey_Status_Lifecycle",
            table: "AiAnalyses");

        migrationBuilder.CreateIndex(
            name: "IX_AiAnalyses_CacheKey_Status_Lifecycle",
            table: "AiAnalyses",
            columns: new[] { "CacheKey", "Status", "LifecycleStatus" },
            unique: true,
            filter: "[Status] = 'Completed' AND [LifecycleStatus] = 'Current'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AiAnalyses_CacheKey_Status_Lifecycle",
            table: "AiAnalyses");

        migrationBuilder.CreateIndex(
            name: "IX_AiAnalyses_CacheKey_Status_Lifecycle",
            table: "AiAnalyses",
            columns: new[] { "CacheKey", "Status", "LifecycleStatus" });
    }
}
