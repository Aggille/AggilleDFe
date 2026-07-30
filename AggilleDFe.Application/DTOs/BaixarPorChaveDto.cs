namespace AggilleDFe.Application.DTOs;

public class BaixarPorChaveDto
{
    public string Chave { get; set; } = string.Empty;
}

public class ResultadoBaixarPorChaveDto
{
    public bool Encontrado { get; set; }
    public bool JaExistia { get; set; }
    public bool DocumentoCompletoBaixado { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}
