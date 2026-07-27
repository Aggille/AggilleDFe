namespace AggilleDFe.Application.DTOs;

public class ResultadoImportacaoXmlsDto
{
    public int ArquivosEncontrados { get; set; }
    public int Importados { get; set; }
    public int JaExistiam { get; set; }
    public int EmpresaNaoEncontrada { get; set; }
    public int FormatoNaoReconhecido { get; set; }
    public List<string> Erros { get; set; } = [];
    public string? Mensagem { get; set; }
}
