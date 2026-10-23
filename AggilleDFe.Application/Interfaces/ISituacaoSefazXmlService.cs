namespace AggilleDFe.Application.Interfaces;

public interface ISituacaoSefazXmlService
{
    /// <summary>
    /// Consulta na SEFAZ a situação (consulta protocolo) de cada chave NFe/CTe
    /// informada, usando o certificado da empresa, e grava na tabela XMLS os
    /// documentos que a SEFAZ devolver cancelados ou recusados pelo destinatário
    /// (Desconhecimento da Operação / Operação Não Realizada). Falhas são
    /// registradas em LOGS e não interrompem as demais chaves. Ver
    /// AggilleDFe.Infrastructure/Integrations/SITUACAO_SEFAZ_XML.md.
    /// </summary>
    Task AtualizarSituacoesAsync(int empresaId, IReadOnlyCollection<string> chaves, CancellationToken cancellationToken = default);
}
