namespace AggilleDFe.Application.Interfaces;

public interface IManifestacaoService
{
    Task<(bool Sucesso, string? Erro)> ManifestarCienciaAsync(string chave, CancellationToken cancellationToken = default);
    Task<(bool Sucesso, string? Erro)> ManifestarDesconhecimentoAsync(string chave, string motivo, CancellationToken cancellationToken = default);
    Task<(bool Sucesso, string? Erro)> ManifestarNaoRealizadaAsync(string chave, string motivo, CancellationToken cancellationToken = default);
}
