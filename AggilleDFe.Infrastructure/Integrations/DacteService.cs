using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;
using DFe.Utils;

namespace AggilleDFe.Infrastructure.Integrations;

public class DacteService(IXmlRepository xmlRepository) : IDacteService
{
    public async Task<(string? Html, string? Erro)> ObterDacteHtmlAsync(string chave, CancellationToken cancellationToken = default)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (xml is null)
        {
            return (null, "Chave não encontrada.");
        }

        if (xml.Modelo != "57")
        {
            return (null, "DACTE disponível apenas para CTe.");
        }

        string conteudoXml;
        if (!string.IsNullOrEmpty(xml.ConteudoXml))
        {
            conteudoXml = xml.ConteudoXml;
        }
        else if (string.IsNullOrWhiteSpace(xml.NomeXml))
        {
            return (null, "O XML completo desse documento ainda não foi baixado (só o resumo está disponível).");
        }
        else if (!File.Exists(xml.NomeXml))
        {
            return (null, "Arquivo XML não encontrado no disco.");
        }
        else
        {
            conteudoXml = await File.ReadAllTextAsync(xml.NomeXml, cancellationToken);
        }

        try
        {
            var cteProc = FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>(conteudoXml);
            var html = DacteHtmlBuilder.Montar(cteProc, xml.Cancelada == "S");
            return (html, null);
        }
        catch (Exception ex)
        {
            return (null, $"Falha ao gerar o DACTE: {ex.Message}");
        }
    }
}
