using System.Collections.Concurrent;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using DFe.Classes.Entidades;
using Microsoft.Extensions.Configuration;
using NFe.Servicos;
using ConsultaProtocoloCte = CTe.Servicos.ConsultaProtocolo.ConsultaProtcoloServico;

namespace AggilleDFe.Infrastructure.Integrations;

public class SituacaoSefazXmlService(
    IEmpresaRepository empresaRepository,
    IXmlRepository xmlRepository,
    ILogRepository logRepository,
    IConfiguration configuration) : ISituacaoSefazXmlService
{
    private const int TpEventoCancelamento = 110111;
    private const int TpEventoCancelamentoSubstituicao = 110112;
    private const int MaximoConsultasSimultaneas = 4;

    // Situações do documento (retConsSitNFe/retConsSitCTe) que indicam cancelamento:
    // 101 - cancelamento homologado; 151/155 - cancelamento homologado fora de prazo.
    private static readonly int[] CStatsCancelado = [101, 151, 155];

    // Situações do evento (retEvento) que indicam evento registrado/vinculado.
    private static readonly int[] CStatsEventoRegistrado = [135, 136, 155];

    // Manifestações conclusivas do destinatário (NFe). A última registrada vale: 210220
    // (Desconhecimento da Operação) e 210240 (Operação Não Realizada) são recusa — a
    // nota não pode ser importada, mesma regra do cancelamento; 210200 (Confirmação).
    private const int TpEventoConfirmacao = 210200;
    private const int TpEventoDesconhecimento = 210220;
    private const int TpEventoNaoRealizada = 210240;

    private sealed record RecusaDestinatario(int TpEvento, DateOnly Data, string? Motivo);

    private sealed record ResultadoConsulta(string Chave, bool Cancelada, DateOnly? DataCancelamento, string? MotivoCancelamento, string? Erro)
    {
        public RecusaDestinatario? Recusa { get; init; }

        // Rejeição da SEFAZ (uso denegado): cStat/xMotivo da situação do documento.
        public (int CStat, string? XMotivo)? Rejeicao { get; init; }
    }

    // Situações do documento que indicam uso denegado (documento rejeitado pela SEFAZ).
    private static readonly int[] CStatsDenegado = [110, 301, 302, 303];

    public async Task AtualizarSituacoesAsync(int empresaId, IReadOnlyCollection<string> chaves, CancellationToken cancellationToken = default)
    {
        if (chaves.Count == 0)
        {
            return;
        }

        var empresa = await empresaRepository.ObterPorIdAsync(empresaId, cancellationToken);
        if (empresa is null)
        {
            return;
        }

        var inicio = TimeOnly.FromDateTime(DateTime.Now);
        var resultados = new ConcurrentBag<ResultadoConsulta>();

        try
        {
            var certificado = ZeusConfiguracaoFactory.CarregarCertificado(empresa);
            var diretorioSchemas = configuration["SchemasPath"] ?? "SCHEMAS";

            // As chamadas SEFAZ rodam em paralelo (cada uma com sua própria configuração
            // e instância de serviço Zeus); a gravação no banco fica sequencial depois,
            // porque o DbContext não é thread-safe.
            await Parallel.ForEachAsync(chaves,
                new ParallelOptions { MaxDegreeOfParallelism = MaximoConsultasSimultaneas, CancellationToken = cancellationToken },
                (chave, _) =>
                {
                    resultados.Add(Consultar(empresa, certificado, diretorioSchemas, chave));
                    return ValueTask.CompletedTask;
                });
        }
        catch (InvalidOperationException ex)
        {
            await LogarAsync(empresa.Id, $"Consulta de situação na SEFAZ (listagem de XMLs): {ex.Message}", cancellationToken: cancellationToken);
            return;
        }

        var canceladas = 0;
        var recusadas = 0;
        var rejeitadas = 0;
        var erros = 0;
        foreach (var resultado in resultados)
        {
            if (resultado.Erro is not null)
            {
                erros++;
                await LogarAsync(empresa.Id, $"Consulta de situação na SEFAZ: {resultado.Erro}", chave: resultado.Chave, cancellationToken: cancellationToken);
                continue;
            }

            if (!resultado.Cancelada && resultado.Recusa is null && resultado.Rejeicao is null)
            {
                continue;
            }

            var xml = await xmlRepository.ObterPorChaveAsync(resultado.Chave, cancellationToken);
            if (xml is null)
            {
                continue;
            }

            var tipo = xml.Modelo == "57" ? "CTe" : "NFe";
            var mensagens = new List<string>();

            if (resultado.Cancelada && xml.Cancelada != "S")
            {
                xml.Cancelada = "S";
                xml.DataCancelamento = resultado.DataCancelamento ?? DateOnly.FromDateTime(DateTime.Now);
                xml.MotivoCancelamento = resultado.MotivoCancelamento;
                canceladas++;
                mensagens.Add($"{tipo}: cancelamento registrado (consulta de situação na SEFAZ).");
            }

            // Recusa do destinatário: grava nos mesmos campos da manifestação feita pelo
            // AggilleDFe (ManifestacaoService), que é como a listagem identifica Recusada.
            if (resultado.Recusa is { } recusa && xml.DataDesconhecimento is null && xml.DataNaoRealizacao is null)
            {
                if (recusa.TpEvento == TpEventoNaoRealizada)
                {
                    xml.DataNaoRealizacao = recusa.Data;
                    xml.MotivoNaoRealizacao = recusa.Motivo;
                    xml.Situacao = "Operação não realizada registrada";
                    mensagens.Add($"{tipo}: Operação Não Realizada do destinatário registrada (consulta de situação na SEFAZ).");
                }
                else
                {
                    xml.DataDesconhecimento = recusa.Data;
                    xml.Situacao = "Desconhecimento registrado";
                    mensagens.Add($"{tipo}: Desconhecimento da Operação do destinatário registrado (consulta de situação na SEFAZ).");
                }

                recusadas++;
            }

            // Uso denegado: grava o cStat/xMotivo da SEFAZ no protocolo do documento
            // (StatusNfe/MensagemNfe), que é como a listagem identifica a nota rejeitada.
            if (resultado.Rejeicao is { } rejeicao && xml.StatusNfe != rejeicao.CStat)
            {
                xml.StatusNfe = rejeicao.CStat;
                xml.MensagemNfe = rejeicao.XMotivo is { Length: > 254 } m ? m[..254] : rejeicao.XMotivo;
                rejeitadas++;
                mensagens.Add($"{tipo}: rejeição da SEFAZ registrada - cStat {rejeicao.CStat} {rejeicao.XMotivo} (consulta de situação na SEFAZ).");
            }

            if (mensagens.Count == 0)
            {
                continue;
            }

            await xmlRepository.AtualizarAsync(xml, cancellationToken);
            foreach (var mensagem in mensagens)
            {
                await LogarAsync(empresa.Id, mensagem, chave: xml.Chave, xmlId: xml.Id, cancellationToken: cancellationToken);
            }
        }

        await LogarAsync(empresa.Id,
            $"Consulta de situação na SEFAZ (listagem de XMLs): {chaves.Count} documento(s) consultado(s), {canceladas} cancelamento(s) registrado(s), {recusadas} recusa(s) do destinatário registrada(s), {rejeitadas} rejeição(ões) da SEFAZ registrada(s), {erros} erro(s).",
            quantidadeXmls: chaves.Count, horaInicio: inicio, cancellationToken: cancellationToken);
    }

    private static ResultadoConsulta Consultar(Empresa empresa, System.Security.Cryptography.X509Certificates.X509Certificate2 certificado, string diretorioSchemas, string chave)
    {
        try
        {
            // A consulta protocolo é atendida pela SEFAZ autorizadora do emitente (UF
            // nos 2 primeiros dígitos da chave), não pela UF da empresa destinatária.
            if (!int.TryParse(chave[..2], out var codigoUf) || !Enum.IsDefined(typeof(Estado), codigoUf))
            {
                return new ResultadoConsulta(chave, false, null, null, "UF da chave inválida.");
            }

            var ufEmitente = (Estado)codigoUf;
            var modelo = chave.Substring(20, 2);

            return modelo switch
            {
                "55" => ConsultarNfe(empresa, certificado, diretorioSchemas, ufEmitente, chave),
                "57" => ConsultarCte(empresa, diretorioSchemas, ufEmitente, chave),
                _ => new ResultadoConsulta(chave, false, null, null, $"Modelo {modelo} não suportado na consulta de situação.")
            };
        }
        catch (Exception ex)
        {
            return new ResultadoConsulta(chave, false, null, null, ex.Message);
        }
    }

    private static ResultadoConsulta ConsultarNfe(Empresa empresa, System.Security.Cryptography.X509Certificates.X509Certificate2 certificado, string diretorioSchemas, Estado ufEmitente, string chave)
    {
        var configuracao = ZeusConfiguracaoFactory.Criar(empresa, diretorioSchemas);
        configuracao.cUF = ufEmitente;

        using var servicosNFe = new ServicosNFe(configuracao, certificado);
        var retorno = servicosNFe.NfeConsultaProtocolo(chave).Retorno;

        // Última manifestação conclusiva do destinatário registrada na nota; só é recusa
        // se for Desconhecimento ou Operação Não Realizada (uma Confirmação posterior
        // prevalece).
        var ultimaManifestacao = retorno.procEventoNFe?
            .Where(p => p.retEvento?.infEvento is { } inf
                && inf.tpEvento is { } tp && (int)tp is TpEventoConfirmacao or TpEventoDesconhecimento or TpEventoNaoRealizada
                && CStatsEventoRegistrado.Contains(inf.cStat))
            .OrderByDescending(p => p.retEvento.infEvento.dhRegEvento)
            .FirstOrDefault();

        RecusaDestinatario? recusa = null;
        if (ultimaManifestacao?.retEvento.infEvento is { } infManifestacao && (int)infManifestacao.tpEvento! != TpEventoConfirmacao)
        {
            var motivoRecusa = ultimaManifestacao.evento?.infEvento?.detEvento?.xJust ?? infManifestacao.xEvento;
            recusa = new RecusaDestinatario((int)infManifestacao.tpEvento!, DateOnly.FromDateTime(infManifestacao.dhRegEvento), motivoRecusa);
        }

        return SituacaoCancelamentoNfe() with { Recusa = recusa };

        ResultadoConsulta SituacaoCancelamentoNfe()
        {
            var eventoCancelamento = retorno.procEventoNFe?
                .Where(p => p.retEvento?.infEvento is { } inf
                    && inf.tpEvento is { } tp && ((int)tp == TpEventoCancelamento || (int)tp == TpEventoCancelamentoSubstituicao)
                    && CStatsEventoRegistrado.Contains(inf.cStat))
                .OrderByDescending(p => p.retEvento.infEvento.dhRegEvento)
                .FirstOrDefault();

            if (eventoCancelamento is not null)
            {
                var infRet = eventoCancelamento.retEvento.infEvento;
                var motivo = eventoCancelamento.evento?.infEvento?.detEvento?.xJust ?? infRet.xEvento ?? infRet.xMotivo;
                return new ResultadoConsulta(chave, true, DateOnly.FromDateTime(infRet.dhRegEvento), motivo, null);
            }

            if (CStatsCancelado.Contains(retorno.cStat))
            {
                return new ResultadoConsulta(chave, true, null, retorno.xMotivo, null);
            }

            return RetornoNaoCancelado(chave, retorno.cStat, retorno.xMotivo);
        }
    }

    private static ResultadoConsulta ConsultarCte(Empresa empresa, string diretorioSchemas, Estado ufEmitente, string chave)
    {
        var configuracao = ZeusConfiguracaoFactory.CriarCte(empresa, diretorioSchemas);
        configuracao.cUF = ufEmitente;

        var retorno = new ConsultaProtocoloCte().ConsultaProtocoloV4(chave, configuracao);

        var eventoCancelamento = retorno.procEventoCTe?
            .Where(p => p.retEvento?.infEvento is { } inf
                && inf.tpEvento == TpEventoCancelamento
                && CStatsEventoRegistrado.Contains(inf.cStat))
            .FirstOrDefault();

        if (eventoCancelamento is not null)
        {
            var infRet = eventoCancelamento.retEvento.infEvento;
            DateOnly? data = infRet.dhRegEvento is { } dh ? DateOnly.FromDateTime(dh.DateTime) : null;
            var justificativa = (eventoCancelamento.eventoCTe?.infEvento?.detEvento?.EventoContainer as CTe.Classes.Servicos.Evento.evCancCTe)?.xJust;
            return new ResultadoConsulta(chave, true, data, justificativa ?? infRet.xEvento ?? infRet.xMotivo, null);
        }

        if (CStatsCancelado.Contains(retorno.cStat))
        {
            return new ResultadoConsulta(chave, true, null, retorno.xMotivo, null);
        }

        return RetornoNaoCancelado(chave, retorno.cStat, retorno.xMotivo);
    }

    // 100/150 (autorizado) é situação válida sem cancelamento; 110/301/302/303 (uso
    // denegado) é rejeição da SEFAZ — a nota não pode ser importada, mesma regra do
    // cancelamento; qualquer outro cStat (ex.: 217 - não consta na base, 656 - consumo
    // indevido) é devolvido como erro para ficar registrado em LOGS.
    private static ResultadoConsulta RetornoNaoCancelado(string chave, int cStat, string? xMotivo)
    {
        if (cStat is 100 or 150)
        {
            return new ResultadoConsulta(chave, false, null, null, null);
        }

        if (CStatsDenegado.Contains(cStat))
        {
            return new ResultadoConsulta(chave, false, null, null, null) { Rejeicao = (cStat, xMotivo) };
        }

        return new ResultadoConsulta(chave, false, null, null, $"SEFAZ retornou cStat {cStat} - {xMotivo}.");
    }

    private Task LogarAsync(int empresaId, string mensagem, string? chave = null, int? xmlId = null, int? quantidadeXmls = null, TimeOnly? horaInicio = null, CancellationToken cancellationToken = default)
    {
        var agora = TimeOnly.FromDateTime(DateTime.Now);
        return logRepository.IncluirAsync(new Log
        {
            Data = DateOnly.FromDateTime(DateTime.Now),
            HoraInicio = horaInicio ?? agora,
            HoraFinal = agora,
            EmpresaId = empresaId,
            Mensagem = mensagem,
            Chave = chave,
            XmlId = xmlId,
            QuantidadeXmls = quantidadeXmls
        }, cancellationToken);
    }
}
