using System.Security.Cryptography.X509Certificates;
using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using DFe.Utils;
using Microsoft.Extensions.Configuration;
using NFe.Classes.Servicos.Tipos;
using NFe.Servicos;
using Shared.DFe.Utils;
using CteConfiguracaoServico = CTe.Classes.ConfiguracaoServico;
using CteLote = CTe.Classes.Servicos.DistribuicaoDFe.loteDistDFeInt;
using NfeConfiguracaoServico = NFe.Utils.ConfiguracaoServico;
using NfeLote = NFe.Classes.Servicos.DistribuicaoDFe.loteDistDFeInt;
using ServicoCTeDistribuicaoDFe = CTe.Servicos.DistribuicaoDFe.ServicoCTeDistribuicaoDFe;

namespace AggilleDFe.Infrastructure.Integrations;

public class DistribuicaoDfeService(
    IEmpresaRepository empresaRepository,
    IXmlRepository xmlRepository,
    ILogRepository logRepository,
    IConfiguration configuration) : IDistribuicaoDfeService
{
    private const int CStatDocumentosLocalizados = 138;
    private const int CStatNenhumDocumentoLocalizado = 137;
    private const int CStatLoteEventoProcessado = 128;
    private const int TpEventoCancelamento = 110111;

    public async Task<(ResultadoBaixarXmlsDto? Resultado, string? Erro)> ExecutarAsync(int empresaId, bool execucaoManual, CancellationToken cancellationToken = default)
    {
        var empresa = await empresaRepository.ObterPorIdAsync(empresaId, cancellationToken);
        if (empresa is null)
        {
            return (null, "Empresa não encontrada.");
        }

        try
        {
            var certificado = ZeusConfiguracaoFactory.CarregarCertificado(empresa);
            var diretorioSchemas = configuration["SchemasPath"] ?? "SCHEMAS";
            var configuracaoNfe = ZeusConfiguracaoFactory.Criar(empresa, diretorioSchemas);
            var configuracaoCte = ZeusConfiguracaoFactory.CriarCte(empresa, diretorioSchemas);

            var (baixadosNfe, eventosNfe) = await ExecutarNfeAsync(empresa, configuracaoNfe, certificado, cancellationToken);
            var (baixadosCte, eventosCte) = await ExecutarCteAsync(empresa, configuracaoCte, certificado, cancellationToken);

            return (new ResultadoBaixarXmlsDto
            {
                XmlsBaixadosNfe = baixadosNfe,
                XmlsBaixadosCte = baixadosCte,
                EventosProcessados = eventosNfe + eventosCte,
                Mensagem = $"{baixadosNfe} XML(s) de NFe e {baixadosCte} XML(s) de CTe baixados."
            }, null);
        }
        catch (InvalidOperationException ex)
        {
            await LogarAsync(empresa.Id, $"Falha ao baixar XMLs: {ex.Message}", cancellationToken: cancellationToken);
            return (null, ex.Message);
        }
        catch (Exception ex)
        {
            await LogarAsync(empresa.Id, $"Falha ao baixar XMLs: {ex.Message}", cancellationToken: cancellationToken);
            return (null, $"Falha ao baixar XMLs: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------------
    // NFe
    // ----------------------------------------------------------------------------

    private async Task<(int Baixados, int Eventos)> ExecutarNfeAsync(Empresa empresa, NfeConfiguracaoServico configuracao, X509Certificate2 certificado, CancellationToken cancellationToken)
    {
        var baixados = 0;
        var eventos = 0;
        var horaInicio = TimeOnly.FromDateTime(DateTime.Now);

        using var servicoNfe = new ServicosNFe(configuracao, certificado);
        var ultNsu = empresa.UltimoNsu ?? 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var retorno = servicoNfe.NfeDistDFeInteresse(ufAutor: empresa.Uf!, documento: empresa.Cnpj!, ultNSU: ultNsu.ToString());
            var status = retorno.Retorno;

            if (status.cStat == CStatNenhumDocumentoLocalizado)
            {
                await LogarAsync(empresa.Id, "NFe: nenhum novo documento localizado na SEFAZ.", cancellationToken: cancellationToken);
                break;
            }

            if (status.cStat != CStatDocumentosLocalizados)
            {
                await LogarAsync(empresa.Id, $"NFe: retorno inesperado da SEFAZ (cStat {status.cStat} - {status.xMotivo}).", cancellationToken: cancellationToken);
                break;
            }

            foreach (var item in status.loteDistDFeInt ?? [])
            {
                try
                {
                    var (novoXml, evento) = await ProcessarItemNfeAsync(empresa, servicoNfe, item, cancellationToken);
                    if (novoXml) baixados++;
                    if (evento) eventos++;
                }
                catch (Exception ex)
                {
                    await LogarAsync(empresa.Id, $"NFe: erro ao processar NSU {item.NSU}: {ex.Message}", cancellationToken: cancellationToken);
                }
            }

            empresa.UltimoNsu = (int)status.ultNSU;
            await empresaRepository.AtualizarAsync(empresa, cancellationToken);

            if (status.ultNSU >= status.maxNSU)
            {
                break;
            }

            ultNsu = (int)status.ultNSU;
            await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);
        }

        await LogarAsync(empresa.Id, $"NFe: execução concluída. {baixados} XML(s) baixado(s).",
            quantidadeXmls: baixados, horaInicio: horaInicio, horaFinal: TimeOnly.FromDateTime(DateTime.Now), cancellationToken: cancellationToken);

        return (baixados, eventos);
    }

    private async Task<(bool NovoXmlBaixado, bool EventoProcessado)> ProcessarItemNfeAsync(Empresa empresa, ServicosNFe servicoNfe, NfeLote item, CancellationToken cancellationToken)
    {
        if (item.ResNFe is not null)
        {
            await ProcessarResumoNfeAsync(empresa, servicoNfe, item.ResNFe, cancellationToken);
            return (false, true);
        }

        if (item.ResEvento is not null)
        {
            await ProcessarResumoEventoNfeAsync(empresa, item.ResEvento, cancellationToken);
            return (false, true);
        }

        if (item.NfeProc is not null)
        {
            await ProcessarDocumentoCompletoNfeAsync(empresa, item, cancellationToken);
            return (true, false);
        }

        if (item.ProcEventoNFe is not null)
        {
            await ProcessarEventoCompletoNfeAsync(empresa, item.ProcEventoNFe, cancellationToken);
            return (false, true);
        }

        await LogarAsync(empresa.Id, $"NFe: item NSU {item.NSU} com schema não reconhecido ({item.schema}).", cancellationToken: cancellationToken);
        return (false, false);
    }

    private async Task ProcessarResumoNfeAsync(Empresa empresa, ServicosNFe servicoNfe, NFe.Classes.Servicos.DistribuicaoDFe.Schemas.resNFe resumo, CancellationToken cancellationToken)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(resumo.chNFe, cancellationToken);
        var novo = xml is null;
        xml ??= new Xml { Chave = resumo.chNFe, EmpresaId = empresa.Id, Modelo = "55" };

        xml.Emissao = DateOnly.FromDateTime(resumo.dhEmi);
        xml.FornecedorCnpj = resumo.CNPJ;
        xml.ValorTotal = resumo.vNF;
        xml.Situacao = "Resumo";
        xml.Schema = "resNFe";

        if (novo) await xmlRepository.IncluirAsync(xml, cancellationToken);
        else await xmlRepository.AtualizarAsync(xml, cancellationToken);

        await LogarAsync(empresa.Id, "NFe: resumo recebido.", chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);

        if (empresa.Manifesta != "S")
        {
            return;
        }

        try
        {
            var retornoManifestacao = servicoNfe.RecepcaoEventoManifestacaoDestinatario(
                idlote: 1,
                sequenciaEvento: 1,
                chaveNFe: resumo.chNFe,
                nFeTipoEventoManifestacaoDestinatario: NFeTipoEvento.TeMdCienciaDaOperacao,
                cpfcnpj: empresa.Cnpj!,
                justificativa: null,
                dhEvento: null);

            var loteCStat = retornoManifestacao.Retorno?.cStat;
            if (loteCStat == CStatLoteEventoProcessado)
            {
                xml.Situacao = "Ciência realizada";
                xml.DataCiencia = DateOnly.FromDateTime(DateTime.Now);
                await xmlRepository.AtualizarAsync(xml, cancellationToken);
            }

            await LogarAsync(empresa.Id,
                $"NFe: manifestação de Ciência da Operação solicitada (cStat {loteCStat} - {retornoManifestacao.Retorno?.xMotivo}).",
                chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await LogarAsync(empresa.Id, $"NFe: erro ao manifestar ciência: {ex.Message}", chave: resumo.chNFe, xmlId: xml.Id, cancellationToken: cancellationToken);
        }
    }

    private async Task ProcessarResumoEventoNfeAsync(Empresa empresa, NFe.Classes.Servicos.DistribuicaoDFe.Schemas.resEvento evento, CancellationToken cancellationToken)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(evento.chNFe, cancellationToken);
        var ehCancelamento = evento.tpEvento == TpEventoCancelamento.ToString();

        if (ehCancelamento && xml is not null)
        {
            xml.Cancelada = "S";
            xml.DataCancelamento = DateOnly.FromDateTime(DateTime.Now);
            xml.MotivoCancelamento = evento.xEvento;
            await xmlRepository.AtualizarAsync(xml, cancellationToken);
            await LogarAsync(empresa.Id, "NFe: cancelamento registrado (resumo de evento).", chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
            return;
        }

        await LogarAsync(empresa.Id, $"NFe: resumo de evento recebido ({evento.xEvento}).", chave: evento.chNFe, xmlId: xml?.Id, cancellationToken: cancellationToken);
    }

    private async Task ProcessarDocumentoCompletoNfeAsync(Empresa empresa, NfeLote item, CancellationToken cancellationToken)
    {
        var doc = item.NfeProc!;
        var infNFe = doc.NFe.infNFe;
        var infProt = doc.protNFe.infProt;
        var chave = infProt.chNFe;

        var conteudoXml = Compressao.Unzip(item.XmlNfe);
        var caminho = SalvarXml(empresa.PastaXml, empresa.Cnpj!, infNFe.ide.dhEmi.DateTime, "NFe", chave, conteudoXml);

        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        var novo = xml is null;
        xml ??= new Xml { Chave = chave, EmpresaId = empresa.Id };

        xml.Protocolo = infProt.nProt;
        xml.Emissao = DateOnly.FromDateTime(infNFe.ide.dhEmi.DateTime);
        xml.DataDownload = DateOnly.FromDateTime(DateTime.Now);
        xml.FornecedorNome = infNFe.emit.xNome;
        xml.FornecedorCnpj = infNFe.emit.CNPJ;
        xml.FornecedorCidade = infNFe.emit.enderEmit.xMun;
        xml.FornecedorUf = infNFe.emit.enderEmit.UF.ToString();
        xml.ValorTotal = infNFe.total.ICMSTot.vNF;
        xml.ValorIcms = infNFe.total.ICMSTot.vICMS;
        xml.StatusNfe = infProt.cStat;
        xml.MensagemNfe = infProt.xMotivo;
        xml.NomeXml = caminho;
        xml.Numero = (int)infNFe.ide.nNF;
        xml.Serie = infNFe.ide.serie.ToString();
        xml.Modelo = "55";
        xml.Schema = item.schema;
        xml.Situacao = "Documento completo";

        if (novo) await xmlRepository.IncluirAsync(xml, cancellationToken);
        else await xmlRepository.AtualizarAsync(xml, cancellationToken);

        await LogarAsync(empresa.Id, "NFe: XML baixado e salvo.", chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
    }

    private async Task ProcessarEventoCompletoNfeAsync(Empresa empresa, NFe.Classes.Servicos.DistribuicaoDFe.Schemas.procEventoNFe evt, CancellationToken cancellationToken)
    {
        var infEvento = evt.retEvento.infEvento;
        var chave = infEvento.chNFe;
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        var ehCancelamento = infEvento.tpEvento == TpEventoCancelamento;

        if (ehCancelamento && xml is not null)
        {
            xml.Cancelada = "S";
            xml.DataCancelamento = DateOnly.FromDateTime(DateTime.Now);
            xml.MotivoCancelamento = infEvento.xMotivo;
            await xmlRepository.AtualizarAsync(xml, cancellationToken);
            await LogarAsync(empresa.Id, "NFe: cancelamento registrado (evento completo).", chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
            return;
        }

        await LogarAsync(empresa.Id, $"NFe: evento completo recebido ({infEvento.xEvento}).", chave: chave, xmlId: xml?.Id, cancellationToken: cancellationToken);
    }

    // ----------------------------------------------------------------------------
    // CTe
    // ----------------------------------------------------------------------------

    private async Task<(int Baixados, int Eventos)> ExecutarCteAsync(Empresa empresa, CteConfiguracaoServico configuracao, X509Certificate2 certificado, CancellationToken cancellationToken)
    {
        var baixados = 0;
        var eventos = 0;
        var horaInicio = TimeOnly.FromDateTime(DateTime.Now);

        var servicoCte = new ServicoCTeDistribuicaoDFe(configuracao, certificado);
        var ultNsu = empresa.UltimoNsuCte ?? 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var retorno = servicoCte.CTeDistDFeInteresse(ufAutor: empresa.Uf!, documento: empresa.Cnpj!, ultNSU: ultNsu.ToString(), nSU: "0", configuracaoServico: configuracao);
            var status = retorno.Retorno;

            if (status.cStat == CStatNenhumDocumentoLocalizado)
            {
                await LogarAsync(empresa.Id, "CTe: nenhum novo documento localizado na SEFAZ.", cancellationToken: cancellationToken);
                break;
            }

            if (status.cStat != CStatDocumentosLocalizados)
            {
                await LogarAsync(empresa.Id, $"CTe: retorno inesperado da SEFAZ (cStat {status.cStat} - {status.xMotivo}).", cancellationToken: cancellationToken);
                break;
            }

            foreach (var item in status.loteDistDFeInt ?? [])
            {
                try
                {
                    var (novoXml, evento) = await ProcessarItemCteAsync(empresa, item, cancellationToken);
                    if (novoXml) baixados++;
                    if (evento) eventos++;
                }
                catch (Exception ex)
                {
                    await LogarAsync(empresa.Id, $"CTe: erro ao processar NSU {item.NSU}: {ex.Message}", cancellationToken: cancellationToken);
                }
            }

            empresa.UltimoNsuCte = (int)status.ultNSU;
            await empresaRepository.AtualizarAsync(empresa, cancellationToken);

            if (status.ultNSU >= status.maxNSU)
            {
                break;
            }

            ultNsu = (int)status.ultNSU;
            await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);
        }

        await LogarAsync(empresa.Id, $"CTe: execução concluída. {baixados} XML(s) baixado(s).",
            quantidadeXmls: baixados, horaInicio: horaInicio, horaFinal: TimeOnly.FromDateTime(DateTime.Now), cancellationToken: cancellationToken);

        return (baixados, eventos);
    }

    private async Task<(bool NovoXmlBaixado, bool EventoProcessado)> ProcessarItemCteAsync(Empresa empresa, CteLote item, CancellationToken cancellationToken)
    {
        if (item.XmlNfe is null)
        {
            await LogarAsync(empresa.Id, $"CTe: item NSU {item.NSU} sem conteúdo.", cancellationToken: cancellationToken);
            return (false, false);
        }

        var conteudo = Compressao.Unzip(item.XmlNfe).RemoverDeclaracaoXml();

        if (conteudo.StartsWith("<cteProc", StringComparison.Ordinal))
        {
            await ProcessarDocumentoCompletoCteAsync(empresa, item.schema, conteudo, cancellationToken);
            return (true, false);
        }

        if (conteudo.StartsWith("<procEventoCTe", StringComparison.Ordinal))
        {
            await ProcessarEventoCompletoCteAsync(empresa, conteudo, cancellationToken);
            return (false, true);
        }

        await ProcessarResumoGenericoCteAsync(empresa, item.schema, conteudo, cancellationToken);
        return (false, true);
    }

    private async Task ProcessarDocumentoCompletoCteAsync(Empresa empresa, string schema, string conteudoXml, CancellationToken cancellationToken)
    {
        var doc = FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>(conteudoXml);
        var infCte = doc.CTe.infCte;
        var infProt = doc.protCTe.infProt;
        var chave = infProt.chCTe;

        var caminho = SalvarXml(empresa.PastaXml, empresa.Cnpj!, infCte.ide.dhEmi.DateTime, "CTe", chave, conteudoXml);

        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        var novo = xml is null;
        xml ??= new Xml { Chave = chave, EmpresaId = empresa.Id };

        xml.Protocolo = infProt.nProt;
        xml.Emissao = DateOnly.FromDateTime(infCte.ide.dhEmi.DateTime);
        xml.DataDownload = DateOnly.FromDateTime(DateTime.Now);
        xml.FornecedorNome = infCte.emit.xNome;
        xml.FornecedorCnpj = infCte.emit.CNPJ;
        xml.FornecedorCidade = infCte.emit.enderEmit.xMun;
        xml.FornecedorUf = infCte.emit.enderEmit.UF.ToString();
        xml.ValorTotal = infCte.vPrest.vTPrest;
        xml.StatusNfe = infProt.cStat;
        xml.MensagemNfe = infProt.xMotivo;
        xml.NomeXml = caminho;
        xml.Numero = (int)infCte.ide.nCT;
        xml.Serie = infCte.ide.serie.ToString();
        xml.Modelo = "57";
        xml.Schema = schema;
        xml.Situacao = "Documento completo";

        if (novo) await xmlRepository.IncluirAsync(xml, cancellationToken);
        else await xmlRepository.AtualizarAsync(xml, cancellationToken);

        await LogarAsync(empresa.Id, "CTe: XML baixado e salvo.", chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
    }

    private async Task ProcessarEventoCompletoCteAsync(Empresa empresa, string conteudoXml, CancellationToken cancellationToken)
    {
        var evt = FuncoesXml.XmlStringParaClasse<CTe.Classes.Servicos.DistribuicaoDFe.Schemas.procEventoCTe>(conteudoXml);
        var chave = evt.eventoCTe.infEvento.chCTe;
        var infEvento = evt.retEventoCTe.infEvento;
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        var ehCancelamento = infEvento.tpEvento == TpEventoCancelamento;

        if (ehCancelamento && xml is not null)
        {
            xml.Cancelada = "S";
            xml.DataCancelamento = DateOnly.FromDateTime(DateTime.Now);
            xml.MotivoCancelamento = infEvento.xMotivo;
            await xmlRepository.AtualizarAsync(xml, cancellationToken);
            await LogarAsync(empresa.Id, "CTe: cancelamento registrado (evento completo).", chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
            return;
        }

        await LogarAsync(empresa.Id, $"CTe: evento completo recebido ({infEvento.xEvento}).", chave: chave, xmlId: xml?.Id, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// A distribuição DFe de CTe nesta versão do Zeus.Net não expõe classes tipadas
    /// para os schemas de resumo (resCTe/resEvento) — só documento completo (cteProc) e
    /// evento completo (procEventoCTe). Decisão confirmada com o usuário: registrar um
    /// XML com dados mínimos (via parsing genérico) e logar, sem manifestação automática.
    /// </summary>
    private async Task ProcessarResumoGenericoCteAsync(Empresa empresa, string schema, string conteudoXml, CancellationToken cancellationToken)
    {
        string? chave = null;
        try
        {
            var documento = System.Xml.Linq.XDocument.Parse(conteudoXml);
            chave = documento.Descendants().FirstOrDefault(e => e.Name.LocalName == "chCTe")?.Value;
        }
        catch (Exception)
        {
            // conteúdo não é um XML válido ou não tem o elemento chCTe — segue sem chave
        }

        chave ??= $"SEMCHAVE-{DateTime.Now:yyyyMMddHHmmss}";

        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        var novo = xml is null;
        xml ??= new Xml { Chave = chave, EmpresaId = empresa.Id, Modelo = "57" };
        xml.Situacao = $"Resumo (schema: {schema})";
        xml.Schema = schema;

        if (novo) await xmlRepository.IncluirAsync(xml, cancellationToken);
        else await xmlRepository.AtualizarAsync(xml, cancellationToken);

        await LogarAsync(empresa.Id, $"CTe: resumo recebido com schema não tipado ({schema}).", chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
    }

    // ----------------------------------------------------------------------------
    // Auxiliares
    // ----------------------------------------------------------------------------

    private static string SalvarXml(string? pastaBase, string cnpj, DateTime dataEmissao, string tipoDocumento, string chave, string conteudoXml)
    {
        if (string.IsNullOrWhiteSpace(pastaBase))
        {
            throw new InvalidOperationException("A empresa não possui uma pasta de XMLs configurada (PastaXml).");
        }

        var pasta = Path.Combine(pastaBase, cnpj, dataEmissao.Year.ToString("D4"), dataEmissao.Month.ToString("D2"), tipoDocumento);
        Directory.CreateDirectory(pasta);

        var caminho = Path.Combine(pasta, $"{chave}.xml");
        File.WriteAllText(caminho, conteudoXml);
        return caminho;
    }

    private async Task LogarAsync(
        int? empresaId,
        string mensagem,
        string? chave = null,
        int? xmlId = null,
        int? quantidadeXmls = null,
        TimeOnly? horaInicio = null,
        TimeOnly? horaFinal = null,
        CancellationToken cancellationToken = default)
    {
        var agora = TimeOnly.FromDateTime(DateTime.Now);
        await logRepository.IncluirAsync(new Log
        {
            Data = DateOnly.FromDateTime(DateTime.Now),
            HoraInicio = horaInicio ?? agora,
            HoraFinal = horaFinal ?? agora,
            EmpresaId = empresaId,
            Mensagem = mensagem,
            Chave = chave,
            XmlId = xmlId,
            QuantidadeXmls = quantidadeXmls
        }, cancellationToken);
    }
}
