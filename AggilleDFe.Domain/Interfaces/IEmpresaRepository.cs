using AggilleDFe.Domain.Entities;

namespace AggilleDFe.Domain.Interfaces;

public interface IEmpresaRepository
{
    Task<IReadOnlyList<Empresa>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default);
    Task<Empresa?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteComCnpjAsync(string cnpj, int? idExcluir = null, CancellationToken cancellationToken = default);
    Task IncluirAsync(Empresa empresa, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Empresa empresa, CancellationToken cancellationToken = default);
    Task ExcluirAsync(Empresa empresa, CancellationToken cancellationToken = default);
}
