using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class VersaoEndpoints
{
    public static IEndpointRouteBuilder MapVersaoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/versao").WithTags("Versão");

        group.MapGet("/", async (IConfiguracaoService service) =>
            Results.Ok(new VersaoDto { Versao = await service.ObterVersaoAsync() }))
        .WithSummary("Obtém a versão do AggilleDFe em execução (exibida no AppBar e na tela de login)");

        return app;
    }
}
