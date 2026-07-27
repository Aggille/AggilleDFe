namespace AggilleDFe.Application.Interfaces;

public interface IDanfeService
{
    Task<(string? Html, string? Erro)> ObterDanfeHtmlAsync(string chave, CancellationToken cancellationToken = default);
}
