namespace AggilleDFe.Application.Interfaces;

public interface IXmlExportService
{
    /// <param name="usarDataDownload">
    /// false (padrão): filtra pela data de emissão do documento. true: filtra
    /// pela data em que o XML foi baixado pela plataforma.
    /// </param>
    /// <returns>Zip pronto (bytes) e o nome sugerido do arquivo, ou erro se não houver XMLs no período/empresa informados.</returns>
    Task<(byte[]? Zip, string? NomeArquivo, string? Erro)> ExportarZipAsync(int ano, int mes, int? empresaId, bool usarDataDownload = false, CancellationToken cancellationToken = default);
}
