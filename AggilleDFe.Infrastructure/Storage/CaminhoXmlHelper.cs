namespace AggilleDFe.Infrastructure.Storage;

/// <summary>
/// Convenção de pasta compartilhada entre o download automático
/// (DistribuicaoDfeService) e o salvamento manual sob demanda
/// (XmlArquivoService): {PastaXml}/{Cnpj}/{ano}/{mes}/{NFe|CTe}/{chave}.xml
/// (ano/mês da emissão do documento, não do download).
/// </summary>
internal static class CaminhoXmlHelper
{
    public static string MontarCaminho(string pastaBase, string cnpj, DateTime dataEmissao, string tipoDocumento, string chave) =>
        Path.Combine(pastaBase, cnpj, dataEmissao.Year.ToString("D4"), dataEmissao.Month.ToString("D2"), tipoDocumento, $"{chave}.xml");

    public static string GravarArquivo(string? pastaBase, string cnpj, DateTime dataEmissao, string tipoDocumento, string chave, string conteudoXml)
    {
        if (string.IsNullOrWhiteSpace(pastaBase))
        {
            throw new InvalidOperationException("A empresa não possui uma pasta de XMLs configurada (PastaXml).");
        }

        var caminho = MontarCaminho(pastaBase, cnpj, dataEmissao, tipoDocumento, chave);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllText(caminho, conteudoXml);
        return caminho;
    }

    /// <summary>
    /// Mesma gravação de <see cref="GravarArquivo"/>, só que sem propagar exceção -
    /// usada pelos fluxos (Distribuição DFe, Importação) onde uma falha de disco
    /// (permissão, caminho errado, disco cheio) não pode impedir o registro no
    /// banco. Chamador loga <c>Erro</c> quando não nulo.
    /// </summary>
    public static (string? Caminho, string? Erro) TentarGravarArquivo(string? pastaBase, string cnpj, DateTime dataEmissao, string tipoDocumento, string chave, string conteudoXml)
    {
        try
        {
            return (GravarArquivo(pastaBase, cnpj, dataEmissao, tipoDocumento, chave, conteudoXml), null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
