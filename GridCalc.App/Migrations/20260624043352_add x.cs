using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridCalc.App.Migrations
{
    /// <inheritdoc />
    public partial class addx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Candle1Minutes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_CandleRecord_Exchange_Symbol_Timestamp",
                table: "Candle1Minutes",
                columns: new[] { "ExchangeId", "Symbol", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandleRecord_Exchange_Symbol_Timestamp",
                table: "Candle1Minutes");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Candle1Minutes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
