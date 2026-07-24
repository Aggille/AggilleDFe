using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IEmpresaService
{
    Task<IReadOnlyList<EmpresaDto>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default);
    Task<EmpresaDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <returns>Id da empresa incluída, ou dicionário de erros de validação (campo -&gt; mensagens).</returns>
    Task<(int? Id, IReadOnlyDictionary<string, string[]>? Erros)> IncluirAsync(EmpresaDto dto, CancellationToken cancellationToken = default);

    /// <returns>Encontrado=false se o id não existir; Erros preenchido se a validação falhar; caso contrário, sucesso.</returns>
    Task<(bool Encontrado, IReadOnlyDictionary<string, string[]>? Erros)> AtualizarAsync(int id, EmpresaDto dto, CancellationToken cancellationToken = default);

    /// <returns>false se o id não existir.</returns>
    Task<bool> ExcluirAsync(int id, CancellationToken cancellationToken = default);
}
