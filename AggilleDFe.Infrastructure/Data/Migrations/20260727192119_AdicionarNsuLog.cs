using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNsuLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Nsu",
                table: "LOGS",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nsu",
                table: "LOGS");
        }
    }
}
