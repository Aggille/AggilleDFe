using AggilleDFe.Domain.Entities;

namespace AggilleDFe.Domain.Interfaces;

public interface IConfiguracaoRepository
{
    Task<Configuracao?> ObterAsync(CancellationToken cancellationToken = default);
    Task SalvarAsync(Configuracao configuracao, CancellationToken cancellationToken = default);
}
