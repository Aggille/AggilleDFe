using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Infrastructure.Storage;

public class XmlArquivoService(IXmlRepository xmlRepository) : IXmlArquivoService
{
    public async Task<(byte[]? Conteudo, string? Erro)> ObterXmlBrutoAsync(string chave, CancellationToken cancellationToken = default)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (xml is null)
        {
            return (null, "Chave não encontrada.");
        }

        if (string.IsNullOrWhiteSpace(xml.NomeXml))
        {
            return (null, "O XML completo desse documento ainda não foi baixado (só o resumo está disponível).");
        }

        if (!File.Exists(xml.NomeXml))
        {
            return (null, "Arquivo XML não encontrado no disco.");
        }

        var conteudo = await File.ReadAllBytesAsync(xml.NomeXml, cancellationToken);
        return (conteudo, null);
    }
}
