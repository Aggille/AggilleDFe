namespace AggilleDFe.Application.DTOs;

public class UsuarioDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Nome { get; set; }

    /// <summary>Só precisa ser preenchida ao incluir; em branco na alteração mantém a senha atual.</summary>
    public string? Senha { get; set; }

    public bool Administrador { get; set; }
    public bool AcessoXmlsBaixados { get; set; }
    public bool AcessoRegistros { get; set; }
    public bool AcessoEmpresas { get; set; }
    public bool AcessoConfiguracao { get; set; }
    public bool AcessoImportacao { get; set; }
    public bool AcessoBaixarXml { get; set; }
    public bool Inativo { get; set; }
}
