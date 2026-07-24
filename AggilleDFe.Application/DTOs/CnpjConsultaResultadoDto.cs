namespace AggilleDFe.Application.DTOs;

public class CnpjConsultaResultadoDto
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? SituacaoCadastral { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cep { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Ddd { get; set; }
    public string? Telefone { get; set; }
}
