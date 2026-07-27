using AggilleDFe.Domain.Entities;

namespace AggilleDFe.Domain.Interfaces;

public interface IXmlRepository
{
    Task<Xml?> ObterPorChaveAsync(string chave, CancellationToken cancellationToken = default);
    Task IncluirAsync(Xml xml, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Xml xml, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Xml>> PesquisarAsync(int? empresaId, DateOnly? dataInicial, DateOnly? dataFinal, string? modelo, string? fornecedor, CancellationToken cancellationToken = default);
}
