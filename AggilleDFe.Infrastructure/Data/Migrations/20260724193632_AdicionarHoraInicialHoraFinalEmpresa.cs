using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarHoraInicialHoraFinalEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "HORA_FINAL",
                table: "EMPRESAS",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HORA_INICIAL",
                table: "EMPRESAS",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HORA_FINAL",
                table: "EMPRESAS");

            migrationBuilder.DropColumn(
                name: "HORA_INICIAL",
                table: "EMPRESAS");
        }
    }
}
