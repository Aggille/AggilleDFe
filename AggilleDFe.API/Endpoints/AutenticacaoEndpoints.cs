using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class AutenticacaoEndpoints
{
    public static IEndpointRouteBuilder MapAutenticacaoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Autenticação");

        group.MapPost("/login", async (LoginRequestDto dto, IAutenticacaoService service) =>
        {
            var resultado = await service.AutenticarAsync(dto);
            return resultado is null ? Results.Unauthorized() : Results.Ok(resultado);
        })
        .WithSummary("Autentica um usuário da plataforma (login/senha) e devolve um token JWT");

        return app;
    }
}
