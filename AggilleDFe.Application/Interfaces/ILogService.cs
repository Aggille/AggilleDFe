using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface ILogService
{
    Task<IReadOnlyList<LogDto>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, CancellationToken cancellationToken = default);
}
