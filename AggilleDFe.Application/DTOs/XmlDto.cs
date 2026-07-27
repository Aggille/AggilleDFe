namespace AggilleDFe.Application.DTOs;

public class XmlDto
{
    public int Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string? Protocolo { get; set; }
    public DateOnly? Emissao { get; set; }
    public DateOnly? DataDownload { get; set; }
    public string? FornecedorNome { get; set; }
    public string? FornecedorCnpj { get; set; }
    public string? FornecedorCidade { get; set; }
    public string? FornecedorUf { get; set; }
    public decimal? ValorTotal { get; set; }
    public decimal? ValorIcms { get; set; }
    public int? StatusNfe { get; set; }
    public string? MensagemNfe { get; set; }
    public string? NomeXml { get; set; }
    public int? Numero { get; set; }
    public string? Serie { get; set; }
    public string? Modelo { get; set; }
    public int? EmpresaId { get; set; }
    public bool Cancelada { get; set; }
    public string? Schema { get; set; }
    public string? Situacao { get; set; }
    public DateOnly? DataCancelamento { get; set; }
    public string? MotivoCancelamento { get; set; }
}
