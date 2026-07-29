using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAcessoExportarXmls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ACESSO_EXPORTAR_XMLS",
                table: "USUARIOS",
                type: "character varying(1)",
                maxLength: 1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ACESSO_EXPORTAR_XMLS",
                table: "USUARIOS");
        }
    }
}
