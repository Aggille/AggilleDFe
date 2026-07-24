using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using AggilleDFe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AggilleDFe.Infrastructure.Repositories;

public class ConfiguracaoRepository(AppDbContext context) : IConfiguracaoRepository
{
    public Task<Configuracao?> ObterAsync(CancellationToken cancellationToken = default) =>
        context.Configuracoes.FirstOrDefaultAsync(cancellationToken);

    public async Task SalvarAsync(Configuracao configuracao, CancellationToken cancellationToken = default)
    {
        if (configuracao.Id == 0)
        {
            context.Configuracoes.Add(configuracao);
        }
        else
        {
            context.Configuracoes.Update(configuracao);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
