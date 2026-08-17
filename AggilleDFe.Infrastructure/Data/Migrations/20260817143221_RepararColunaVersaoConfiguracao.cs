using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepararColunaVersaoConfiguracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repara ambientes onde a migration AdicionaVersaoConfiguracao ficou
            // registrada como aplicada no __EFMigrationsHistory sem a coluna
            // realmente existir (historico dessincronizado do schema real).
            migrationBuilder.Sql(
                "ALTER TABLE \"CONFIGURACAO\" ADD COLUMN IF NOT EXISTS \"VERSAO\" character varying(20);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
