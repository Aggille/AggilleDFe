using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class ConfiguracaoEndpoints
{
    public static IEndpointRouteBuilder MapConfiguracaoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/configuracao").WithTags("Configuração");

        group.MapGet("/", async (IConfiguracaoService service) =>
        {
            var configuracao = await service.ObterAsync();
            return configuracao is null ? Results.NotFound() : Results.Ok(configuracao);
        })
        .WithSummary("Obtém a configuração global do sistema (único registro)");

        group.MapPut("/", async (ConfiguracaoDto dto, IConfiguracaoService service) =>
        {
            var erros = await service.SalvarAsync(dto);
            return erros is null ? Results.Ok() : Results.ValidationProblem(erros);
        })
        .WithSummary("Cria ou atualiza a configuração global do sistema");

        return app;
    }
}
