using System.IO.Compression;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Infrastructure.Integrations;

public class XmlExportService(IXmlRepository xmlRepository, IEmpresaRepository empresaRepository, IXmlArquivoService xmlArquivoService) : IXmlExportService
{
    public async Task<(byte[]? Zip, string? NomeArquivo, string? Erro)> ExportarZipAsync(int ano, int mes, int? empresaId, bool usarDataDownload = false, CancellationToken cancellationToken = default)
    {
        if (mes is < 1 or > 12)
        {
            return (null, null, "Mês inválido — informe um valor entre 1 e 12.");
        }

        var periodoInicial = new DateOnly(ano, mes, 1);
        var periodoFinal = periodoInicial.AddMonths(1).AddDays(-1);

        var todos = usarDataDownload
            ? await xmlRepository.PesquisarAsync(empresaId, periodoInicial, periodoFinal, null, null, null, null, cancellationToken)
            : await xmlRepository.PesquisarAsync(empresaId, null, null, null, null, periodoInicial, periodoFinal, cancellationToken);
        var elegiveis = todos.Where(x => x.Modelo is "55" or "57").ToList();

        if (elegiveis.Count == 0)
        {
            return (null, null, "Nenhum XML de NFe/CTe encontrado para o período/empresa informados.");
        }

        var cnpjsPorEmpresa = new Dictionary<int, string>();

        async Task<string> ObterCnpjAsync(int id)
        {
            if (cnpjsPorEmpresa.TryGetValue(id, out var cnpjCache))
            {
                return cnpjCache;
            }

            var empresa = await empresaRepository.ObterPorIdAsync(id, cancellationToken);
            var cnpj = empresa?.Cnpj ?? "SEM_CNPJ";
            cnpjsPorEmpresa[id] = cnpj;
            return cnpj;
        }

        using var memoria = new MemoryStream();
        var algumAdicionado = false;

        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var xml in elegiveis)
            {
                var (conteudo, _) = await xmlArquivoService.ObterXmlBrutoAsync(xml.Chave, cancellationToken);
                if (conteudo is null)
                {
                    continue;
                }

                var cnpj = xml.EmpresaId is int id ? await ObterCnpjAsync(id) : "SEM_EMPRESA";
                var tipoDocumento = xml.Modelo == "55" ? "NFe" : "CTe";
                var entrada = zip.CreateEntry($"{cnpj}/{tipoDocumento}/{xml.Chave}.xml", CompressionLevel.Optimal);

                await using var entradaStream = entrada.Open();
                await entradaStream.WriteAsync(conteudo, cancellationToken);
                algumAdicionado = true;
            }
        }

        if (!algumAdicionado)
        {
            return (null, null, "Os XMLs do período/empresa informados não foram encontrados no banco nem em disco.");
        }

        var sufixoEmpresa = empresaId is int idEmpresa && cnpjsPorEmpresa.TryGetValue(idEmpresa, out var cnpjEmpresa)
            ? $"_{cnpjEmpresa}"
            : string.Empty;
        var nomeArquivo = $"XMLs{sufixoEmpresa}_{ano:D4}-{mes:D2}.zip";

        return (memoria.ToArray(), nomeArquivo, null);
    }
}
