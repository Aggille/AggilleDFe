using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;
using DFe.Utils;
using NFe.Danfe.Html;
using NFe.Danfe.Html.CrossCutting;
using NFe.Danfe.Html.Dominio;

namespace AggilleDFe.Infrastructure.Integrations;

public class DanfeService(IXmlRepository xmlRepository) : IDanfeService
{
    public async Task<(string? Html, string? Erro)> ObterDanfeHtmlAsync(string chave, CancellationToken cancellationToken = default)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (xml is null)
        {
            return (null, "Chave não encontrada.");
        }

        if (xml.Modelo != "55")
        {
            return (null, "DANFE disponível apenas para NFe.");
        }

        if (string.IsNullOrWhiteSpace(xml.NomeXml))
        {
            return (null, "O XML completo desse documento ainda não foi baixado (só o resumo está disponível).");
        }

        if (!File.Exists(xml.NomeXml))
        {
            return (null, "Arquivo XML não encontrado no disco.");
        }

        try
        {
            var conteudoXml = await File.ReadAllTextAsync(xml.NomeXml, cancellationToken);
            var nfeProc = FuncoesXml.XmlStringParaClasse<NFe.Classes.nfeProc>(conteudoXml);

            var status = xml.Cancelada == "S" ? Status.Cancelada : Status.Autorizada;
            var danfeNFe = new DanfeNFe(nfeProc.NFe, status, xml.Protocolo ?? string.Empty, string.Empty, new Issqn(), string.Empty);
            var danfeHtml = new DanfeNfeHtml2(danfeNFe);
            var documento = await danfeHtml.ObterDocHtmlAsync();

            return (documento.Html, null);
        }
        catch (Exception ex)
        {
            return (null, $"Falha ao gerar o DANFE: {ex.Message}");
        }
    }
}
