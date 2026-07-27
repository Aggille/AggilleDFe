using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IDistribuicaoLoteService
{
    Task<ResultadoDistribuicaoLoteDto> ExecutarTodasAsync(bool execucaoManual, CancellationToken cancellationToken = default);
}
