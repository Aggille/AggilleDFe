using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IXmlService
{
    Task<IReadOnlyList<XmlDto>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, CancellationToken cancellationToken = default);
}
