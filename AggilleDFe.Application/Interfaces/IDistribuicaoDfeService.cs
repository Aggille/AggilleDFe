using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IDistribuicaoDfeService
{
    Task<(ResultadoBaixarXmlsDto? Resultado, string? Erro)> ExecutarAsync(int empresaId, bool execucaoManual, CancellationToken cancellationToken = default);
}
