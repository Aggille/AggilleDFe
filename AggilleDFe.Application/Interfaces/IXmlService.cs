using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IXmlService
{
    Task<IReadOnlyList<XmlDto>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, DateOnly? emissaoInicial = null, DateOnly? emissaoFinal = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<XmlDto>? Xmls, string? Erro)> PesquisarPorEmpresaAsync(DateOnly? dataInicial, DateOnly? dataFinal, string? cnpjEmpresa, string? tipo, bool usarDataDownload = false, bool consultaSefaz = false, CancellationToken cancellationToken = default);
}
