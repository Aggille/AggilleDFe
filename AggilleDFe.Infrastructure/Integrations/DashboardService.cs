using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Infrastructure.Integrations;

public class DashboardService(
    IEmpresaRepository empresaRepository,
    ILogRepository logRepository) : IDashboardService
{
    private const int DiasAvisoVencimentoCertificado = 15;
    private static readonly string[] PalavrasChaveErro = ["erro", "falha", "rejei", "inesperado"];

    public async Task<DashboardDto> ObterAsync(CancellationToken cancellationToken = default)
    {
        var todasEmpresas = await empresaRepository.PesquisarAsync(null, cancellationToken);
        var empresasAtivas = todasEmpresas.Where(e => e.Inativo != "S").ToList();
        var agora = DateTime.Now;
        var hoje = DateOnly.FromDateTime(agora);

        var logsHoje = await logRepository.PesquisarAsync(null, hoje, hoje, cancellationToken);
        var ultimaExecucaoPorEmpresa = logsHoje
            .Where(l => l.EmpresaId is not null)
            .GroupBy(l => l.EmpresaId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.HoraFinal ?? l.HoraInicio).First());

        var empresas = empresasAtivas.Select(empresa =>
        {
            ultimaExecucaoPorEmpresa.TryGetValue(empresa.Id, out var ultimoLog);
            return new DashboardEmpresaResumoDto
            {
                EmpresaId = empresa.Id,
                RazaoSocial = empresa.RazaoSocial,
                UltimaExecucaoData = ultimoLog?.Data,
                UltimaExecucaoHora = ultimoLog?.HoraFinal ?? ultimoLog?.HoraInicio,
                Bloqueada = empresa.BloqueadaAte > agora,
                BloqueadaAte = empresa.BloqueadaAte,
                CertificadoDiasRestantes = ObterDiasRestantesCertificado(empresa)
            };
        }).ToList();

        return new DashboardDto
        {
            EmpresasAtivas = empresasAtivas.Count,
            EmpresasBloqueadas = empresas.Count(e => e.Bloqueada),
            CertificadosVencendoEm15Dias = empresas.Count(e => e.CertificadoDiasRestantes is <= DiasAvisoVencimentoCertificado),
            ErrosHoje = logsHoje.Count(l => ContemPalavraChaveErro(l.Mensagem)),
            Empresas = empresas
        };
    }

    private static int? ObterDiasRestantesCertificado(Empresa empresa)
    {
        try
        {
            var certificado = ZeusConfiguracaoFactory.CarregarCertificado(empresa);
            return (certificado.NotAfter.Date - DateTime.Now.Date).Days;
        }
        catch
        {
            return null;
        }
    }

    private static bool ContemPalavraChaveErro(string? mensagem) =>
        mensagem is not null && PalavrasChaveErro.Any(p => mensagem.Contains(p, StringComparison.OrdinalIgnoreCase));
}
