using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridCalc.App.Migrations
{
    /// <inheritdoc />
    public partial class changecombinekey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Symbols",
                table: "Symbols");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Symbols",
                table: "Symbols",
                columns: new[] { "ExchangeId", "BaseAsset", "QuoteAsset" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Symbols",
                table: "Symbols");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Symbols",
                table: "Symbols",
                columns: new[] { "BaseAsset", "QuoteAsset" });
        }
    }
}
