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
    IEmailNotificacaoService emailNotificacaoService,
    IConfiguration configuration) : IDistribuicaoDfeService
{
    private const int CStatDocumentosLocalizados = 138;
    private const int CStatNenhumDocumentoLocalizado = 137;
    private const int CStatLoteEventoProcessado = 128;
    private const int CStatConsumoIndevido = 656;
    private const int TpEventoCancelamento = 110111;
    private const int DiasAvisoVencimentoCertificado = 15;

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
            await VerificarValidadeCertificadoAsync(empresa, certificado, cancellationToken);

            var diretorioSchemas = configuration["SchemasPath"] ?? "SCHEMAS";
            var configuracaoNfe = ZeusConfiguracaoFactory.Criar(empresa, diretorioSchemas);
            var configuracaoCte = ZeusConfiguracaoFactory.CriarCte(empresa, diretorioSchemas);

            var (baixadosNfe, eventosNfe) = await ExecutarNfeAsync(empresa, configuracaoNfe, certificado, cancellationToken);
            var (baixadosCte, eventosCte) = await ExecutarCteAsync(empresa, configuracaoCte, certificado, cancellationToken);

            if (baixadosNfe + baixadosCte > 0)
            {
                await emailNotificacaoService.EnviarAsync(empresa,
                    $"AggilleDFe: novos documentos baixados - {empresa.RazaoSocial}",
                    $"{baixadosNfe} XML(s) de NFe e {baixadosCte} XML(s) de CTe foram baixados para \"{empresa.RazaoSocial}\".",
                    cancellationToken);
            }

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

    private async Task VerificarValidadeCertificadoAsync(Empresa empresa, X509Certificate2 certificado, CancellationToken cancellationToken)
    {
        var diasRestantes = (certificado.NotAfter.Date - DateTime.Now.Date).Days;
        if (diasRestantes > DiasAvisoVencimentoCertificado)
        {
            return;
        }

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        if (empresa.CertificadoNotificadoEm == hoje)
        {
            return;
        }

        var mensagem = diasRestantes >= 0
            ? $"O certificado digital da empresa \"{empresa.RazaoSocial}\" vence em {diasRestantes} dia(s), em {certificado.NotAfter:dd/MM/yyyy}."
            : $"O certificado digital da empresa \"{empresa.RazaoSocial}\" está VENCIDO desde {certificado.NotAfter:dd/MM/yyyy}.";

        await LogarAsync(empresa.Id, mensagem, cancellationToken: cancellationToken);
        await emailNotificacaoService.EnviarAsync(empresa, $"AggilleDFe: certificado digital de {empresa.RazaoSocial}", mensagem, cancellationToken);

        empresa.CertificadoNotificadoEm = hoje;
        await empresaRepository.AtualizarAsync(empresa, cancellationToken);
    }

    private async Task BloquearPorConsumoIndevidoAsync(Empresa empresa, CancellationToken cancellationToken)
    {
        empresa.BloqueadaAte = DateTime.Now.AddHours(1);
        await empresaRepository.AtualizarAsync(empresa, cancellationToken);

        await emailNotificacaoService.EnviarAsync(empresa,
            $"AggilleDFe: {empresa.RazaoSocial} bloqueada por consumo indevido",
            $"A SEFAZ rejeitou a distribuição de documentos para \"{empresa.RazaoSocial}\" com o motivo " +
            $"\"Consumo Indevido\" (cStat 656). A empresa ficará fora das próximas execuções até " +
            $"{empresa.BloqueadaAte:dd/MM/yyyy HH:mm}.",
            cancellationToken);
    }

    /// <summary>
    /// Baixa uma NFe específica pela chave, sob demanda (tela "Baixar por Chave"),
    /// em vez de esperar o próximo ciclo de Distribuição DFe por NSU. Usa o mesmo
    /// serviço SEFAZ (NfeDistDFeInteresse) da rotina automática (ver
    /// ExecutarNfeAsync), só que passando <c>chNFE</c> em vez de <c>ultNSU</c>/
    /// <c>nSU</c> — a SEFAZ retorna o(s) item(ns) da distribuição relativos só a
    /// essa chave, no mesmo formato (resNFe/nfeProc/resEvento/procEventoNFe) já
    /// tratado por <see cref="ProcessarItemNfeAsync"/>, que é reaproveitado aqui
    /// sem alterações (mesmo upsert por chave, mesma gravação em disco).
    /// Não mexe em <c>Empresa.UltimoNsu</c> - essa consulta é independente da
    /// janela incremental de NSU usada pelo ciclo automático.
    /// </summary>
    public async Task<(ResultadoBaixarPorChaveDto? Resultado, string? Erro)> BaixarPorChaveAsync(int empresaId, string chave, CancellationToken cancellationToken = default)
    {
        var empresa = await empresaRepository.ObterPorIdAsync(empresaId, cancellationToken);
        if (empresa is null)
        {
            return (null, "Empresa não encontrada.");
        }

        chave = chave?.Trim() ?? string.Empty;
        if (chave.Length != 44 || !chave.All(char.IsDigit))
        {
            return (null, "Chave de acesso inválida (deve ter 44 dígitos numéricos).");
        }

        try
        {
            var existiaAntes = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken) is not null;

            var certificado = ZeusConfiguracaoFactory.CarregarCertificado(empresa);
            var diretorioSchemas = configuration["SchemasPath"] ?? "SCHEMAS";
            var configuracaoNfe = ZeusConfiguracaoFactory.Criar(empresa, diretorioSchemas);

            using var servicoNfe = new ServicosNFe(configuracaoNfe, certificado);
            var retorno = servicoNfe.NfeDistDFeInteresse(ufAutor: empresa.Uf!, documento: empresa.Cnpj!, ultNSU: string.Empty, nSU: string.Empty, chNFE: chave);
            var status = retorno.Retorno;

            if (status.cStat == CStatNenhumDocumentoLocalizado)
            {
                await LogarAsync(empresa.Id, $"NFe: download manual por chave não encontrou documento para \"{chave}\".", chave: chave, cancellationToken: cancellationToken);
                return (new ResultadoBaixarPorChaveDto
                {
                    Encontrado = false,
                    Mensagem = "Nenhum documento encontrado para essa chave nessa empresa (confira se a empresa selecionada é realmente a destinatária da NFe)."
                }, null);
            }

            if (status.cStat != CStatDocumentosLocalizados)
            {
                var mensagemErro = $"SEFAZ retornou cStat {status.cStat} - {status.xMotivo}.";
                await LogarAsync(empresa.Id, $"NFe: download manual por chave \"{chave}\" - {mensagemErro}", chave: chave, cancellationToken: cancellationToken);
                if (status.cStat == CStatConsumoIndevido)
                {
                    await BloquearPorConsumoIndevidoAsync(empresa, cancellationToken);
                }
                return (null, mensagemErro);
            }

            var itens = status.loteDistDFeInt ?? [];
            var baixouDocumentoCompleto = false;
            var apenasResumo = false;

            foreach (var item in itens)
            {
                await ProcessarItemNfeAsync(empresa, servicoNfe, item, cancellationToken);
                if (item.NfeProc is not null)
                {
                    baixouDocumentoCompleto = true;
                }
                else if (item.ResNFe is not null)
                {
                    apenasResumo = true;
                }
            }

            var mensagem = baixouDocumentoCompleto
                ? existiaAntes
                    ? "XML baixado e atualizado com sucesso (já havia um registro para essa chave)."
                    : "XML baixado e salvo com sucesso."
                : apenasResumo
                    ? "A NFe foi localizada, mas a SEFAZ só disponibilizou o resumo por enquanto — o documento completo ainda não está pronto (tente novamente em alguns minutos)."
                    : "A consulta encontrou algo relacionado a essa chave (ex.: evento), mas não um XML de NFe completo pra baixar.";

            await LogarAsync(empresa.Id, "NFe: XML baixado manualmente por chave.", chave: chave, cancellationToken: cancellationToken);

            return (new ResultadoBaixarPorChaveDto
            {
                Encontrado = true,
                JaExistia = existiaAntes,
                DocumentoCompletoBaixado = baixouDocumentoCompleto,
                Mensagem = mensagem
            }, null);
        }
        catch (InvalidOperationException ex)
        {
            await LogarAsync(empresa.Id, $"Falha ao baixar XML manualmente por chave \"{chave}\": {ex.Message}", chave: chave, cancellationToken: cancellationToken);
            return (null, ex.Message);
        }
        catch (Exception ex)
        {
            await LogarAsync(empresa.Id, $"Falha ao baixar XML manualmente por chave \"{chave}\": {ex.Message}", chave: chave, cancellationToken: cancellationToken);
            return (null, $"Falha ao baixar XML: {ex.Message}");
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
                break;
            }

            if (status.cStat != CStatDocumentosLocalizados)
            {
                await LogarAsync(empresa.Id, $"NFe: retorno inesperado da SEFAZ (cStat {status.cStat} - {status.xMotivo}).", nsu: ultNsu, cancellationToken: cancellationToken);
                if (status.cStat == CStatConsumoIndevido)
                {
                    await BloquearPorConsumoIndevidoAsync(empresa, cancellationToken);
                }
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
                    await LogarAsync(empresa.Id, $"NFe: erro ao processar NSU {item.NSU}: {ex.Message}", nsu: (int)item.NSU, cancellationToken: cancellationToken);
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
            quantidadeXmls: baixados, horaInicio: horaInicio, horaFinal: TimeOnly.FromDateTime(DateTime.Now), nsu: empresa.UltimoNsu, cancellationToken: cancellationToken);

        return (baixados, eventos);
    }

    private async Task<(bool NovoXmlBaixado, bool EventoProcessado)> ProcessarItemNfeAsync(Empresa empresa, ServicosNFe servicoNfe, NfeLote item, CancellationToken cancellationToken)
    {
        var nsu = (int)item.NSU;

        if (item.ResNFe is not null)
        {
            await ProcessarResumoNfeAsync(empresa, servicoNfe, item.ResNFe, nsu, cancellationToken);
            return (false, true);
        }

        if (item.ResEvento is not null)
        {
            await ProcessarResumoEventoNfeAsync(empresa, item.ResEvento, nsu, cancellationToken);
            return (false, true);
        }

        if (item.NfeProc is not null)
        {
            await ProcessarDocumentoCompletoNfeAsync(empresa, item, nsu, cancellationToken);
            return (true, false);
        }

        if (item.ProcEventoNFe is not null)
        {
            await ProcessarEventoCompletoNfeAsync(empresa, item.ProcEventoNFe, nsu, cancellationToken);
            return (false, true);
        }

        await LogarAsync(empresa.Id, $"NFe: item NSU {item.NSU} com schema não reconhecido ({item.schema}).", nsu: nsu, cancellationToken: cancellationToken);
        return (false, false);
    }

    private async Task ProcessarResumoNfeAsync(Empresa empresa, ServicosNFe servicoNfe, NFe.Classes.Servicos.DistribuicaoDFe.Schemas.resNFe resumo, int? nsu, CancellationToken cancellationToken)
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

        await LogarAsync(empresa.Id, "NFe: resumo recebido.", chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);

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
            else
            {
                await LogarAsync(empresa.Id,
                    $"NFe: manifestação de Ciência da Operação não confirmada (cStat {loteCStat} - {retornoManifestacao.Retorno?.xMotivo}).",
                    chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await LogarAsync(empresa.Id, $"NFe: erro ao manifestar ciência: {ex.Message}", chave: resumo.chNFe, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }
    }

    private async Task ProcessarResumoEventoNfeAsync(Empresa empresa, NFe.Classes.Servicos.DistribuicaoDFe.Schemas.resEvento evento, int? nsu, CancellationToken cancellationToken)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(evento.chNFe, cancellationToken);
        var ehCancelamento = evento.tpEvento == TpEventoCancelamento.ToString();

        if (ehCancelamento && xml is not null)
        {
            xml.Cancelada = "S";
            xml.DataCancelamento = DateOnly.FromDateTime(DateTime.Now);
            xml.MotivoCancelamento = evento.xEvento;
            await xmlRepository.AtualizarAsync(xml, cancellationToken);
            await LogarAsync(empresa.Id, "NFe: cancelamento registrado (resumo de evento).", chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }

        // Resumo de evento que não é cancelamento (ex.: outras manifestações de
        // terceiros) não gera log — decisão do usuário: só ficam em LOGS os XMLs
        // baixados, resumos da consulta (resNFe/resumo de CTe) e erros.
    }

    private async Task ProcessarDocumentoCompletoNfeAsync(Empresa empresa, NfeLote item, int? nsu, CancellationToken cancellationToken)
    {
        var doc = item.NfeProc!;
        var infNFe = doc.NFe.infNFe;
        var infProt = doc.protNFe.infProt;
        var chave = infProt.chNFe;

        var empresaDestino = await ResolverEmpresaDestinoAsync(empresa, infNFe.dest?.CNPJ, cancellationToken);

        var conteudoXml = Compressao.Unzip(item.XmlNfe);
        var (caminho, erroDisco) = Storage.CaminhoXmlHelper.TentarGravarArquivo(empresaDestino.PastaXml, empresaDestino.Cnpj!, infNFe.ide.dhEmi.DateTime, "NFe", chave, conteudoXml);

        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        var novo = xml is null;
        xml ??= new Xml { Chave = chave, EmpresaId = empresaDestino.Id };
        xml.EmpresaId = empresaDestino.Id;

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
        xml.NomeXml = caminho ?? xml.NomeXml;
        xml.ConteudoXml = conteudoXml;
        xml.Numero = (int)infNFe.ide.nNF;
        xml.Serie = infNFe.ide.serie.ToString();
        xml.Modelo = "55";
        xml.Schema = item.schema;
        xml.Situacao = "Documento completo";

        if (novo) await xmlRepository.IncluirAsync(xml, cancellationToken);
        else await xmlRepository.AtualizarAsync(xml, cancellationToken);

        var observacaoRedirecionamento = empresaDestino.Id != empresa.Id
            ? $" (retornado na consulta da empresa \"{empresa.RazaoSocial}\", mas destinado a \"{empresaDestino.RazaoSocial}\")"
            : string.Empty;

        if (erroDisco is null)
        {
            await LogarAsync(empresaDestino.Id, $"NFe: XML baixado e salvo.{observacaoRedirecionamento}", chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }
        else
        {
            await LogarAsync(empresaDestino.Id,
                $"NFe: XML baixado e registrado no banco, mas falhou ao gravar em disco (pasta \"{empresaDestino.PastaXml}\"): {erroDisco}{observacaoRedirecionamento}",
                chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// A Distribuição DFe pode retornar, junto aos documentos da empresa consultada,
    /// notas destinadas a OUTRA empresa cadastrada no sistema — típico quando matriz e
    /// filial usam o mesmo certificado digital (a SEFAZ não filtra estritamente pelo
    /// CNPJ informado nesse caso). Confirmado em produção: uma NFe com
    /// <c>dest.CNPJ</c> da filial apareceu na consulta feita com o CNPJ da matriz e foi
    /// salva (antes desta correção) na pasta da matriz. Aqui, se o CNPJ do
    /// destinatário do documento não bate com o da empresa consultada mas corresponde
    /// a outra empresa cadastrada, o documento pertence a essa outra empresa — usa a
    /// pasta/CNPJ/EmpresaId dela. Se não corresponder a nenhuma empresa cadastrada
    /// (ex.: NFe emitida pela própria empresa consultada, para um terceiro), mantém a
    /// empresa original.
    /// </summary>
    private async Task<Empresa> ResolverEmpresaDestinoAsync(Empresa empresaConsultada, string? cnpjDestinatario, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cnpjDestinatario) || cnpjDestinatario == empresaConsultada.Cnpj)
        {
            return empresaConsultada;
        }

        var empresaCorreta = await empresaRepository.ObterPorCnpjAsync(cnpjDestinatario, cancellationToken);
        return empresaCorreta ?? empresaConsultada;
    }

    private async Task ProcessarEventoCompletoNfeAsync(Empresa empresa, NFe.Classes.Servicos.DistribuicaoDFe.Schemas.procEventoNFe evt, int? nsu, CancellationToken cancellationToken)
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
            await LogarAsync(empresa.Id, "NFe: cancelamento registrado (evento completo).", chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }

        // Evento completo que não é cancelamento não gera log (mesma decisão de
        // ProcessarResumoEventoNfeAsync).
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
                break;
            }

            if (status.cStat != CStatDocumentosLocalizados)
            {
                await LogarAsync(empresa.Id, $"CTe: retorno inesperado da SEFAZ (cStat {status.cStat} - {status.xMotivo}).", nsu: ultNsu, cancellationToken: cancellationToken);
                if (status.cStat == CStatConsumoIndevido)
                {
                    await BloquearPorConsumoIndevidoAsync(empresa, cancellationToken);
                }
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
                    await LogarAsync(empresa.Id, $"CTe: erro ao processar NSU {item.NSU}: {ex.Message}", nsu: (int)item.NSU, cancellationToken: cancellationToken);
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
            quantidadeXmls: baixados, horaInicio: horaInicio, horaFinal: TimeOnly.FromDateTime(DateTime.Now), nsu: empresa.UltimoNsuCte, cancellationToken: cancellationToken);

        return (baixados, eventos);
    }

    private async Task<(bool NovoXmlBaixado, bool EventoProcessado)> ProcessarItemCteAsync(Empresa empresa, CteLote item, CancellationToken cancellationToken)
    {
        var nsu = (int)item.NSU;

        if (item.XmlNfe is null)
        {
            await LogarAsync(empresa.Id, $"CTe: item NSU {item.NSU} sem conteúdo.", nsu: nsu, cancellationToken: cancellationToken);
            return (false, false);
        }

        var conteudo = Compressao.Unzip(item.XmlNfe).RemoverDeclaracaoXml();

        if (conteudo.StartsWith("<cteProc", StringComparison.Ordinal))
        {
            await ProcessarDocumentoCompletoCteAsync(empresa, item.schema, conteudo, nsu, cancellationToken);
            return (true, false);
        }

        if (conteudo.StartsWith("<procEventoCTe", StringComparison.Ordinal))
        {
            await ProcessarEventoCompletoCteAsync(empresa, conteudo, nsu, cancellationToken);
            return (false, true);
        }

        await ProcessarResumoGenericoCteAsync(empresa, item.schema, conteudo, nsu, cancellationToken);
        return (false, true);
    }

    private async Task ProcessarDocumentoCompletoCteAsync(Empresa empresa, string schema, string conteudoXml, int? nsu, CancellationToken cancellationToken)
    {
        var doc = FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>(conteudoXml);
        var infCte = doc.CTe.infCte;
        var infProt = doc.protCTe.infProt;
        var chave = infProt.chCTe;

        var empresaDestino = await ResolverEmpresaDestinoAsync(empresa, infCte.dest?.CNPJ, cancellationToken);

        var (caminho, erroDisco) = Storage.CaminhoXmlHelper.TentarGravarArquivo(empresaDestino.PastaXml, empresaDestino.Cnpj!, infCte.ide.dhEmi.DateTime, "CTe", chave, conteudoXml);

        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        var novo = xml is null;
        xml ??= new Xml { Chave = chave, EmpresaId = empresaDestino.Id };
        xml.EmpresaId = empresaDestino.Id;

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
        xml.NomeXml = caminho ?? xml.NomeXml;
        xml.ConteudoXml = conteudoXml;
        xml.Numero = (int)infCte.ide.nCT;
        xml.Serie = infCte.ide.serie.ToString();
        xml.Modelo = "57";
        xml.Schema = schema;
        xml.Situacao = "Documento completo";

        if (novo) await xmlRepository.IncluirAsync(xml, cancellationToken);
        else await xmlRepository.AtualizarAsync(xml, cancellationToken);

        var observacaoRedirecionamento = empresaDestino.Id != empresa.Id
            ? $" (retornado na consulta da empresa \"{empresa.RazaoSocial}\", mas destinado a \"{empresaDestino.RazaoSocial}\")"
            : string.Empty;

        if (erroDisco is null)
        {
            await LogarAsync(empresaDestino.Id, $"CTe: XML baixado e salvo.{observacaoRedirecionamento}", chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }
        else
        {
            await LogarAsync(empresaDestino.Id,
                $"CTe: XML baixado e registrado no banco, mas falhou ao gravar em disco (pasta \"{empresaDestino.PastaXml}\"): {erroDisco}{observacaoRedirecionamento}",
                chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }
    }

    private async Task ProcessarEventoCompletoCteAsync(Empresa empresa, string conteudoXml, int? nsu, CancellationToken cancellationToken)
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
            await LogarAsync(empresa.Id, "CTe: cancelamento registrado (evento completo).", chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
        }

        // Evento completo que não é cancelamento não gera log (mesma decisão do
        // lado NFe, ver ProcessarEventoCompletoNfeAsync).
    }

    /// <summary>
    /// A distribuição DFe de CTe nesta versão do Zeus.Net não expõe classes tipadas
    /// para os schemas de resumo (resCTe/resEvento) — só documento completo (cteProc) e
    /// evento completo (procEventoCTe). Decisão confirmada com o usuário: registrar um
    /// XML com dados mínimos (via parsing genérico) e logar, sem manifestação automática.
    /// </summary>
    private async Task ProcessarResumoGenericoCteAsync(Empresa empresa, string schema, string conteudoXml, int? nsu, CancellationToken cancellationToken)
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

        await LogarAsync(empresa.Id, $"CTe: resumo recebido com schema não tipado ({schema}).", chave: xml.Chave, xmlId: xml.Id, nsu: nsu, cancellationToken: cancellationToken);
    }

    // ----------------------------------------------------------------------------
    // Auxiliares
    // ----------------------------------------------------------------------------

    private async Task LogarAsync(
        int? empresaId,
        string mensagem,
        string? chave = null,
        int? xmlId = null,
        int? quantidadeXmls = null,
        TimeOnly? horaInicio = null,
        TimeOnly? horaFinal = null,
        int? nsu = null,
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
            QuantidadeXmls = quantidadeXmls,
            Nsu = nsu
        }, cancellationToken);
    }
}
