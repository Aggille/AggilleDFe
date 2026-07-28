using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/dashboard", async (IDashboardService service) =>
            Results.Ok(await service.ObterAsync()))
        .WithTags("Dashboard")
        .WithSummary("Resumo para a tela inicial: empresas ativas, bloqueadas por consumo indevido, certificados vencendo e erros de hoje");

        return app;
    }
}
