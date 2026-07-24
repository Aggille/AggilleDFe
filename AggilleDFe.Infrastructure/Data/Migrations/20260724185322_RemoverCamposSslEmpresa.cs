using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoverCamposSslEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SSL_CRYPT",
                table: "EMPRESAS");

            migrationBuilder.DropColumn(
                name: "SSL_HTTP_LIB",
                table: "EMPRESAS");

            migrationBuilder.DropColumn(
                name: "SSL_LIB",
                table: "EMPRESAS");

            migrationBuilder.DropColumn(
                name: "SSL_TYPE",
                table: "EMPRESAS");

            migrationBuilder.DropColumn(
                name: "SSL_XML_SIGN_LIB",
                table: "EMPRESAS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SSL_CRYPT",
                table: "EMPRESAS",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SSL_HTTP_LIB",
                table: "EMPRESAS",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SSL_LIB",
                table: "EMPRESAS",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SSL_TYPE",
                table: "EMPRESAS",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SSL_XML_SIGN_LIB",
                table: "EMPRESAS",
                type: "integer",
                nullable: true);
        }
    }
}
