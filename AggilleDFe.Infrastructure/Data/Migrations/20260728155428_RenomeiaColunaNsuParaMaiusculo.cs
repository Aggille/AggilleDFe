using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggilleDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenomeiaColunaNsuParaMaiusculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A migration AdicionarNsuLog original criou a coluna como "Nsu"
            // (sem HasColumnName, bug corrigido depois). Em ambientes onde
            // aquela migration ja foi aplicada com o nome antigo, o EF nao a
            // reaplica so porque o conteudo mudou (rastreia por Id), entao
            // renomeia aqui de forma condicional/idempotente.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'LOGS' AND column_name = 'Nsu'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'LOGS' AND column_name = 'NSU'
                    ) THEN
                        ALTER TABLE ""LOGS"" RENAME COLUMN ""Nsu"" TO ""NSU"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'LOGS' AND column_name = 'NSU'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'LOGS' AND column_name = 'Nsu'
                    ) THEN
                        ALTER TABLE ""LOGS"" RENAME COLUMN ""NSU"" TO ""Nsu"";
                    END IF;
                END $$;
            ");
        }
    }
}
