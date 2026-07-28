namespace AggilleDFe.Application.DTOs;

public class DashboardDto
{
    public int EmpresasAtivas { get; set; }
    public int EmpresasBloqueadas { get; set; }
    public int CertificadosVencendoEm15Dias { get; set; }
    public int ErrosHoje { get; set; }
    public List<DashboardEmpresaResumoDto> Empresas { get; set; } = [];
}

public class DashboardEmpresaResumoDto
{
    public int EmpresaId { get; set; }
    public string? RazaoSocial { get; set; }
    public DateOnly? UltimaExecucaoData { get; set; }
    public TimeOnly? UltimaExecucaoHora { get; set; }
    public bool Bloqueada { get; set; }
    public DateTime? BloqueadaAte { get; set; }
    public int? CertificadoDiasRestantes { get; set; }
}
