using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IDistribuicaoDfeService
{
    Task<(ResultadoBaixarXmlsDto? Resultado, string? Erro)> ExecutarAsync(int empresaId, bool execucaoManual, CancellationToken cancellationToken = default);

    Task<(ResultadoBaixarPorChaveDto? Resultado, string? Erro)> BaixarPorChaveAsync(int empresaId, string chave, CancellationToken cancellationToken = default);
}
