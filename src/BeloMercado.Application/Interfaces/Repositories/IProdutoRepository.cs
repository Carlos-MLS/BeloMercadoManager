using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Produto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> Ean13ExistsAsync(string ean13, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Produto produto, CancellationToken cancellationToken = default);
    Task AddHistoricoPrecoAsync(HistoricoPreco historico, CancellationToken cancellationToken = default);
}
