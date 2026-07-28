namespace AggilleDFe.Application.Interfaces;

public interface IDacteService
{
    Task<(string? Html, string? Erro)> ObterDacteHtmlAsync(string chave, CancellationToken cancellationToken = default);
}
