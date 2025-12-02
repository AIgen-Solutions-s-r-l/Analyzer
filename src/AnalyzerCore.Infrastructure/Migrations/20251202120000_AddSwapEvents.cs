using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalyzerCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SwapEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PoolAddress = table.Column<string>(type: "TEXT", maxLength: 42, nullable: false),
                    ChainId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TransactionHash = table.Column<string>(type: "TEXT", maxLength: 66, nullable: false),
                    BlockNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    LogIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Sender = table.Column<string>(type: "TEXT", maxLength: 42, nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", maxLength: 42, nullable: false),
                    Amount0 = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    Amount1 = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "TEXT", precision: 36, scale: 18, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SwapEvents_PoolAddress_Timestamp",
                table: "SwapEvents",
                columns: new[] { "PoolAddress", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SwapEvents_Timestamp",
                table: "SwapEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SwapEvents_TransactionHash_LogIndex",
                table: "SwapEvents",
                columns: new[] { "TransactionHash", "LogIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SwapEvents");
        }
    }
}
