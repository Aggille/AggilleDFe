namespace AggilleDFe.Domain.Entities;

public class Configuracao
{
    public int Id { get; set; }
    public string? NomeEmpresa { get; set; }
    public string? CnpjEmpresa { get; set; }
    public int? VersaoBanco { get; set; }
    public int? TempoExecucao { get; set; }
    public int? QuantidadeEmpresasPermitidas { get; set; }
    public string? ApiAtiva { get; set; }
    public int? PortaApi { get; set; }
    public string? UsuarioApi { get; set; }
    public string? SenhaApi { get; set; }
    public string? ProcessarIndividualmente { get; set; }
    public int? UltimaEmpresaProcessadaId { get; set; }

    /// <summary>
    /// Versão do app em execução (ano.release.build, ex.: "2026.015.00001"),
    /// gravada automaticamente pela API a cada start a partir da versão
    /// compilada na imagem (ver Directory.Build.props e Program.cs) — não é
    /// editável pela tela de Configuração.
    /// </summary>
    public string? Versao { get; set; }
}
