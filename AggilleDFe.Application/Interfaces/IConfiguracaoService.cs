using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IConfiguracaoService
{
    Task<ConfiguracaoDto?> ObterAsync(CancellationToken cancellationToken = default);

    /// <returns>null se salvou com sucesso; dicionário de erros de validação (campo -&gt; mensagens) caso contrário.</returns>
    Task<IReadOnlyDictionary<string, string[]>?> SalvarAsync(ConfiguracaoDto dto, CancellationToken cancellationToken = default);

    Task<string?> ObterVersaoAsync(CancellationToken cancellationToken = default);
}
