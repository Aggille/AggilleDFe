using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IXmlImportService
{
    Task<(ResultadoImportacaoXmlsDto? Resultado, string? Erro)> ImportarPastaAsync(string pasta, CancellationToken cancellationToken = default);
}
