using System.Text;
using AggilleDFe.Application.Interfaces;
using AggilleDFe.Domain.Interfaces;

namespace AggilleDFe.Infrastructure.Storage;

public class XmlArquivoService(IXmlRepository xmlRepository, IEmpresaRepository empresaRepository) : IXmlArquivoService
{
    public async Task<(byte[]? Conteudo, string? Erro)> ObterXmlBrutoAsync(string chave, CancellationToken cancellationToken = default)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (xml is null)
        {
            return (null, "Chave não encontrada.");
        }

        if (!string.IsNullOrEmpty(xml.ConteudoXml))
        {
            return (Encoding.UTF8.GetBytes(xml.ConteudoXml), null);
        }

        if (string.IsNullOrWhiteSpace(xml.NomeXml))
        {
            return (null, "O XML completo desse documento ainda não foi baixado (só o resumo está disponível).");
        }

        if (!File.Exists(xml.NomeXml))
        {
            return (null, "Arquivo XML não encontrado no disco.");
        }

        var conteudo = await File.ReadAllBytesAsync(xml.NomeXml, cancellationToken);
        return (conteudo, null);
    }

    public async Task<(string? Caminho, string? Erro)> SalvarEmDiscoAsync(string chave, CancellationToken cancellationToken = default)
    {
        var xml = await xmlRepository.ObterPorChaveAsync(chave, cancellationToken);
        if (xml is null)
        {
            return (null, "Chave não encontrada.");
        }

        if (string.IsNullOrEmpty(xml.ConteudoXml))
        {
            return (null, "Não há conteúdo do XML armazenado no banco para salvar em disco.");
        }

        if (xml.EmpresaId is null)
        {
            return (null, "XML sem empresa associada.");
        }

        var empresa = await empresaRepository.ObterPorIdAsync(xml.EmpresaId.Value, cancellationToken);
        if (empresa is null)
        {
            return (null, "Empresa não encontrada.");
        }

        if (string.IsNullOrWhiteSpace(empresa.Cnpj))
        {
            return (null, "A empresa não possui CNPJ cadastrado.");
        }

        var tipoDocumento = xml.Modelo switch
        {
            "55" => "NFe",
            "57" => "CTe",
            _ => xml.Modelo ?? "XML"
        };
        var dataEmissao = (xml.Emissao ?? DateOnly.FromDateTime(DateTime.Now)).ToDateTime(TimeOnly.MinValue);

        try
        {
            var caminho = CaminhoXmlHelper.GravarArquivo(empresa.PastaXml, empresa.Cnpj, dataEmissao, tipoDocumento, chave, xml.ConteudoXml);
            xml.NomeXml = caminho;
            await xmlRepository.AtualizarAsync(xml, cancellationToken);
            return (caminho, null);
        }
        catch (Exception ex)
        {
            return (null, $"Falha ao gravar XML em disco: {ex.Message}");
        }
    }
}
