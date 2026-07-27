using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Worker;

public class Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger) : BackgroundService
{
    private const int TempoExecucaoPadraoMinutos = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var proximoCicloEmMinutos = TempoExecucaoPadraoMinutos;

            try
            {
                proximoCicloEmMinutos = await ExecutarCicloAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha inesperada no ciclo do Worker.");
            }

            await Task.Delay(TimeSpan.FromMinutes(proximoCicloEmMinutos), stoppingToken);
        }
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
