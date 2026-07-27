namespace AggilleDFe.Application.DTOs;

public class ResultadoBaixarXmlsDto
{
    public int XmlsBaixadosNfe { get; set; }
    public int XmlsBaixadosCte { get; set; }
    public int EventosProcessados { get; set; }
    public string? Mensagem { get; set; }
}
