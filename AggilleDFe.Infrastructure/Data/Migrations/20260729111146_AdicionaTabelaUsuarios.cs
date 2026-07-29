using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTabelaUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USUARIOS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LOGIN = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NOME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SENHA_HASH = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ADMINISTRADOR = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ACESSO_XMLS_BAIXADOS = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ACESSO_REGISTROS = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ACESSO_EMPRESAS = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ACESSO_CONFIGURACAO = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ACESSO_IMPORTACAO = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ACESSO_BAIXAR_XML = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    INATIVO = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIOS", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USUARIOS_LOGIN",
                table: "USUARIOS",
                column: "LOGIN",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USUARIOS");
        }
    }
}
