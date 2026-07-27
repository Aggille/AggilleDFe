namespace AggilleDFe.Application.Interfaces;

public interface IXmlArquivoService
{
    Task<(byte[]? Conteudo, string? Erro)> ObterXmlBrutoAsync(string chave, CancellationToken cancellationToken = default);
}
