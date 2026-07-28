namespace AggilleDFe.Application.Interfaces;

public interface IXmlArquivoService
{
    Task<(byte[]? Conteudo, string? Erro)> ObterXmlBrutoAsync(string chave, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grava em disco (na pasta configurada da empresa) o XML cujo conteúdo já está
    /// armazenado no banco (Xml.ConteudoXml) — usado quando a gravação automática
    /// original falhou ou foi para um caminho errado.
    /// </summary>
    Task<(string? Caminho, string? Erro)> SalvarEmDiscoAsync(string chave, CancellationToken cancellationToken = default);
}
