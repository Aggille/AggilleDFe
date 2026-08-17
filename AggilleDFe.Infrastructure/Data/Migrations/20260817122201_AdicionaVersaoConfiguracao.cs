using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaVersaoConfiguracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VERSAO",
                table: "CONFIGURACAO",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VERSAO",
                table: "CONFIGURACAO");
        }
    }
}
