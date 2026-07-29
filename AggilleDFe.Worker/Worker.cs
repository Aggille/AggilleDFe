using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Worker;

public class Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger) : BackgroundService
{
    private const int TempoExecucaoPadraoMinutos = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Espera o intervalo configurado ANTES do primeiro ciclo também -
        // sem isso, toda vez que o Worker é reiniciado (deploy, restart do
        // container/serviço, etc.) ele dispara um ciclo completo (todas as
        // empresas elegíveis) na hora, mesmo que o ciclo anterior tenha
        // rodado há poucos minutos.
        var proximoCicloEmMinutos = await ObterIntervaloConfiguradoAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(proximoCicloEmMinutos), stoppingToken);

            try
            {
                proximoCicloEmMinutos = await ExecutarCicloAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha inesperada no ciclo do Worker.");
                proximoCicloEmMinutos = TempoExecucaoPadraoMinutos;
            }
        }
    }

    private async Task<int> ObterIntervaloConfiguradoAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var configuracaoRepository = scope.ServiceProvider.GetRequiredService<IConfiguracaoRepository>();
        var configuracao = await configuracaoRepository.ObterAsync(stoppingToken);
        return configuracao?.TempoExecucao is > 0 ? configuracao.TempoExecucao!.Value : TempoExecucaoPadraoMinutos;
    }

    private async Task<int> ExecutarCicloAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var configuracaoRepository = scope.ServiceProvider.GetRequiredService<IConfiguracaoRepository>();
        var configuracao = await configuracaoRepository.ObterAsync(stoppingToken);

        if (configuracao?.TempoExecucao is not > 0)
        {
            logger.LogWarning(
                "Configuração ausente ou Tempo de Execução inválido — aguardando {minutos} min antes de tentar novamente.",
                TempoExecucaoPadraoMinutos);
            return TempoExecucaoPadraoMinutos;
        }

        var distribuicaoLoteService = scope.ServiceProvider.GetRequiredService<IDistribuicaoLoteService>();
        var resultado = await distribuicaoLoteService.ExecutarTodasAsync(execucaoManual: false, stoppingToken);
        logger.LogInformation("Ciclo concluído: {mensagem}", resultado.Mensagem);

        return configuracao.TempoExecucao!.Value;
    }
}
