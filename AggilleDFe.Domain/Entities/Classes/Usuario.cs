namespace AggilleDFe.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string SenhaHash { get; set; } = string.Empty;
    public string? Administrador { get; set; }
    public string? AcessoXmlsBaixados { get; set; }
    public string? AcessoRegistros { get; set; }
    public string? AcessoEmpresas { get; set; }
    public string? AcessoConfiguracao { get; set; }
    public string? AcessoImportacao { get; set; }
    public string? AcessoBaixarXml { get; set; }
    public string? AcessoExportarXmls { get; set; }
    public string? AcessoBaixarPorChave { get; set; }
    public string? Inativo { get; set; }
}
