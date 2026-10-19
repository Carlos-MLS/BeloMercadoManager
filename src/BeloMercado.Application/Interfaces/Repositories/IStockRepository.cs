using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IStockRepository
{
    Task<IReadOnlyList<Stock>> ListByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Stock>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Stock>> ListAlertasByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Stock>> ListAllAlertasAsync(CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Stock> stocks, CancellationToken cancellationToken = default);
    Task<Stock?> GetByLojaProdutoAsync(Guid lojaId, Guid produtoId, CancellationToken cancellationToken = default);
    Task<Stock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
