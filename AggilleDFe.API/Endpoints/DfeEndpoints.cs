using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.API;

public static class DfeEndpoints
{
    public static IEndpointRouteBuilder MapDfeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dfe").WithTags("Integração DFe").RequireAuthorization("BasicApi");

        group.MapGet("/{chave}/xml", async (string chave, IXmlArquivoService xmlArquivoService, IEmpresaRepository empresaRepository, IDistribuicaoDfeService distribuicaoDfeService, CancellationToken cancellationToken) =>
        {
            var (conteudo, erro) = await xmlArquivoService.ObterXmlBrutoAsync(chave, cancellationToken);
            if (conteudo is not null)
            {
                return Results.File(conteudo, "application/xml", $"{chave}.xml");
            }

            // Não achou no banco/disco - antes de devolver 404, tenta baixar da
            // SEFAZ agora (fora do ciclo normal por NSU, ver BAIXAR_POR_CHAVE.md).
            // A chave não indica a empresa destinatária (só o emitente), então
            // testa cada empresa cadastrada ativa até uma achar o documento.
            var agora = DateTime.Now;
            var empresas = await empresaRepository.PesquisarAsync(null, cancellationToken);
            foreach (var empresa in empresas.Where(e => e.Inativo != "S" && !(e.BloqueadaAte > agora)))
            {
                var (resultado, _) = await distribuicaoDfeService.BaixarPorChaveAsync(empresa.Id, chave, cancellationToken);
                if (resultado?.DocumentoCompletoBaixado == true)
                {
                    (conteudo, erro) = await xmlArquivoService.ObterXmlBrutoAsync(chave, cancellationToken);
                    break;
                }
            }

            return conteudo is not null
                ? Results.File(conteudo, "application/xml", $"{chave}.xml")
                : Results.NotFound(new { erro });
        })
        .WithSummary("Retorna o XML de uma NFe pela chave de acesso — se ainda não estiver baixado, tenta baixar da SEFAZ na hora (ver BAIXAR_POR_CHAVE.md) antes de responder 404");

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
