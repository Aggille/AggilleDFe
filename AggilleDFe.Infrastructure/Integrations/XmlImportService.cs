using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Entities;
using AggilleDFe.Domain.Interfaces;
using DFe.Utils;
using Shared.DFe.Utils;

namespace AggilleDFe.Infrastructure.Integrations;

public class XmlImportService(IEmpresaRepository empresaRepository, IXmlRepository xmlRepository) : IXmlImportService
{
    public async Task<(ResultadoImportacaoXmlsDto? Resultado, string? Erro)> ImportarPastaAsync(string pasta, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pasta) || !Directory.Exists(pasta))
        {
            return (null, "Pasta não encontrada.");
        }

        var resultado = new ResultadoImportacaoXmlsDto();
        var arquivos = Directory.EnumerateFiles(pasta, "*.xml", SearchOption.AllDirectories).ToList();
        resultado.ArquivosEncontrados = arquivos.Count;

        foreach (var arquivo in arquivos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var conteudo = (await File.ReadAllTextAsync(arquivo, cancellationToken)).RemoverDeclaracaoXml();

                if (conteudo.StartsWith("<nfeProc", StringComparison.Ordinal))
                {
                    await ImportarNfeAsync(conteudo, arquivo, resultado, cancellationToken);
                }
                else if (conteudo.StartsWith("<cteProc", StringComparison.Ordinal))
                {
                    await ImportarCteAsync(conteudo, arquivo, resultado, cancellationToken);
                }
                else
                {
                    resultado.FormatoNaoReconhecido++;
                }
            }
            catch (Exception ex)
            {
                resultado.Erros.Add($"{Path.GetFileName(arquivo)}: {ex.Message}");
            }
        }

        resultado.Mensagem = $"{resultado.ArquivosEncontrados} arquivo(s) encontrado(s) — {resultado.Importados} importado(s), " +
            $"{resultado.JaExistiam} já existente(s), {resultado.EmpresaNaoEncontrada} sem empresa correspondente, " +
            $"{resultado.FormatoNaoReconhecido} com formato não reconhecido" +
            (resultado.Erros.Count > 0 ? $", {resultado.Erros.Count} com erro." : ".");

        return (resultado, null);
    }

    private async Task ImportarNfeAsync(string conteudo, string arquivo, ResultadoImportacaoXmlsDto resultado, CancellationToken cancellationToken)
    {
        var doc = FuncoesXml.XmlStringParaClasse<NFe.Classes.nfeProc>(conteudo);
        var infNFe = doc.NFe.infNFe;
        var infProt = doc.protNFe.infProt;
        var chave = infProt.chNFe;

        if (await xmlRepository.ObterPorChaveAsync(chave, cancellationToken) is not null)
        {
            resultado.JaExistiam++;
            return;
        }

        if (string.IsNullOrWhiteSpace(infNFe.dest?.CNPJ))
        {
            resultado.EmpresaNaoEncontrada++;
            return;
        }

        var empresa = await empresaRepository.ObterPorCnpjAsync(infNFe.dest.CNPJ, cancellationToken);
        if (empresa is null)
        {
            resultado.EmpresaNaoEncontrada++;
            return;
        }

        var xml = new Xml
        {
            Chave = chave,
            EmpresaId = empresa.Id,
            Protocolo = infProt.nProt,
            Emissao = DateOnly.FromDateTime(infNFe.ide.dhEmi.DateTime),
            DataDownload = DateOnly.FromDateTime(DateTime.Now),
            FornecedorNome = infNFe.emit.xNome,
            FornecedorCnpj = infNFe.emit.CNPJ,
            FornecedorCidade = infNFe.emit.enderEmit.xMun,
            FornecedorUf = infNFe.emit.enderEmit.UF.ToString(),
            ValorTotal = infNFe.total.ICMSTot.vNF,
            ValorIcms = infNFe.total.ICMSTot.vICMS,
            StatusNfe = infProt.cStat,
            MensagemNfe = infProt.xMotivo,
            Numero = (int)infNFe.ide.nNF,
            Serie = infNFe.ide.serie.ToString(),
            Modelo = "55",
            NomeXml = arquivo,
            Situacao = "Documento completo (importado)"
        };

        await xmlRepository.IncluirAsync(xml, cancellationToken);
        resultado.Importados++;
    }

    private async Task ImportarCteAsync(string conteudo, string arquivo, ResultadoImportacaoXmlsDto resultado, CancellationToken cancellationToken)
    {
        var doc = FuncoesXml.XmlStringParaClasse<CTe.Classes.cteProc>(conteudo);
        var infCte = doc.CTe.infCte;
        var infProt = doc.protCTe.infProt;
        var chave = infProt.chCTe;

        if (await xmlRepository.ObterPorChaveAsync(chave, cancellationToken) is not null)
        {
            resultado.JaExistiam++;
            return;
        }

        if (string.IsNullOrWhiteSpace(infCte.dest?.CNPJ))
        {
            resultado.EmpresaNaoEncontrada++;
            return;
        }

        var empresa = await empresaRepository.ObterPorCnpjAsync(infCte.dest.CNPJ, cancellationToken);
        if (empresa is null)
        {
            resultado.EmpresaNaoEncontrada++;
            return;
        }

        var xml = new Xml
        {
            Chave = chave,
            EmpresaId = empresa.Id,
            Protocolo = infProt.nProt,
            Emissao = DateOnly.FromDateTime(infCte.ide.dhEmi.DateTime),
            DataDownload = DateOnly.FromDateTime(DateTime.Now),
            FornecedorNome = infCte.emit.xNome,
            FornecedorCnpj = infCte.emit.CNPJ,
            FornecedorCidade = infCte.emit.enderEmit.xMun,
            FornecedorUf = infCte.emit.enderEmit.UF.ToString(),
            ValorTotal = infCte.vPrest.vTPrest,
            StatusNfe = infProt.cStat,
            MensagemNfe = infProt.xMotivo,
            Numero = (int)infCte.ide.nCT,
            Serie = infCte.ide.serie.ToString(),
            Modelo = "57",
            NomeXml = arquivo,
            Situacao = "Documento completo (importado)"
        };

        await xmlRepository.IncluirAsync(xml, cancellationToken);
        resultado.Importados++;
    }
}
