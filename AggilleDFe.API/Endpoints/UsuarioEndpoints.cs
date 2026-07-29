using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class UsuarioEndpoints
{
    public static IEndpointRouteBuilder MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/usuarios").WithTags("Usuários");

        group.MapGet("/", async (string? busca, IUsuarioService service) =>
            Results.Ok(await service.PesquisarAsync(busca)))
        .WithSummary("Pesquisa usuários por login ou nome");

        group.MapGet("/{id:int}", async (int id, IUsuarioService service) =>
        {
            var usuario = await service.ObterPorIdAsync(id);
            return usuario is null ? Results.NotFound() : Results.Ok(usuario);
        })
        .WithSummary("Obtém um usuário pelo id");

        group.MapPost("/", async (UsuarioDto dto, IUsuarioService service) =>
        {
            var (id, erros) = await service.IncluirAsync(dto);
            return erros is null ? Results.Ok(new { id }) : Results.ValidationProblem(erros);
        })
        .WithSummary("Inclui um novo usuário");

        group.MapPut("/{id:int}", async (int id, UsuarioDto dto, IUsuarioService service) =>
        {
            var (encontrado, erros) = await service.AtualizarAsync(id, dto);
            if (!encontrado)
            {
                return Results.NotFound();
            }

            return erros is null ? Results.Ok() : Results.ValidationProblem(erros);
        })
        .WithSummary("Atualiza os dados de um usuário existente");

        group.MapDelete("/{id:int}", async (int id, IUsuarioService service) =>
        {
            var encontrado = await service.ExcluirAsync(id);
            return encontrado ? Results.Ok() : Results.NotFound();
        })
        .WithSummary("Exclui um usuário");

        return app;
    }
}
