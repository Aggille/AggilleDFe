namespace AggilleDFe.Application.DTOs;

public class StatusSefazResultadoDto
{
    public int CStat { get; set; }
    public string XMotivo { get; set; } = string.Empty;
    public string? Uf { get; set; }
    public string? Ambiente { get; set; }
    public string? VersaoLayout { get; set; }
    public DateTimeOffset? DhRecbto { get; set; }
    public int? TempoMedioMs { get; set; }
}
