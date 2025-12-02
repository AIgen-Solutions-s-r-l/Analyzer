using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalyzerCore.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add PriceHistory table for storing historical price data.
    /// </summary>
    public partial class AddPriceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenAddress = table.Column<string>(type: "TEXT", maxLength: 42, nullable: false),
                    QuoteTokenAddress = table.Column<string>(type: "TEXT", maxLength: 42, nullable: false),
                    QuoteTokenSymbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    PriceUsd = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    PoolAddress = table.Column<string>(type: "TEXT", maxLength: 42, nullable: true),
                    Reserve0 = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    Reserve1 = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    Liquidity = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    BlockNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistories", x => x.Id);
                });

            // Index for querying by token and time
            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_TokenAddress_QuoteTokenSymbol_Timestamp",
                table: "PriceHistories",
                columns: new[] { "TokenAddress", "QuoteTokenSymbol", "Timestamp" });

            // Index for time-based queries (cleanup, TWAP)
            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_Timestamp",
                table: "PriceHistories",
                column: "Timestamp");

            // Index for pool-specific queries
            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_PoolAddress_Timestamp",
                table: "PriceHistories",
                columns: new[] { "PoolAddress", "Timestamp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceHistories");
        }
    }
}
