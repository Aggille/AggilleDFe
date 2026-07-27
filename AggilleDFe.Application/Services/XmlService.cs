using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Application.Services;

public class XmlService(IXmlRepository repository) : IXmlService
{
    public async Task<IReadOnlyList<XmlDto>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, DateOnly? emissaoInicial = null, DateOnly? emissaoFinal = null, CancellationToken cancellationToken = default)
    {
        var xmls = await repository.PesquisarAsync(empresaId, dataInicial, dataFinal, modelo, fornecedor, emissaoInicial, emissaoFinal, cancellationToken);
        return xmls.Select(ParaDto).ToList();
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
        MotivoCancelamento = xml.MotivoCancelamento
    };
}
