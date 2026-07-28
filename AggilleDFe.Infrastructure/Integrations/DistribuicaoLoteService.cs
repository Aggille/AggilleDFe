using System.Collections.Concurrent;
using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Application.Services;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AggilleDFe.Infrastructure.Integrations;

public class DistribuicaoLoteService(
    IServiceScopeFactory scopeFactory,
    IConfiguracaoRepository configuracaoRepository,
    IEmpresaRepository empresaRepository,
    ILogRepository logRepository) : IDistribuicaoLoteService
{
    public async Task<ResultadoDistribuicaoLoteDto> ExecutarTodasAsync(bool execucaoManual, CancellationToken cancellationToken = default)
    {
        var configuracao = await configuracaoRepository.ObterAsync(cancellationToken);
        var todasEmpresas = await empresaRepository.PesquisarAsync(null, cancellationToken);
        var agora = DateTime.Now;

        var empresasAtivas = todasEmpresas.Where(e => e.Inativo != "S").ToList();

        var empresasBloqueadas = empresasAtivas.Where(e => e.BloqueadaAte > agora).ToList();
        foreach (var empresa in empresasBloqueadas)
        {
            await LogarEmpresaNaoProcessadaAsync(empresa, cancellationToken,
                $"bloqueada por consumo indevido até {empresa.BloqueadaAte:dd/MM/yyyy HH:mm}");
        }

        var empresasDisponiveis = empresasAtivas.Except(empresasBloqueadas).ToList();
        var empresasElegiveis = empresasDisponiveis
            .Where(e => JanelaExecucaoService.PodeExecutar(e, agora, execucaoManual))
            .ToList();
        var empresasForaDaJanela = empresasDisponiveis.Except(empresasElegiveis).ToList();

        foreach (var empresa in empresasForaDaJanela)
        {
            await LogarEmpresaNaoProcessadaAsync(empresa, cancellationToken);
        }

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

        List<Empresa> empresasProcessadas;

        if (configuracao?.ProcessarIndividualmente == "S")
        {
            // "Processar 1 empresa de cada vez": só a empresa da vez
            // (rodízio, na ordem de Posicao/Id) é consultada na SEFAZ nesta
            // chamada — as demais só ficam registradas como não
            // processadas. Vale tanto pro ciclo automático do Worker quanto
            // pro botão manual "Baixar XMLs" (todas as empresas) — evita
            // bater na SEFAZ pra todas as empresas de uma vez (causa comum
            // de rejeição cStat 656 "Consumo Indevido"). Só o botão de
            // baixar XMLs de UMA empresa específica (grid de Empresas) foge
            // dessa regra, porque nem passa por aqui — chama
            // IDistribuicaoDfeService direto pra aquela empresa.
            var ordenadas = empresasElegiveis.OrderBy(e => e.Posicao ?? int.MaxValue).ThenBy(e => e.Id).ToList();
            var indiceUltima = configuracao.UltimaEmpresaProcessadaId is int ultimaId
                ? ordenadas.FindIndex(e => e.Id == ultimaId)
                : -1;
            var empresaDaVez = ordenadas[(indiceUltima + 1) % ordenadas.Count];

            foreach (var empresa in ordenadas.Where(e => e.Id != empresaDaVez.Id))
            {
                await LogarEmpresaNaoProcessadaAsync(empresa, cancellationToken);
            }

            await ProcessarEmpresaAsync(empresaDaVez.Id);

            configuracao.UltimaEmpresaProcessadaId = empresaDaVez.Id;
            await configuracaoRepository.SalvarAsync(configuracao, cancellationToken);

            empresasProcessadas = [empresaDaVez];
        }
        else
        {
            await Task.WhenAll(empresasElegiveis.Select(empresa => ProcessarEmpresaAsync(empresa.Id)));
            empresasProcessadas = empresasElegiveis;
        }

        var empresasComErro = resultadosPorEmpresa.Count(r => r.ComErro);
        var xmlsNfe = resultadosPorEmpresa.Sum(r => r.XmlsNfe);
        var xmlsCte = resultadosPorEmpresa.Sum(r => r.XmlsCte);

        return new ResultadoDistribuicaoLoteDto
        {
            EmpresasProcessadas = empresasProcessadas.Count,
            EmpresasComErro = empresasComErro,
            XmlsBaixadosNfe = xmlsNfe,
            XmlsBaixadosCte = xmlsCte,
            Mensagem = $"{empresasProcessadas.Count} empresa(s) processada(s) — {xmlsNfe} XML(s) de NFe e {xmlsCte} XML(s) de CTe baixados" +
                (empresasComErro > 0 ? $" ({empresasComErro} empresa(s) com erro — ver Registros)." : ".")
        };
    }

    private async Task LogarEmpresaNaoProcessadaAsync(Empresa empresa, CancellationToken cancellationToken, string? motivo = null)
    {
        var agora = TimeOnly.FromDateTime(DateTime.Now);
        await logRepository.IncluirAsync(new Log
        {
            Data = DateOnly.FromDateTime(DateTime.Now),
            HoraInicio = agora,
            HoraFinal = agora,
            EmpresaId = empresa.Id,
            Mensagem = motivo is null ? "Empresa não processada" : $"Empresa não processada ({motivo})"
        }, cancellationToken);
    }
}
