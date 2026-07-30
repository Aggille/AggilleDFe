namespace AggilleDFe.Application.Interfaces;

public interface IHtmlToPdfService
{
    Task<byte[]> ConverterAsync(string html, CancellationToken cancellationToken = default);
}
