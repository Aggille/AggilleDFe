using AggilleDFe.Domain.Entities;

namespace AggilleDFe.Domain.Interfaces;

public interface ILogRepository
{
    Task IncluirAsync(Log log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Log>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, CancellationToken cancellationToken = default);
}
