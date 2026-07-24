using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONFIGURACAO",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NOME_EMPRESA = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CNPJ_EMPRESA = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    VERSAO_BANCO = table.Column<int>(type: "integer", nullable: true),
                    TEMPO_EXECUCAO = table.Column<int>(type: "integer", nullable: true),
                    QUANTIDADE_EMPRESAS_PERMITIDAS = table.Column<int>(type: "integer", nullable: true),
                    API_ATIVA = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    PORTA_API = table.Column<int>(type: "integer", nullable: true),
                    USUARIO_API = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SENHA_API = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PROCESSAR_INDIVIDUALMENTE = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFIGURACAO", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "EMPRESAS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RAZAO_SOCIAL = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CNPJ = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UF = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    CERTIFICADO_DIGITAL = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SENHA_CERTIFICADO = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PASTA_XML = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ULTIMO_NSU = table.Column<int>(type: "integer", nullable: true),
                    AMBIENTE = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    TIMEOUT = table.Column<int>(type: "integer", nullable: true),
                    TEMPO_RETORNO = table.Column<int>(type: "integer", nullable: true),
                    INTERVALO_TENTATIVAS = table.Column<int>(type: "integer", nullable: true),
                    QUANTIDADE_TENTATIVAS = table.Column<int>(type: "integer", nullable: true),
                    SSL_LIB = table.Column<int>(type: "integer", nullable: true),
                    SSL_CRYPT = table.Column<int>(type: "integer", nullable: true),
                    SSL_HTTP_LIB = table.Column<int>(type: "integer", nullable: true),
                    SSL_XML_SIGN_LIB = table.Column<int>(type: "integer", nullable: true),
                    SSL_TYPE = table.Column<int>(type: "integer", nullable: true),
                    EMAIL_ENVIO_NOTIFICACOES = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SERVIDOR_SMTP = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    USUARIO_SMTP = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SENHA_SMTP = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EMAIL_SMTP = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TIPO_AUTENTICACAO_SMTP = table.Column<int>(type: "integer", nullable: true),
                    SERVIDOR_POP = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    USUARIO_POP = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EMAIL_POP = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SENHA_POP = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TIPO_AUTENTICACAO_POP = table.Column<int>(type: "integer", nullable: true),
                    PORTA_POP = table.Column<int>(type: "integer", nullable: true),
                    PORTA_SMTP = table.Column<int>(type: "integer", nullable: true),
                    IE = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MANIFESTA = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    POSICAO = table.Column<int>(type: "integer", nullable: true),
                    INATIVO = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ULTIMO_NSU_CTE = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPRESAS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LOGS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DATA = table.Column<DateOnly>(type: "date", nullable: true),
                    HORA_INICIO = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    HORA_FINAL = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EMPRESA_ID = table.Column<int>(type: "integer", nullable: true),
                    QUANTIDADE_XMLS = table.Column<int>(type: "integer", nullable: true),
                    MENSAGEM = table.Column<string>(type: "text", nullable: true),
                    XML_ID = table.Column<int>(type: "integer", nullable: true),
                    CHAVE = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOGS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "XMLS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CHAVE = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    PROTOCOLO = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EMISSAO = table.Column<DateOnly>(type: "date", nullable: true),
                    DATA_DOWNLOAD = table.Column<DateOnly>(type: "date", nullable: true),
                    FORNECEDOR_NOME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FORNECEDOR_CNPJ = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FORNECEDOR_CIDADE = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FORNECEDOR_UF = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    VALOR_TOTAL = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    VALOR_ICMS = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    STATUS_NFE = table.Column<int>(type: "integer", nullable: true),
                    MENSAGEM_NFE = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    NOME_XML = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    NUMERO = table.Column<int>(type: "integer", nullable: true),
                    SERIE = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    MODELO = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    EMPRESA_ID = table.Column<int>(type: "integer", nullable: true),
                    CANCELADA = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    SCHEMA = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DESCRICAO = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MENSAGEM = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SITUACAO = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DATA_CIENCIA = table.Column<DateOnly>(type: "date", nullable: true),
                    DATA_REALIZACAO = table.Column<DateOnly>(type: "date", nullable: true),
                    DATA_NAO_REALIZACAO = table.Column<DateOnly>(type: "date", nullable: true),
                    DATA_DESCONHECIMENTO = table.Column<DateOnly>(type: "date", nullable: true),
                    MOTIVO_NAO_REALIZACAO = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DATA_CANCELAMENTO = table.Column<DateOnly>(type: "date", nullable: true),
                    MOTIVO_CANCELAMENTO = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XMLS", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONFIGURACAO");

            migrationBuilder.DropTable(
                name: "EMPRESAS");

            migrationBuilder.DropTable(
                name: "LOGS");

            migrationBuilder.DropTable(
                name: "XMLS");
        }
    }
}
