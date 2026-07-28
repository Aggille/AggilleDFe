using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AggilleDFe.Infrastructure.Repositories;

public class XmlRepository(AppDbContext context) : IXmlRepository
{
    public Task<Xml?> ObterPorChaveAsync(string chave, CancellationToken cancellationToken = default) =>
        context.Xmls.FirstOrDefaultAsync(x => x.Chave == chave, cancellationToken);

    public async Task IncluirAsync(Xml xml, CancellationToken cancellationToken = default)
    {
        context.Xmls.Add(xml);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Xml xml, CancellationToken cancellationToken = default)
    {
        context.Xmls.Update(xml);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Xml>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, DateOnly? emissaoInicial = null, DateOnly? emissaoFinal = null, CancellationToken cancellationToken = default)
    {
        var consulta = context.Xmls.AsQueryable();

        if (empresaId is not null)
        {
            consulta = consulta.Where(x => x.EmpresaId == empresaId);
        }

        if (dataInicial is not null)
        {
            consulta = consulta.Where(x => x.DataDownload >= dataInicial);
        }

        if (dataFinal is not null)
        {
            consulta = consulta.Where(x => x.DataDownload <= dataFinal);
        }

        if (emissaoInicial is not null)
        {
            consulta = consulta.Where(x => x.Emissao >= emissaoInicial);
        }

        if (emissaoFinal is not null)
        {
            consulta = consulta.Where(x => x.Emissao <= emissaoFinal);
        }

        if (!string.IsNullOrWhiteSpace(modelo))
        {
            consulta = consulta.Where(x => x.Modelo == modelo);
        }

        if (!string.IsNullOrWhiteSpace(fornecedor))
        {
            consulta = consulta.Where(x => EF.Functions.ILike(x.FornecedorNome ?? string.Empty, $"%{fornecedor}%"));
        }

        // Projeta sem ConteudoXml: a listagem pode ter centenas de linhas e esse
        // campo guarda o XML inteiro, desnecessário e custoso pra grid.
        return await consulta
            .OrderByDescending(x => x.DataDownload)
            .ThenByDescending(x => x.Emissao)
            .ThenByDescending(x => x.Id)
            .Select(x => new Xml
            {
                Id = x.Id,
                Chave = x.Chave,
                Protocolo = x.Protocolo,
                Emissao = x.Emissao,
                DataDownload = x.DataDownload,
                FornecedorNome = x.FornecedorNome,
                FornecedorCnpj = x.FornecedorCnpj,
                FornecedorCidade = x.FornecedorCidade,
                FornecedorUf = x.FornecedorUf,
                ValorTotal = x.ValorTotal,
                ValorIcms = x.ValorIcms,
                StatusNfe = x.StatusNfe,
                MensagemNfe = x.MensagemNfe,
                NomeXml = x.NomeXml,
                Numero = x.Numero,
                Serie = x.Serie,
                Modelo = x.Modelo,
                EmpresaId = x.EmpresaId,
                Cancelada = x.Cancelada,
                Schema = x.Schema,
                Descricao = x.Descricao,
                Mensagem = x.Mensagem,
                Situacao = x.Situacao,
                DataCiencia = x.DataCiencia,
                DataRealizacao = x.DataRealizacao,
                DataNaoRealizacao = x.DataNaoRealizacao,
                DataDesconhecimento = x.DataDesconhecimento,
                MotivoNaoRealizacao = x.MotivoNaoRealizacao,
                DataCancelamento = x.DataCancelamento,
                MotivoCancelamento = x.MotivoCancelamento
            })
            .ToListAsync(cancellationToken);
    }
}
