using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Application.Validation;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Application.Services;

public class XmlService(IXmlRepository repository, IEmpresaRepository empresaRepository, ISituacaoSefazXmlService situacaoSefazXmlService) : IXmlService
{
    public async Task<IReadOnlyList<XmlDto>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, DateOnly? emissaoInicial = null, DateOnly? emissaoFinal = null, CancellationToken cancellationToken = default)
    {
        var xmls = await repository.PesquisarAsync(empresaId, dataInicial, dataFinal, modelo, fornecedor, emissaoInicial, emissaoFinal, cancellationToken);
        return xmls.Select(ParaDto).ToList();
    }

    public async Task<(IReadOnlyList<XmlDto>? Xmls, string? Erro)> PesquisarPorEmpresaAsync(DateOnly? dataInicial, DateOnly? dataFinal, string? cnpjEmpresa, string? tipo, bool usarDataDownload = false, bool consultaSefaz = false, CancellationToken cancellationToken = default)
    {
        if (dataInicial is null || dataFinal is null)
        {
            return (null, "Informe a data inicial e a data final.");
        }

        if (dataInicial > dataFinal)
        {
            return (null, "A data inicial não pode ser maior que a data final.");
        }

        // Aceita CNPJ com máscara (00.000.000/0000-00); o cadastro de empresas
        // grava só os 14 caracteres, sem máscara.
        var cnpj = new string((cnpjEmpresa ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (!CnpjValidator.FormatoValido(cnpj) || !CnpjValidator.DigitosVerificadoresValidos(cnpj))
        {
            return (null, "CNPJ da empresa inválido.");
        }

        var empresa = await empresaRepository.ObterPorCnpjAsync(cnpj, cancellationToken);
        if (empresa is null)
        {
            return (null, $"Nenhuma empresa cadastrada com o CNPJ {cnpj}.");
        }

        string? modelo;
        switch ((tipo ?? "todos").Trim().ToLowerInvariant())
        {
            case "todos":
                modelo = null;
                break;
            case "nfe":
                modelo = "55";
                break;
            case "cte":
                modelo = "57";
                break;
            case "nfse":
                // A distribuição DFe (SEFAZ) só entrega NFe/CTe — NFSe ainda não
                // é baixada nem gravada na tabela XMLS, então não há o que filtrar.
                return ([], null);
            default:
                return (null, "Tipo de documento inválido — use todos, nfe, cte ou nfse.");
        }

        var xmls = await PesquisarPeriodoAsync();

        // consultaSefaz: antes de devolver a lista, consulta na SEFAZ a situação dos
        // documentos ainda não cancelados nem recusados e grava os cancelamentos e as
        // recusas do destinatário encontrados, para o sistema integrador já receber
        // Cancelada/Recusada (e motivos) atualizados — nos dois casos o documento não
        // pode ser importado.
        if (consultaSefaz)
        {
            var chaves = xmls
                .Where(x => x.Cancelada != "S" && !EstaRecusada(x) && (x.Modelo == "55" || x.Modelo == "57") && x.Chave.Length == 44)
                .Select(x => x.Chave)
                .Distinct()
                .ToList();

            if (chaves.Count > 0)
            {
                await situacaoSefazXmlService.AtualizarSituacoesAsync(empresa.Id, chaves, cancellationToken);
                xmls = await PesquisarPeriodoAsync();
            }
        }

        return (xmls.Select(ParaDto).ToList(), null);

        Task<IReadOnlyList<Xml>> PesquisarPeriodoAsync() => usarDataDownload
            ? repository.PesquisarAsync(empresa.Id, dataInicial, dataFinal, modelo, null, null, null, cancellationToken)
            : repository.PesquisarAsync(empresa.Id, null, null, modelo, null, dataInicial, dataFinal, cancellationToken);
    }

    private static XmlDto ParaDto(Xml xml) => new()
    {
        Id = xml.Id,
        Chave = xml.Chave,
        Protocolo = xml.Protocolo,
        Emissao = xml.Emissao,
        DataDownload = xml.DataDownload,
        FornecedorNome = xml.FornecedorNome,
        FornecedorCnpj = xml.FornecedorCnpj,
        FornecedorCidade = xml.FornecedorCidade,
        FornecedorUf = xml.FornecedorUf,
        ValorTotal = xml.ValorTotal,
        ValorIcms = xml.ValorIcms,
        StatusNfe = xml.StatusNfe,
        MensagemNfe = xml.MensagemNfe,
        NomeXml = xml.NomeXml,
        Numero = xml.Numero,
        Serie = xml.Serie,
        Modelo = xml.Modelo,
        EmpresaId = xml.EmpresaId,
        Cancelada = xml.Cancelada == "S",
        Schema = xml.Schema,
        Situacao = xml.Situacao,
        DataCancelamento = xml.DataCancelamento,
        MotivoCancelamento = xml.MotivoCancelamento,
        Recusada = EstaRecusada(xml),
        DataRecusa = xml.DataNaoRealizacao ?? xml.DataDesconhecimento,
        MotivoRecusa = xml.DataNaoRealizacao is not null
            ? xml.MotivoNaoRealizacao ?? "Operação Não Realizada"
            : xml.DataDesconhecimento is not null ? "Desconhecimento da Operação" : null
    };

    // Recusada pelo destinatário: Operação Não Realizada (210240) ou Desconhecimento
    // da Operação (210220) registrados — manifestados pelo AggilleDFe ou encontrados
    // na consulta de situação na SEFAZ (consultaSefaz).
    private static bool EstaRecusada(Xml xml) => xml.DataNaoRealizacao is not null || xml.DataDesconhecimento is not null;
}
