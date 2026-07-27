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

    public async Task<IReadOnlyList<Xml>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, CancellationToken cancellationToken = default)
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

        if (!string.IsNullOrWhiteSpace(modelo))
        {
            consulta = consulta.Where(x => x.Modelo == modelo);
        }

        if (!string.IsNullOrWhiteSpace(fornecedor))
        {
            consulta = consulta.Where(x => EF.Functions.ILike(x.FornecedorNome ?? string.Empty, $"%{fornecedor}%"));
        }

        return await consulta
            .OrderByDescending(x => x.DataDownload)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
