using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VSG.MarketData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMonitoredSymbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MonitoredSymbols",
                columns: new[] { "Id", "IsActive", "Symbol" },
                values: new object[,]
                {
                    { 1, true, "BTCUSDT" },
                    { 2, true, "ADAUSDT" },
                    { 3, true, "ETHUSDT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredSymbols_Symbol",
                table: "MonitoredSymbols",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MonitoredSymbols_Symbol",
                table: "MonitoredSymbols");

            migrationBuilder.DeleteData(
                table: "MonitoredSymbols",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MonitoredSymbols",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MonitoredSymbols",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
