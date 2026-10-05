using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAllTickDataProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DataProviders",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "Name", "ProviderType", "UpdatedAtUtc" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "alltick", "AllTick", "Market", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DataProviders",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000004"));
        }
    }
}
