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

        var existente = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (existente is not null)
        {
            // Registro já existia (ex.: só o resumo, ou baixado antes deste campo
            // existir) — atualiza com o conteúdo real em vez de só pular, e grava
            // (ou regrava) o arquivo na pasta padrão da empresa, mesma convenção
            // da Distribuição DFe (ver GravarArquivoPadraoAsync).
            existente.ConteudoXml = conteudo;
            existente.NomeXml = await GravarArquivoPadraoAsync(existente.EmpresaId, infNFe.ide.dhEmi.DateTime, "NFe", chave, conteudo, arquivo, existente.NomeXml, resultado, cancellationToken);
            await xmlRepository.AtualizarAsync(existente, cancellationToken);
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

        var nomeXml = GravarArquivoPadrao(empresa, infNFe.ide.dhEmi.DateTime, "NFe", chave, conteudo, arquivo, resultado);

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
            NomeXml = nomeXml,
            ConteudoXml = conteudo,
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

        var existente = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (existente is not null)
        {
            existente.ConteudoXml = conteudo;
            existente.NomeXml = await GravarArquivoPadraoAsync(existente.EmpresaId, infCte.ide.dhEmi.DateTime, "CTe", chave, conteudo, arquivo, existente.NomeXml, resultado, cancellationToken);
            await xmlRepository.AtualizarAsync(existente, cancellationToken);
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

        var nomeXml = GravarArquivoPadrao(empresa, infCte.ide.dhEmi.DateTime, "CTe", chave, conteudo, arquivo, resultado);

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
            NomeXml = nomeXml,
            ConteudoXml = conteudo,
            Situacao = "Documento completo (importado)"
        };

        await xmlRepository.IncluirAsync(xml, cancellationToken);
        resultado.Importados++;
    }

    /// <summary>
    /// Grava o XML na pasta padrão da empresa (mesma convenção da Distribuição
    /// DFe, ver <see cref="Storage.CaminhoXmlHelper"/>) para um registro NOVO —
    /// a empresa já foi encontrada nesse caso. Falha de disco não impede a
    /// importação (fica registrada em <c>resultado.Erros</c>), e o caminho
    /// original do arquivo escaneado serve de fallback pro <c>NomeXml</c>.
    /// </summary>
    private static string GravarArquivoPadrao(Empresa empresa, DateTime dataEmissao, string tipoDocumento, string chave, string conteudo, string arquivoOrigem, ResultadoImportacaoXmlsDto resultado)
    {
        var (caminho, erro) = Storage.CaminhoXmlHelper.TentarGravarArquivo(empresa.PastaXml, empresa.Cnpj!, dataEmissao, tipoDocumento, chave, conteudo);
        if (erro is not null)
        {
            resultado.Erros.Add($"{Path.GetFileName(arquivoOrigem)}: falhou ao gravar em disco na pasta padrão ({erro}).");
        }

        return caminho ?? arquivoOrigem;
    }

    /// <summary>
    /// Mesma gravação de <see cref="GravarArquivoPadrao"/>, para um registro que
    /// JÁ existia (ex.: resumo, ou importado antes desta convenção existir) —
    /// aqui só temos o <c>EmpresaId</c> gravado no próprio <see cref="Xml"/>,
    /// então busca a <see cref="Empresa"/> por id. Sem empresa associada (ou sem
    /// CNPJ cadastrado), mantém o comportamento anterior: só preenche
    /// <c>NomeXml</c> se ainda estiver vazio, sem sobrescrever um caminho já
    /// válido.
    /// </summary>
    private async Task<string?> GravarArquivoPadraoAsync(int? empresaId, DateTime dataEmissao, string tipoDocumento, string chave, string conteudo, string arquivoOrigem, string? nomeXmlAtual, ResultadoImportacaoXmlsDto resultado, CancellationToken cancellationToken)
    {
        var empresa = empresaId is not null ? await empresaRepository.ObterPorIdAsync(empresaId.Value, cancellationToken) : null;
        if (empresa?.Cnpj is null)
        {
            return nomeXmlAtual ?? arquivoOrigem;
        }

        return GravarArquivoPadrao(empresa, dataEmissao, tipoDocumento, chave, conteudo, arquivoOrigem, resultado);
    }
}
