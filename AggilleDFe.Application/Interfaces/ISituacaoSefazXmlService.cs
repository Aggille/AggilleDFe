namespace AggilleDFe.Application.Interfaces;

public interface ISituacaoSefazXmlService
{
    /// <summary>
    /// Consulta na SEFAZ a situação (consulta protocolo) de cada chave NFe/CTe
    /// informada, usando o certificado da empresa, e marca como cancelados na
    /// tabela XMLS os documentos que a SEFAZ devolver cancelados. Falhas são
    /// registradas em LOGS e não interrompem as demais chaves. Ver
    /// AggilleDFe.Infrastructure/Integrations/SITUACAO_SEFAZ_XML.md.
    /// </summary>
    Task AtualizarCancelamentosAsync(int empresaId, IReadOnlyCollection<string> chaves, CancellationToken cancellationToken = default);
}
