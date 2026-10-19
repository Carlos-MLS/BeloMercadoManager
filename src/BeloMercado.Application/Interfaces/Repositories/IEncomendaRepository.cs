using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IEncomendaRepository
{
    Task<Encomenda?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Encomenda>> ListByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Encomenda>> ListAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Encomenda encomenda, CancellationToken cancellationToken = default);
    Task AddLinhasAsync(IEnumerable<LinhaEncomenda> linhas, CancellationToken cancellationToken = default);
    void RemoveLinhas(IEnumerable<LinhaEncomenda> linhas);
    void Remove(Encomenda encomenda);
}
