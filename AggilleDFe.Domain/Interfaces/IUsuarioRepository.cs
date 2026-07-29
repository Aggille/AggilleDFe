using AggilleDFe.Domain.Entities;

namespace AggilleDFe.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<bool> ExisteComLoginAsync(string login, int? idExcluir = null, CancellationToken cancellationToken = default);
    Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default);
    Task IncluirAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task ExcluirAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
