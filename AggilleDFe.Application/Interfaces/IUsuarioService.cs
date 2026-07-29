using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default);
    Task<UsuarioDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <returns>Id do usuário incluído, ou dicionário de erros de validação (campo -&gt; mensagens).</returns>
    Task<(int? Id, IReadOnlyDictionary<string, string[]>? Erros)> IncluirAsync(UsuarioDto dto, CancellationToken cancellationToken = default);

    /// <returns>Encontrado=false se o id não existir; Erros preenchido se a validação falhar; caso contrário, sucesso.</returns>
    Task<(bool Encontrado, IReadOnlyDictionary<string, string[]>? Erros)> AtualizarAsync(int id, UsuarioDto dto, CancellationToken cancellationToken = default);

    /// <returns>false se o id não existir.</returns>
    Task<bool> ExcluirAsync(int id, CancellationToken cancellationToken = default);
}
