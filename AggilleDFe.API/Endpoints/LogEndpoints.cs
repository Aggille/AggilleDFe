using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class LogEndpoints
{
    public static IEndpointRouteBuilder MapLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/logs").WithTags("Registros");

        group.MapGet("/", async (int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, ILogService service) =>
            Results.Ok(await service.PesquisarAsync(empresaId, dataInicial, dataFinal)))
        .WithSummary("Pesquisa os registros de log, com filtros opcionais por empresa e período");

        return app;
    }
}
