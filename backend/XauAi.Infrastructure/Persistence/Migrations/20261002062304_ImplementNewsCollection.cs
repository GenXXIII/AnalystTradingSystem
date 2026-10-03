using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementNewsCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_ContentHash",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "UX_NewsArticles_Provider_ExternalId",
                table: "NewsArticles");

            migrationBuilder.RenameColumn(
                name: "SourceCategory",
                table: "NewsArticles",
                newName: "PrimaryCategory");

            migrationBuilder.RenameColumn(
                name: "FetchedAtUtc",
                table: "NewsArticles",
                newName: "CollectedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "NewsArticles",
                newName: "ProviderArticleId");

            migrationBuilder.AddColumn<string>(
                name: "CategoriesJson",
                table: "NewsArticles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "CountryCodesJson",
                table: "NewsArticles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "EntitiesJson",
                table: "NewsArticles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "NewsArticles",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCategoriesJson",
                table: "NewsArticles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "NewsArticles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelevanceLevel",
                table: "NewsArticles",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "Irrelevant");

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "NewsArticles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "NewsArticles",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NewsCollectionRuns",
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
                    ArticlesReceived = table.Column<int>(type: "int", nullable: false),
                    ArticlesInserted = table.Column<int>(type: "int", nullable: false),
                    ArticlesSkipped = table.Column<int>(type: "int", nullable: false),
                    ArticlesRejected = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsCollectionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsCollectionRuns_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsCollectionStates",
                columns: table => new
                {
                    DataProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastSuccessfulCollectionAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastRequestedFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastRequestedToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    RequestsMade = table.Column<int>(type: "int", nullable: false),
                    RateLimitResponses = table.Column<int>(type: "int", nullable: false),
                    ArticlesReceived = table.Column<int>(type: "int", nullable: false),
                    ArticlesInserted = table.Column<int>(type: "int", nullable: false),
                    ArticlesSkipped = table.Column<int>(type: "int", nullable: false),
                    ArticlesRejected = table.Column<int>(type: "int", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    LastErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsCollectionStates", x => x.DataProviderId);
                    table.ForeignKey(
                        name: "FK_NewsCollectionStates_DataProviders_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DataProviders",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "Name", "ProviderType", "UpdatedAtUtc" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "newsdata", "NewsData.io", "News", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Category_PublishedAtUtc",
                table: "NewsArticles",
                columns: new[] { "PrimaryCategory", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Relevance_PublishedAtUtc",
                table: "NewsArticles",
                columns: new[] { "RelevanceLevel", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Source_PublishedAtUtc",
                table: "NewsArticles",
                columns: new[] { "SourceName", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_NewsArticles_ContentHash",
                table: "NewsArticles",
                column: "ContentHash",
                unique: true,
                filter: "[ContentHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_NewsArticles_Provider_ExternalId",
                table: "NewsArticles",
                columns: new[] { "DataProviderId", "ProviderArticleId" },
                unique: true,
                filter: "[ProviderArticleId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NewsArticles_CategoriesJson",
                table: "NewsArticles",
                sql: "ISJSON([CategoriesJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NewsArticles_CountryCodesJson",
                table: "NewsArticles",
                sql: "ISJSON([CountryCodesJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NewsArticles_EntitiesJson",
                table: "NewsArticles",
                sql: "ISJSON([EntitiesJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NewsArticles_ProviderCategoriesJson",
                table: "NewsArticles",
                sql: "ISJSON([ProviderCategoriesJson]) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_NewsCollectionRuns_Provider_StartedAtUtc",
                table: "NewsCollectionRuns",
                columns: new[] { "DataProviderId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsCollectionRuns_Status_StartedAtUtc",
                table: "NewsCollectionRuns",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsCollectionStates_LastSuccessful",
                table: "NewsCollectionStates",
                column: "LastSuccessfulCollectionAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsCollectionRuns");

            migrationBuilder.DropTable(
                name: "NewsCollectionStates");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_Category_PublishedAtUtc",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_Relevance_PublishedAtUtc",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_Source_PublishedAtUtc",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "UX_NewsArticles_ContentHash",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "UX_NewsArticles_Provider_ExternalId",
                table: "NewsArticles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NewsArticles_CategoriesJson",
                table: "NewsArticles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NewsArticles_CountryCodesJson",
                table: "NewsArticles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NewsArticles_EntitiesJson",
                table: "NewsArticles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NewsArticles_ProviderCategoriesJson",
                table: "NewsArticles");

            migrationBuilder.DeleteData(
                table: "DataProviders",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"));

            migrationBuilder.DropColumn(
                name: "CategoriesJson",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "CountryCodesJson",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "EntitiesJson",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "ProviderCategoriesJson",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "RelevanceLevel",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "NewsArticles");

            migrationBuilder.RenameColumn(
                name: "ProviderArticleId",
                table: "NewsArticles",
                newName: "ExternalId");

            migrationBuilder.RenameColumn(
                name: "PrimaryCategory",
                table: "NewsArticles",
                newName: "SourceCategory");

            migrationBuilder.RenameColumn(
                name: "CollectedAtUtc",
                table: "NewsArticles",
                newName: "FetchedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_ContentHash",
                table: "NewsArticles",
                column: "ContentHash",
                filter: "[ContentHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_NewsArticles_Provider_ExternalId",
                table: "NewsArticles",
                columns: new[] { "DataProviderId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }
    }
}
