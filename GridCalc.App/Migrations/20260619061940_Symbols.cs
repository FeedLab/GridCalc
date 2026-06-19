using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridCalc.App.Migrations
{
    /// <inheritdoc />
    public partial class Symbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Symbols",
                columns: table => new
                {
                    BaseAsset = table.Column<string>(type: "TEXT", nullable: false),
                    QuoteAsset = table.Column<string>(type: "TEXT", nullable: false),
                    ExchangeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symbols", x => new { x.BaseAsset, x.QuoteAsset });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Symbols");
        }
    }
}
