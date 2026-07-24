using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using NFe.Servicos;

namespace AggilleDFe.Infrastructure.Integrations;

public class SefazStatusService(IEmpresaRepository empresaRepository, IConfiguration configuration) : ISefazStatusService
{
    public async Task<(StatusSefazResultadoDto? Resultado, string? Erro)> ConsultarStatusAsync(int empresaId, CancellationToken cancellationToken = default)
    {
        var empresa = await empresaRepository.ObterPorIdAsync(empresaId, cancellationToken);
        if (empresa is null)
        {
            return (null, "Empresa não encontrada.");
        }

        try
        {
            var certificado = ZeusConfiguracaoFactory.CarregarCertificado(empresa);
            var diretorioSchemas = configuration["SchemasPath"] ?? "SCHEMAS";
            var configuracao = ZeusConfiguracaoFactory.Criar(empresa, diretorioSchemas);

            using var servicosNFe = new ServicosNFe(configuracao, certificado);
            var retorno = servicosNFe.NfeStatusServico(exceptionCompleta: false);
            var status = retorno.Retorno;

            return (new StatusSefazResultadoDto
            {
                CStat = status.cStat,
                XMotivo = status.xMotivo,
                Uf = status.cUF.ToString(),
                Ambiente = status.tpAmb == DFe.Classes.Flags.TipoAmbiente.Producao ? "P" : "H",
                VersaoLayout = status.versao,
                DhRecbto = status.dhRecbto,
                TempoMedioMs = status.tMed
            }, null);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }
        catch (Exception ex)
        {
            return (null, $"Falha ao consultar o status do SEFAZ: {ex.Message}");
        }
    }
}
