using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XauAi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetireMt5Provider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DataProviders",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "IsActive", "Name" },
                values: new object[] { false, "MetaTrader 5 (retired)" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DataProviders",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "IsActive", "Name" },
                values: new object[] { true, "MetaTrader 5" });
        }
    }
}
