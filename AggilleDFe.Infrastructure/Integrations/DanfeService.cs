using System.Globalization;
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
            // Bug do próprio Zeus.Net.NFe.Danfe.Html: FormatarNumeroDanfe()
            // (NFe.Danfe.Html/CrossCutting/Utils.cs) monta a string do valor
            // com vírgula decimal e depois faz `double.TryParse(str, out
            // var result)` SEM especificar cultura - se a cultura ambiente
            // da thread não usa vírgula como separador decimal (ex.:
            // Invariant), a vírgula é lida como separador de milhar e o
            // valor sai 100x maior (ex.: R$ 573,92 vira R$ 57.392,17 vira
            // R$ 5.739.217,00). Forçar pt-BR aqui, só ao redor da geração do
            // DANFE, faz esse parse interno do Zeus funcionar certo.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");

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
