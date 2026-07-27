namespace AggilleDFe.Application.DTOs;

public class ResultadoDistribuicaoLoteDto
{
    public int EmpresasProcessadas { get; set; }
    public int EmpresasComErro { get; set; }
    public int XmlsBaixadosNfe { get; set; }
    public int XmlsBaixadosCte { get; set; }
    public string? Mensagem { get; set; }
}
