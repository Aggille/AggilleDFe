using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> ObterAsync(CancellationToken cancellationToken = default);
}
