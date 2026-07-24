namespace AggilleDFe.Application.DTOs;

public class ConfiguracaoDto
{
    public int Id { get; set; }
    public string? CnpjEmpresa { get; set; }
    public string? NomeEmpresa { get; set; }
    public int? TempoExecucao { get; set; }
    public int? PortaApi { get; set; }
    public string? UsuarioApi { get; set; }
    public string? SenhaApi { get; set; }
    public bool ApiAtiva { get; set; }
    public bool ProcessarIndividualmente { get; set; }
}
