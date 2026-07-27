namespace AggilleDFe.Application.DTOs;

public class LogDto
{
    public int Id { get; set; }
    public DateOnly? Data { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFinal { get; set; }
    public int? EmpresaId { get; set; }
    public int? QuantidadeXmls { get; set; }
    public string? Mensagem { get; set; }
    public int? XmlId { get; set; }
    public string? Chave { get; set; }
    public int? Nsu { get; set; }
}
