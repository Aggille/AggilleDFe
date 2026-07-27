using System.Collections.Concurrent;
using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Application.Services;
using AggilleDFe.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AggilleDFe.Infrastructure.Integrations;

public class DistribuicaoLoteService(
    IServiceScopeFactory scopeFactory,
    IConfiguracaoRepository configuracaoRepository,
    IEmpresaRepository empresaRepository) : IDistribuicaoLoteService
{
    public async Task<ResultadoDistribuicaoLoteDto> ExecutarTodasAsync(bool execucaoManual, CancellationToken cancellationToken = default)
    {
        var configuracao = await configuracaoRepository.ObterAsync(cancellationToken);
        var todasEmpresas = await empresaRepository.PesquisarAsync(null, cancellationToken);
        var agora = DateTime.Now;

        var empresasElegiveis = todasEmpresas
            .Where(e => e.Inativo != "S")
            .Where(e => JanelaExecucaoService.PodeExecutar(e, agora, execucaoManual))
            .ToList();

        if (empresasElegiveis.Count == 0)
        {
            return new ResultadoDistribuicaoLoteDto { Mensagem = "Nenhuma empresa elegível para execução no momento." };
        }

        var resultadosPorEmpresa = new ConcurrentBag<(bool ComErro, int XmlsNfe, int XmlsCte)>();

        async Task ProcessarEmpresaAsync(int empresaId)
        {
            using var scope = scopeFactory.CreateScope();
            var distribuicaoDfeService = scope.ServiceProvider.GetRequiredService<IDistribuicaoDfeService>();
            var (resultado, erro) = await distribuicaoDfeService.ExecutarAsync(empresaId, execucaoManual, cancellationToken);
            resultadosPorEmpresa.Add((erro is not null, resultado?.XmlsBaixadosNfe ?? 0, resultado?.XmlsBaixadosCte ?? 0));
        }

        if (configuracao?.ProcessarIndividualmente == "S")
        {
            foreach (var empresa in empresasElegiveis)
            {
                await ProcessarEmpresaAsync(empresa.Id);
            }
        }
        else
        {
            await Task.WhenAll(empresasElegiveis.Select(empresa => ProcessarEmpresaAsync(empresa.Id)));
        }

        var empresasComErro = resultadosPorEmpresa.Count(r => r.ComErro);
        var xmlsNfe = resultadosPorEmpresa.Sum(r => r.XmlsNfe);
        var xmlsCte = resultadosPorEmpresa.Sum(r => r.XmlsCte);

        return new ResultadoDistribuicaoLoteDto
        {
            EmpresasProcessadas = empresasElegiveis.Count,
            EmpresasComErro = empresasComErro,
            XmlsBaixadosNfe = xmlsNfe,
            XmlsBaixadosCte = xmlsCte,
            Mensagem = $"{empresasElegiveis.Count} empresa(s) processada(s) — {xmlsNfe} XML(s) de NFe e {xmlsCte} XML(s) de CTe baixados" +
                (empresasComErro > 0 ? $" ({empresasComErro} empresa(s) com erro — ver Registros)." : ".")
        };
    }
}
