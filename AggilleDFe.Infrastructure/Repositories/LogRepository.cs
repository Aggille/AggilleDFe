using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AggilleDFe.Infrastructure.Repositories;

public class LogRepository(AppDbContext context) : ILogRepository
{
    public async Task IncluirAsync(Log log, CancellationToken cancellationToken = default)
    {
        context.Logs.Add(log);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Log>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, CancellationToken cancellationToken = default)
    {
        var consulta = context.Logs.AsQueryable();

        if (empresaId is not null)
        {
            consulta = consulta.Where(l => l.EmpresaId == empresaId);
        }

        if (dataInicial is not null)
        {
            consulta = consulta.Where(l => l.Data >= dataInicial);
        }

        if (dataFinal is not null)
        {
            consulta = consulta.Where(l => l.Data <= dataFinal);
        }

        return await consulta
            .OrderByDescending(l => l.Data)
            .ThenByDescending(l => l.Id)
            .ToListAsync(cancellationToken);
    }
}
