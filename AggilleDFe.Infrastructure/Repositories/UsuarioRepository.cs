using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AggilleDFe.Infrastructure.Repositories;

public class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public async Task<IReadOnlyList<Usuario>> PesquisarAsync(string? busca, CancellationToken cancellationToken = default)
    {
        var consulta = context.Usuarios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            consulta = consulta.Where(u =>
                EF.Functions.ILike(u.Login, $"%{busca}%") ||
                EF.Functions.ILike(u.Nome ?? string.Empty, $"%{busca}%"));
        }

        return await consulta.OrderBy(u => u.Login).ToListAsync(cancellationToken);
    }

    public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken = default) =>
        context.Usuarios.FirstOrDefaultAsync(u => u.Login == login, cancellationToken);

    public Task<bool> ExisteComLoginAsync(string login, int? idExcluir = null, CancellationToken cancellationToken = default) =>
        context.Usuarios.AnyAsync(u => u.Login == login && (idExcluir == null || u.Id != idExcluir), cancellationToken);

    public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) =>
        context.Usuarios.AnyAsync(cancellationToken);

    public async Task IncluirAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Update(usuario);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExcluirAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Remove(usuario);
        await context.SaveChangesAsync(cancellationToken);
    }
}
