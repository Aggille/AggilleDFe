namespace AggilleDFe.Application.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public int ExpiraMinutos { get; set; } = 600;
}
