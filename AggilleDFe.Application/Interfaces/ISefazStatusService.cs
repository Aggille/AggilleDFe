using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface ISefazStatusService
{
    Task<(StatusSefazResultadoDto? Resultado, string? Erro)> ConsultarStatusAsync(int empresaId, CancellationToken cancellationToken = default);
}
