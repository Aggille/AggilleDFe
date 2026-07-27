using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class DfeEndpoints
{
    public static IEndpointRouteBuilder MapDfeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dfe").WithTags("Integração DFe").RequireAuthorization("BasicApi");

        group.MapGet("/{chave}/xml", async (string chave, IXmlArquivoService xmlArquivoService) =>
        {
            var (conteudo, erro) = await xmlArquivoService.ObterXmlBrutoAsync(chave);
            return conteudo is not null
                ? Results.File(conteudo, "application/xml", $"{chave}.xml")
                : Results.NotFound(new { erro });
        })
        .WithSummary("Retorna o XML de uma NFe pela chave de acesso");

        group.MapPost("/{chave}/manifestacao/ciencia", async (string chave, IManifestacaoService manifestacaoService) =>
        {
            var (sucesso, erro) = await manifestacaoService.ManifestarCienciaAsync(chave);
            return sucesso ? Results.Ok() : Results.BadRequest(new { erro });
        })
        .WithSummary("Manifesta Ciência da Operação para uma NFe");

        group.MapPost("/{chave}/manifestacao/desconhecimento", async (string chave, ManifestacaoMotivoDto dto, IManifestacaoService manifestacaoService) =>
        {
            var (sucesso, erro) = await manifestacaoService.ManifestarDesconhecimentoAsync(chave, dto.Motivo);
            return sucesso ? Results.Ok() : Results.BadRequest(new { erro });
        })
        .WithSummary("Manifesta Desconhecimento da Operação para uma NFe (motivo obrigatório, 15 a 255 caracteres)");

        group.MapPost("/{chave}/manifestacao/nao-realizada", async (string chave, ManifestacaoMotivoDto dto, IManifestacaoService manifestacaoService) =>
        {
            var (sucesso, erro) = await manifestacaoService.ManifestarNaoRealizadaAsync(chave, dto.Motivo);
            return sucesso ? Results.Ok() : Results.BadRequest(new { erro });
        })
        .WithSummary("Manifesta Operação Não Realizada para uma NFe (motivo obrigatório, 15 a 255 caracteres)");

        return app;
    }
}
