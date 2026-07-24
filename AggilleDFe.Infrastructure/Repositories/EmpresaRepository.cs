using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AggilleDFe.Infrastructure.Repositories;

public class EmpresaRepository(AppDbContext context) : IEmpresaRepository
{
    public async Task<IReadOnlyList<Empresa>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default)
    {
        var consulta = context.Empresas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            consulta = consulta.Where(e =>
                EF.Functions.ILike(e.RazaoSocial ?? string.Empty, $"%{busca}%") ||
                EF.Functions.ILike(e.Cnpj ?? string.Empty, $"%{busca}%"));
        }

        return await consulta.OrderBy(e => e.RazaoSocial).ToListAsync(cancellationToken);
    }

    public Task<Empresa?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Empresas.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExisteComCnpjAsync(string cnpj, int? idExcluir = null, CancellationToken cancellationToken = default) =>
        context.Empresas.AnyAsync(e => e.Cnpj == cnpj && (idExcluir == null || e.Id != idExcluir), cancellationToken);

    public async Task IncluirAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        context.Empresas.Add(empresa);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        context.Empresas.Update(empresa);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExcluirAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        context.Empresas.Remove(empresa);
        await context.SaveChangesAsync(cancellationToken);
    }
}
