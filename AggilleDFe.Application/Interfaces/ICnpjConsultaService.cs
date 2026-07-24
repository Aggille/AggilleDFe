using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface ICnpjConsultaService
{
    /// <returns>null se o CNPJ não foi encontrado na base consultada.</returns>
    Task<CnpjConsultaResultadoDto?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default);
}
