using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Application.Services;

public class LogService(ILogRepository repository) : ILogService
{
    public async Task<IReadOnlyList<LogDto>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, CancellationToken cancellationToken = default)
    {
        var logs = await repository.PesquisarAsync(empresaId, dataInicial, dataFinal, cancellationToken);
        return logs.Select(ParaDto).ToList();
    }

    private static LogDto ParaDto(Log log) => new()
    {
        Id = log.Id,
        Data = log.Data,
        HoraInicio = log.HoraInicio,
        HoraFinal = log.HoraFinal,
        EmpresaId = log.EmpresaId,
        QuantidadeXmls = log.QuantidadeXmls,
        Mensagem = log.Mensagem,
        XmlId = log.XmlId,
        Chave = log.Chave
    };
}
