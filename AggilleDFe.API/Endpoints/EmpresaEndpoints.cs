using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class EmpresaEndpoints
{
    public static IEndpointRouteBuilder MapEmpresaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/empresas").WithTags("Empresas");

        group.MapGet("/", async (string? busca, IEmpresaService service) =>
            Results.Ok(await service.PesquisarAsync(busca)))
        .WithSummary("Pesquisa empresas por razão social ou C.N.P.J.");

        group.MapGet("/{id:int}", async (int id, IEmpresaService service) =>
        {
            var empresa = await service.ObterPorIdAsync(id);
            return empresa is null ? Results.NotFound() : Results.Ok(empresa);
        })
        .WithSummary("Obtém uma empresa pelo id");

        group.MapPost("/", async (EmpresaDto dto, IEmpresaService service) =>
        {
            var (id, erros) = await service.IncluirAsync(dto);
            return erros is null ? Results.Ok(new { id }) : Results.ValidationProblem(erros);
        })
        .WithSummary("Inclui uma nova empresa");

        group.MapPut("/{id:int}", async (int id, EmpresaDto dto, IEmpresaService service) =>
        {
            var (encontrado, erros) = await service.AtualizarAsync(id, dto);
            if (!encontrado)
            {
                return Results.NotFound();
            }

            return erros is null ? Results.Ok() : Results.ValidationProblem(erros);
        })
        .WithSummary("Atualiza os dados de uma empresa existente");

        group.MapDelete("/{id:int}", async (int id, IEmpresaService service) =>
        {
            var encontrado = await service.ExcluirAsync(id);
            return encontrado ? Results.Ok() : Results.NotFound();
        })
        .WithSummary("Exclui uma empresa");

        group.MapGet("/{id:int}/status-sefaz", async (int id, ISefazStatusService sefazStatusService) =>
        {
            var (resultado, erro) = await sefazStatusService.ConsultarStatusAsync(id);
            return resultado is not null ? Results.Ok(resultado) : Results.BadRequest(new { erro });
        })
        .WithSummary("Consulta o status do serviço da SEFAZ para a UF da empresa, via Zeus DFe.NET");

        group.MapPost("/{id:int}/baixar-xmls", async (int id, IDistribuicaoDfeService distribuicaoDfeService) =>
        {
            var (resultado, erro) = await distribuicaoDfeService.ExecutarAsync(id, execucaoManual: true);
            return resultado is not null ? Results.Ok(resultado) : Results.BadRequest(new { erro });
        })
        .WithSummary("Executa manualmente a Distribuição DFe (NFe e CTe) da empresa, baixando os XMLs disponíveis a partir do último NSU");

        group.MapPost("/baixar-xmls", async (IDistribuicaoLoteService distribuicaoLoteService) =>
            Results.Ok(await distribuicaoLoteService.ExecutarTodasAsync(execucaoManual: true)))
        .WithSummary("Executa manualmente a Distribuição DFe para todas as empresas elegíveis, respeitando Configuracao.ProcessarIndividualmente");

        group.MapGet("/consulta-cnpj/{cnpj}", async (string cnpj, ICnpjConsultaService consultaService) =>
        {
            var resultado = await consultaService.ConsultarAsync(cnpj);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        })
        .WithSummary("Consulta dados cadastrais de uma empresa pelo C.N.P.J. (CNPJ.ws)");

        return app;
    }
}
