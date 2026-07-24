using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AggilleDFe.Application.DTOs;
using AggilleDFe.Application.Interfaces;

namespace AggilleDFe.Infrastructure.Integrations;

public class CnpjWsConsultaService(HttpClient httpClient) : ICnpjConsultaService
{
    public async Task<CnpjConsultaResultadoDto?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        var cnpjLimpo = new string(cnpj.Where(char.IsLetterOrDigit).ToArray());

        using var resposta = await httpClient.GetAsync($"cnpj/{cnpjLimpo}", cancellationToken);

        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        resposta.EnsureSuccessStatusCode();

        var empresa = await resposta.Content.ReadFromJsonAsync<CnpjWsRespostaModel>(cancellationToken);
        var estabelecimento = empresa?.Estabelecimento;

        if (empresa is null || estabelecimento is null)
        {
            return null;
        }

        return new CnpjConsultaResultadoDto
        {
            RazaoSocial = empresa.RazaoSocial ?? string.Empty,
            NomeFantasia = estabelecimento.NomeFantasia,
            SituacaoCadastral = estabelecimento.SituacaoCadastral,
            Logradouro = estabelecimento.Logradouro,
            Numero = estabelecimento.Numero,
            Complemento = estabelecimento.Complemento,
            Bairro = estabelecimento.Bairro,
            Cep = estabelecimento.Cep,
            Cidade = estabelecimento.Cidade?.Nome,
            Uf = estabelecimento.Estado?.Sigla,
            Ddd = estabelecimento.Ddd1,
            Telefone = estabelecimento.Telefone1
        };
    }

    private class CnpjWsRespostaModel
    {
        [JsonPropertyName("razao_social")]
        public string? RazaoSocial { get; set; }

        [JsonPropertyName("estabelecimento")]
        public EstabelecimentoModel? Estabelecimento { get; set; }
    }

    private class EstabelecimentoModel
    {
        [JsonPropertyName("nome_fantasia")]
        public string? NomeFantasia { get; set; }

        [JsonPropertyName("situacao_cadastral")]
        public string? SituacaoCadastral { get; set; }

        [JsonPropertyName("logradouro")]
        public string? Logradouro { get; set; }

        [JsonPropertyName("numero")]
        public string? Numero { get; set; }

        [JsonPropertyName("complemento")]
        public string? Complemento { get; set; }

        [JsonPropertyName("bairro")]
        public string? Bairro { get; set; }

        [JsonPropertyName("cep")]
        public string? Cep { get; set; }

        [JsonPropertyName("ddd1")]
        public string? Ddd1 { get; set; }

        [JsonPropertyName("telefone1")]
        public string? Telefone1 { get; set; }

        [JsonPropertyName("cidade")]
        public CidadeModel? Cidade { get; set; }

        [JsonPropertyName("estado")]
        public EstadoModel? Estado { get; set; }
    }

    private class CidadeModel
    {
        [JsonPropertyName("nome")]
        public string? Nome { get; set; }
    }

    private class EstadoModel
    {
        [JsonPropertyName("sigla")]
        public string? Sigla { get; set; }
    }
}
