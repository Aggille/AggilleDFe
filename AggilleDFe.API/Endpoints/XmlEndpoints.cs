using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.API;

public static class XmlEndpoints
{
    public static IEndpointRouteBuilder MapXmlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/xmls").WithTags("XMLs");

        group.MapGet("/", async (int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, DateOnly? emissaoInicial, DateOnly? emissaoFinal, IXmlService service) =>
            Results.Ok(await service.PesquisarAsync(empresaId, dataInicial, dataFinal, modelo, fornecedor, emissaoInicial, emissaoFinal)))
        .WithSummary("Pesquisa os XMLs baixados, com filtros opcionais por empresa, período (data de download), período de emissão (emissaoInicial/emissaoFinal), modelo (55=NFe/57=CTe) e nome do fornecedor");

        group.MapGet("/{chave}/arquivo", async (string chave, IXmlArquivoService xmlArquivoService) =>
        {
            var (conteudo, erro) = await xmlArquivoService.ObterXmlBrutoAsync(chave);
            return conteudo is not null
                ? Results.File(conteudo, "application/xml", $"{chave}.xml")
                : Results.NotFound(new { erro });
        })
        .WithSummary("Baixa o arquivo XML de um documento pela chave (uso interno da tela XMLs Baixados, sem autenticação — autenticação Basic é só para a API externa, /api/v1/dfe/*)");

        group.MapPost("/{chave}/salvar-em-disco", async (string chave, IXmlArquivoService xmlArquivoService) =>
        {
            var (caminho, erro) = await xmlArquivoService.SalvarEmDiscoAsync(chave);
            return caminho is not null ? Results.Ok(new { caminho }) : Results.BadRequest(new { erro });
        })
        .WithSummary("Grava (ou regrava) em disco, na pasta configurada da empresa, o XML cujo conteúdo já está no banco — uso interno da tela XMLs Baixados, para quando a gravação automática falhou ou foi para um caminho errado");

        group.MapGet("/{chave}/danfe", async (string chave, IDanfeService danfeService) =>
        {
            var (html, erro) = await danfeService.ObterDanfeHtmlAsync(chave);
            return html is not null ? Results.Content(html, "text/html") : Results.NotFound(new { erro });
        })
        .WithSummary("Retorna o DANFE (HTML, pronto para impressão) de uma NFe pela chave (uso interno da tela XMLs Baixados, sem autenticação — autenticação Basic é só para a API externa, /api/v1/dfe/*)");

        group.MapGet("/{chave}/dacte", async (string chave, IDacteService dacteService) =>
        {
            var (html, erro) = await dacteService.ObterDacteHtmlAsync(chave);
            return html is not null ? Results.Content(html, "text/html") : Results.NotFound(new { erro });
        })
        .WithSummary("Retorna o DACTE (HTML, pronto para impressão, layout próprio) de um CTe pela chave (uso interno da tela XMLs Baixados, sem autenticação — autenticação Basic é só para a API externa, /api/v1/dfe/*)");

        group.MapPost("/{chave}/manifestacao/ciencia", async (string chave, IManifestacaoService manifestacaoService) =>
        {
            var (sucesso, erro) = await manifestacaoService.ManifestarCienciaAsync(chave);
            return sucesso ? Results.Ok() : Results.BadRequest(new { erro });
        })
        .WithSummary("Manifesta Ciência da Operação para uma NFe (uso interno da tela XMLs Baixados)");

        group.MapPost("/{chave}/manifestacao/desconhecimento", async (string chave, ManifestacaoMotivoDto dto, IManifestacaoService manifestacaoService) =>
        {
            var (sucesso, erro) = await manifestacaoService.ManifestarDesconhecimentoAsync(chave, dto.Motivo);
            return sucesso ? Results.Ok() : Results.BadRequest(new { erro });
        })
        .WithSummary("Manifesta Desconhecimento da Operação para uma NFe (uso interno da tela XMLs Baixados)");

        group.MapPost("/{chave}/manifestacao/nao-realizada", async (string chave, ManifestacaoMotivoDto dto, IManifestacaoService manifestacaoService) =>
        {
            var (sucesso, erro) = await manifestacaoService.ManifestarNaoRealizadaAsync(chave, dto.Motivo);
            return sucesso ? Results.Ok() : Results.BadRequest(new { erro });
        })
        .WithSummary("Manifesta Operação Não Realizada para uma NFe (uso interno da tela XMLs Baixados)");

        group.MapPost("/importar", async (ImportarXmlsDto dto, IXmlImportService xmlImportService) =>
        {
            var (resultado, erro) = await xmlImportService.ImportarPastaAsync(dto.Pasta);
            return resultado is not null ? Results.Ok(resultado) : Results.BadRequest(new { erro });
        })
        .WithSummary("Varre uma pasta (recursivamente) e importa os XMLs (nfeProc/cteProc) ainda não cadastrados, associando pela empresa com o CNPJ do emitente");

        return app;
    }
}
