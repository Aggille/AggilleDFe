namespace AggilleDFe.Application.DTOs;

public class LoginRequestDto
{
    public string Login { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public bool Administrador { get; set; }
    public bool AcessoXmlsBaixados { get; set; }
    public bool AcessoRegistros { get; set; }
    public bool AcessoEmpresas { get; set; }
    public bool AcessoConfiguracao { get; set; }
    public bool AcessoImportacao { get; set; }
    public bool AcessoBaixarXml { get; set; }
    public bool AcessoExportarXmls { get; set; }
}
