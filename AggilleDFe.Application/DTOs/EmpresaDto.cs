namespace AggilleDFe.Application.DTOs;

public class EmpresaDto
{
    public int Id { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;

    public string? CertificadoDigital { get; set; }
    public string? SenhaCertificado { get; set; }
    public string? PastaXml { get; set; }
    public int? UltimoNsu { get; set; }
    public string? Ambiente { get; set; }
    public int? Timeout { get; set; }
    public int? TempoRetorno { get; set; }
    public int? IntervaloTentativas { get; set; }
    public int? QuantidadeTentativas { get; set; }
    public string? EmailEnvioNotificacoes { get; set; }
    public string? ServidorSmtp { get; set; }
    public string? UsuarioSmtp { get; set; }
    public string? SenhaSmtp { get; set; }
    public string? EmailSmtp { get; set; }
    public int? TipoAutenticacaoSmtp { get; set; }
    public string? ServidorPop { get; set; }
    public string? UsuarioPop { get; set; }
    public string? EmailPop { get; set; }
    public string? SenhaPop { get; set; }
    public int? TipoAutenticacaoPop { get; set; }
    public int? PortaPop { get; set; }
    public int? PortaSmtp { get; set; }
    public string? Ie { get; set; }
    public bool Manifesta { get; set; }
    public int? Posicao { get; set; }
    public bool Inativo { get; set; }
    public int? UltimoNsuCte { get; set; }
    public TimeOnly? HoraInicial { get; set; }
    public TimeOnly? HoraFinal { get; set; }
}
