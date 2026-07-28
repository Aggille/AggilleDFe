using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using NFe.Classes.Servicos.Tipos;
using NFe.Servicos;

namespace AggilleDFe.Infrastructure.Integrations;

public class ManifestacaoService(
    IEmpresaRepository empresaRepository,
    IXmlRepository xmlRepository,
    ILogRepository logRepository,
    IConfiguration configuration) : IManifestacaoService
{
    private const int CStatLoteEventoProcessado = 128;
    private const int MotivoTamanhoMinimo = 15;
    private const int MotivoTamanhoMaximo = 255;

    public Task<(bool Sucesso, string? Erro)> ManifestarCienciaAsync(string chave, CancellationToken cancellationToken = default) =>
        ManifestarAsync(chave, NFeTipoEvento.TeMdCienciaDaOperacao, justificativa: null, cancellationToken);

    public Task<(bool Sucesso, string? Erro)> ManifestarDesconhecimentoAsync(string chave, string motivo, CancellationToken cancellationToken = default)
    {
        var erroMotivo = ValidarMotivo(motivo);
        return erroMotivo is not null
            ? Task.FromResult<(bool, string?)>((false, erroMotivo))
            : ManifestarAsync(chave, NFeTipoEvento.TeMdDesconhecimentoDaOperacao, motivo, cancellationToken);
    }

    public Task<(bool Sucesso, string? Erro)> ManifestarNaoRealizadaAsync(string chave, string motivo, CancellationToken cancellationToken = default)
    {
        var erroMotivo = ValidarMotivo(motivo);
        return erroMotivo is not null
            ? Task.FromResult<(bool, string?)>((false, erroMotivo))
            : ManifestarAsync(chave, NFeTipoEvento.TeMdOperacaoNaoRealizada, motivo, cancellationToken);
    }

    private static string? ValidarMotivo(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return "O motivo é obrigatório.";
        }

        var tamanho = motivo.Trim().Length;
        if (tamanho < MotivoTamanhoMinimo)
        {
            return $"O motivo deve ter pelo menos {MotivoTamanhoMinimo} caracteres.";
        }

        if (tamanho > MotivoTamanhoMaximo)
        {
            return $"O motivo deve ter no máximo {MotivoTamanhoMaximo} caracteres.";
        }

        return null;
    }

    private async Task<(bool Sucesso, string? Erro)> ManifestarAsync(string chave, NFeTipoEvento tipoEvento, string? justificativa, CancellationToken cancellationToken)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (xml is null)
        {
            return (false, "Chave não encontrada.");
        }

        if (xml.Modelo != "55")
        {
            return (false, "Manifestação do destinatário disponível apenas para NFe.");
        }

        if (xml.EmpresaId is null)
        {
            return (false, "XML sem empresa associada.");
        }

        var empresa = await empresaRepository.ObterPorIdAsync(xml.EmpresaId.Value, cancellationToken);
        if (empresa is null)
        {
            return (false, "Empresa não encontrada.");
        }

        try
        {
            var certificado = ZeusConfiguracaoFactory.CarregarCertificado(empresa);
            var diretorioSchemas = configuration["SchemasPath"] ?? "SCHEMAS";
            var configuracaoServico = ZeusConfiguracaoFactory.Criar(empresa, diretorioSchemas);

            using var servicoNfe = new ServicosNFe(configuracaoServico, certificado);
            var retorno = servicoNfe.RecepcaoEventoManifestacaoDestinatario(
                idlote: 1,
                sequenciaEvento: 1,
                chaveNFe: chave,
                nFeTipoEventoManifestacaoDestinatario: tipoEvento,
                cpfcnpj: empresa.Cnpj!,
                justificativa: justificativa,
                dhEvento: null);

            var loteCStat = retorno.Retorno?.cStat;
            if (loteCStat != CStatLoteEventoProcessado)
            {
                var mensagemErro = $"SEFAZ não confirmou a manifestação (cStat {loteCStat} - {retorno.Retorno?.xMotivo}).";
                await LogarAsync(empresa.Id, $"Manifestação {tipoEvento} não confirmada: {mensagemErro}", chave, xml.Id, cancellationToken);
                return (false, mensagemErro);
            }

            AtualizarXmlConformeEvento(xml, tipoEvento, justificativa);
            await xmlRepository.AtualizarAsync(xml, cancellationToken);

            return (true, null);
        }
        catch (InvalidOperationException ex)
        {
            await LogarAsync(empresa.Id, $"Falha ao manifestar {DescricaoEvento(tipoEvento)}: {ex.Message}", chave, xml.Id, cancellationToken);
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            var mensagem = $"Falha ao manifestar {DescricaoEvento(tipoEvento)}: {ex.Message}";
            await LogarAsync(empresa.Id, mensagem, chave, xml.Id, cancellationToken);
            return (false, mensagem);
        }
    }

    private static void AtualizarXmlConformeEvento(Xml xml, NFeTipoEvento tipoEvento, string? justificativa)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);

        switch (tipoEvento)
        {
            case NFeTipoEvento.TeMdCienciaDaOperacao:
                xml.DataCiencia = hoje;
                xml.Situacao = "Ciência realizada";
                break;
            case NFeTipoEvento.TeMdDesconhecimentoDaOperacao:
                xml.DataDesconhecimento = hoje;
                xml.Situacao = "Desconhecimento registrado";
                break;
            case NFeTipoEvento.TeMdOperacaoNaoRealizada:
                xml.DataNaoRealizacao = hoje;
                xml.MotivoNaoRealizacao = justificativa;
                xml.Situacao = "Operação não realizada registrada";
                break;
        }
    }

    private static string DescricaoEvento(NFeTipoEvento tipoEvento) => tipoEvento switch
    {
        NFeTipoEvento.TeMdCienciaDaOperacao => "Ciência da Operação",
        NFeTipoEvento.TeMdDesconhecimentoDaOperacao => "Desconhecimento da Operação",
        NFeTipoEvento.TeMdOperacaoNaoRealizada => "Operação Não Realizada",
        _ => tipoEvento.ToString()
    };

    private Task LogarAsync(int empresaId, string mensagem, string chave, int xmlId, CancellationToken cancellationToken)
    {
        var agora = TimeOnly.FromDateTime(DateTime.Now);
        return logRepository.IncluirAsync(new Log
        {
            Data = DateOnly.FromDateTime(DateTime.Now),
            HoraInicio = agora,
            HoraFinal = agora,
            EmpresaId = empresaId,
            Mensagem = mensagem,
            Chave = chave,
            XmlId = xmlId
        }, cancellationToken);
    }
}
