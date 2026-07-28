using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaBloqueioConsumoIndevidoENotificacaoCertificadoEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BLOQUEADA_ATE",
                table: "EMPRESAS",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CERTIFICADO_NOTIFICADO_EM",
                table: "EMPRESAS",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BLOQUEADA_ATE",
                table: "EMPRESAS");

            migrationBuilder.DropColumn(
                name: "CERTIFICADO_NOTIFICADO_EM",
                table: "EMPRESAS");
        }
    }
}
