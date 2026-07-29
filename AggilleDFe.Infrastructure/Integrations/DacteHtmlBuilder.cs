using System.Globalization;
using System.Net;
using System.Text;
using CTe.Classes;
using CTe.Classes.Informacoes.Tipos;
using DFe.Classes.Extensoes;

namespace AggilleDFe.Infrastructure.Integrations;

/// <summary>
/// Monta o DACTE em HTML pronto pra impressão a partir de um <see cref="cteProc"/> já
/// desserializado — não existe pacote Zeus.Net publicado pra DACTE (ver decisão em
/// `DACTE.md`), então o layout é montado à mão, nos mesmos moldes do DANFE em HTML
/// (`DanfeService`/`DANFE.md`): simplificado, sem código de barras (a chave aparece
/// agrupada em blocos de 4 dígitos, como costuma acompanhar o código de barras real),
/// cobrindo os campos comuns ao modal rodoviário — o mais frequente — sem entrar nos
/// detalhes específicos de cada modal (aéreo/aquaviário/ferroviário/dutoviário).
/// </summary>
internal static class DacteHtmlBuilder
{
    public static string Montar(cteProc doc, bool cancelada)
    {
        var infCte = doc.CTe.infCte;
        var ide = infCte.ide;
        var emit = infCte.emit;
        var vPrest = infCte.vPrest;
        var infProt = doc.protCTe.infProt;

        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
        sb.Append("<title>DACTE - ").Append(Html(infProt.chCTe)).Append("</title>");
        sb.Append("<style>").Append(Css).Append("</style></head><body>");

        sb.Append("<div class=\"folha\">");
        MontarCabecalho(sb, ide, emit, infProt, cancelada);
        MontarRemetenteDestinatario(sb, infCte.rem, infCte.dest);
        MontarPercurso(sb, ide);
        MontarValores(sb, vPrest);
        MontarObservacoes(sb, infCte.compl?.xObs);
        sb.Append("</div>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void MontarCabecalho(StringBuilder sb, CTe.Classes.Informacoes.Identificacao.ide ide, CTe.Classes.Informacoes.Emitente.emit emit, CTe.Classes.Protocolo.infProt infProt, bool cancelada)
    {
        sb.Append("<div class=\"cabecalho\">");
        sb.Append("<div class=\"emitente\">");
        sb.Append("<div class=\"emitente-nome\">").Append(Html(emit.xNome)).Append("</div>");
        if (emit.enderEmit is { } end)
        {
            sb.Append("<div>").Append(Html(end.xLgr)).Append(", ").Append(Html(end.nro))
              .Append(" - ").Append(Html(end.xBairro)).Append(" - ").Append(Html(end.xMun))
              .Append('/').Append(Html(end.UF.GetSiglaUfString())).Append(" - CEP ").Append(end.CEP.ToString("00000-000", CultureInfo.InvariantCulture)).Append("</div>");
        }
        sb.Append("<div>CNPJ: ").Append(FormatarCnpj(emit.CNPJ)).Append(" IE: ").Append(Html(emit.IE)).Append("</div>");
        sb.Append("</div>");

        sb.Append("<div class=\"titulo\">");
        sb.Append("<div class=\"titulo-dacte\">DACTE</div>");
        sb.Append("<div>Documento Auxiliar do Conhecimento de Transporte Eletrônico</div>");
        sb.Append("<div>Modelo 57 &nbsp; Série ").Append(ide.serie).Append(" &nbsp; Número ").Append(ide.nCT.ToString("D9", CultureInfo.InvariantCulture)).Append("</div>");
        sb.Append("<div>Emissão: ").Append(ide.dhEmi.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)).Append("</div>");
        if (cancelada)
        {
            sb.Append("<div class=\"chip-cancelada\">CANCELADA</div>");
        }
        sb.Append("</div>");
        sb.Append("</div>");

        sb.Append("<div class=\"chave\">");
        sb.Append("<div class=\"chave-label\">Chave de acesso</div>");
        sb.Append("<div class=\"chave-valor\">").Append(AgruparChave(infProt.chCTe)).Append("</div>");
        sb.Append("</div>");

        sb.Append("<div class=\"protocolo\">Protocolo de autorização: ").Append(Html(infProt.nProt))
          .Append(" &nbsp; Recebimento: ").Append(infProt.dhRecbto.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)).Append("</div>");

        sb.Append("<div class=\"linha3\">");
        sb.Append(Campo("Natureza da Operação", ide.natOp));
        sb.Append(Campo("Tipo do Serviço", FormatarTpServ(ide.tpServ)));
        sb.Append(Campo("Forma de Pagamento", FormatarForPag(ide.forPag)));
        sb.Append(Campo("Tomador do Serviço (quem paga o frete)", FormatarToma(ide.tomaBase3?.toma, ide.toma4)));
        sb.Append("</div>");
    }

    private static void MontarRemetenteDestinatario(StringBuilder sb, CTe.Classes.Informacoes.Remetente.rem? rem, CTe.Classes.Informacoes.Destinatario.dest? dest)
    {
        sb.Append("<div class=\"secao-titulo\">Remetente / Destinatário</div>");
        sb.Append("<div class=\"duas-colunas\">");

        sb.Append("<div class=\"caixa\">");
        sb.Append("<div class=\"caixa-titulo\">Remetente</div>");
        if (rem is not null)
        {
            sb.Append("<div>").Append(Html(rem.xNome)).Append("</div>");
            sb.Append("<div>").Append(FormatarCnpjOuCpf(rem.CNPJ, rem.CPF)).Append("</div>");
            if (rem.enderReme is { } er)
            {
                sb.Append("<div>").Append(Html(er.xLgr)).Append(", ").Append(Html(er.nro))
                  .Append(" - ").Append(Html(er.xMun)).Append('/').Append(Html(er.UF.GetSiglaUfString())).Append("</div>");
            }
        }
        sb.Append("</div>");

        sb.Append("<div class=\"caixa\">");
        sb.Append("<div class=\"caixa-titulo\">Destinatário</div>");
        if (dest is not null)
        {
            sb.Append("<div>").Append(Html(dest.xNome)).Append("</div>");
            sb.Append("<div>").Append(FormatarCnpjOuCpf(dest.CNPJ, dest.CPF)).Append("</div>");
            if (dest.enderDest is { } ed)
            {
                sb.Append("<div>").Append(Html(ed.xLgr)).Append(", ").Append(Html(ed.nro))
                  .Append(" - ").Append(Html(ed.xMun)).Append('/').Append(Html(ed.UF.GetSiglaUfString())).Append("</div>");
            }
        }
        sb.Append("</div>");

        sb.Append("</div>");
    }

    private static void MontarPercurso(StringBuilder sb, CTe.Classes.Informacoes.Identificacao.ide ide)
    {
        sb.Append("<div class=\"secao-titulo\">Percurso</div>");
        sb.Append("<div class=\"linha3\">");
        sb.Append(Campo("Início", $"{ide.xMunIni}/{ide.UFIni.GetSiglaUfString()}"));
        sb.Append(Campo("Fim", $"{ide.xMunFim}/{ide.UFFim.GetSiglaUfString()}"));
        sb.Append(Campo("CFOP", ide.CFOP.ToString(CultureInfo.InvariantCulture)));
        sb.Append("</div>");
    }

    private static void MontarValores(StringBuilder sb, CTe.Classes.Informacoes.Valores.vPrest vPrest)
    {
        sb.Append("<div class=\"secao-titulo\">Componentes do Valor da Prestação</div>");
        sb.Append("<table class=\"tabela\"><thead><tr><th>Nome</th><th class=\"valor\">Valor</th></tr></thead><tbody>");
        foreach (var comp in vPrest.Comp ?? [])
        {
            sb.Append("<tr><td>").Append(Html(comp.xNome)).Append("</td><td class=\"valor\">")
              .Append(comp.vComp.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))).Append("</td></tr>");
        }
        sb.Append("</tbody></table>");

        sb.Append("<div class=\"linha3\">");
        sb.Append(Campo("Valor a Receber", vPrest.vRec.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))));
        sb.Append(Campo("Valor Total da Prestação", vPrest.vTPrest.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))));
        sb.Append("</div>");
    }

    private static void MontarObservacoes(StringBuilder sb, string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(observacoes))
        {
            return;
        }

        sb.Append("<div class=\"secao-titulo\">Observações</div>");
        sb.Append("<div class=\"observacoes\">").Append(Html(observacoes)).Append("</div>");
    }

    private static string Campo(string rotulo, string? valor) =>
        $"<div class=\"campo\"><div class=\"campo-rotulo\">{Html(rotulo)}</div><div class=\"campo-valor\">{Html(valor)}</div></div>";

    private static string AgruparChave(string? chave)
    {
        if (string.IsNullOrEmpty(chave))
        {
            return string.Empty;
        }

        var blocos = new List<string>();
        for (var i = 0; i < chave.Length; i += 4)
        {
            blocos.Add(chave.Substring(i, Math.Min(4, chave.Length - i)));
        }

        return string.Join(' ', blocos);
    }

    private static string FormatarCnpj(string? cnpj) =>
        !string.IsNullOrEmpty(cnpj) && cnpj.Length == 14
            ? $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}"
            : cnpj ?? string.Empty;

    private static string FormatarCnpjOuCpf(string? cnpj, string? cpf) =>
        !string.IsNullOrEmpty(cnpj) ? $"CNPJ: {FormatarCnpj(cnpj)}" : !string.IsNullOrEmpty(cpf) ? $"CPF: {cpf}" : string.Empty;

    private static string FormatarTpServ(tpServ tpServ) => tpServ switch
    {
        tpServ.normal => "Normal",
        tpServ.subcontratacao => "Subcontratação",
        tpServ.redespacho => "Redespacho",
        tpServ.redespachoIntermediario => "Redespacho Intermediário",
        tpServ.servicoVinculadoMultimodal => "Serviço Vinculado a Multimodal",
        tpServ.transportePessoas => "Transporte de Pessoas",
        tpServ.transporteValores => "Transporte de Valores",
        tpServ.excessoBagagem => "Excesso de Bagagem",
        _ => tpServ.ToString()
    };

    private static string FormatarForPag(forPag? valor) => valor switch
    {
        forPag.Pago => "Pago",
        forPag.Apagar => "A pagar",
        forPag.Outros => "Outros",
        _ => string.Empty
    };

    private static string FormatarToma(toma? valor, CTe.Classes.Informacoes.Identificacao.toma4? toma4)
    {
        var rotulo = valor switch
        {
            toma.Remetente => "Remetente",
            toma.Expedidor => "Expedidor",
            toma.Recebedor => "Recebedor",
            toma.Destinatario => "Destinatário",
            toma.Outros => "Outros",
            _ => string.Empty
        };

        if (valor == toma.Outros && toma4 is not null)
        {
            rotulo += !string.IsNullOrEmpty(toma4.xNome) ? $" ({toma4.xNome})" : string.Empty;
        }

        return rotulo;
    }

    private static string Html(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

    private const string Css = """
        @page { size: A4; margin: 12mm; }
        * { box-sizing: border-box; }
        body { font-family: Arial, Helvetica, sans-serif; font-size: 12px; color: #111; margin: 0; }
        .folha { max-width: 190mm; margin: 0 auto; }
        .cabecalho { display: flex; justify-content: space-between; border: 1px solid #333; padding: 8px; gap: 12px; }
        .emitente-nome { font-weight: bold; font-size: 14px; margin-bottom: 4px; }
        .titulo { text-align: center; min-width: 220px; }
        .titulo-dacte { font-weight: bold; font-size: 20px; }
        .chip-cancelada { display: inline-block; margin-top: 6px; padding: 2px 10px; background: #b71c1c; color: #fff; font-weight: bold; border-radius: 3px; }
        .chave { border: 1px solid #333; border-top: none; padding: 8px; text-align: center; }
        .chave-label { font-size: 10px; color: #555; }
        .chave-valor { font-family: "Courier New", monospace; font-size: 14px; letter-spacing: 1px; margin-top: 2px; }
        .protocolo { border: 1px solid #333; border-top: none; padding: 6px 8px; font-size: 11px; }
        .secao-titulo { background: #eee; border: 1px solid #333; border-top: none; padding: 4px 8px; font-weight: bold; font-size: 11px; }
        .linha3 { display: flex; border: 1px solid #333; border-top: none; }
        .linha3 .campo { flex: 1; padding: 6px 8px; border-right: 1px solid #ccc; }
        .linha3 .campo:last-child { border-right: none; }
        .campo-rotulo { font-size: 9px; color: #555; text-transform: uppercase; }
        .campo-valor { font-size: 12px; margin-top: 2px; }
        .duas-colunas { display: flex; border: 1px solid #333; border-top: none; }
        .caixa { flex: 1; padding: 8px; border-right: 1px solid #ccc; }
        .caixa:last-child { border-right: none; }
        .caixa-titulo { font-weight: bold; font-size: 10px; margin-bottom: 4px; }
        .tabela { width: 100%; border-collapse: collapse; border: 1px solid #333; border-top: none; }
        .tabela th, .tabela td { border-bottom: 1px solid #ccc; padding: 4px 8px; text-align: left; font-size: 11px; }
        .tabela .valor { text-align: right; }
        .observacoes { border: 1px solid #333; border-top: none; padding: 8px; font-size: 11px; white-space: pre-wrap; }
        @media print { body { -webkit-print-color-adjust: exact; print-color-adjust: exact; } }
        """;
}
