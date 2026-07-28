using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaUltimaEmpresaProcessadaConfiguracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ULTIMA_EMPRESA_PROCESSADA_ID",
                table: "CONFIGURACAO",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ULTIMA_EMPRESA_PROCESSADA_ID",
                table: "CONFIGURACAO");
        }
    }
}
