using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface ILojaRepository
{
    Task<IReadOnlyList<Loja>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Loja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DesignacaoExistsAsync(string designacao, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Loja loja, CancellationToken cancellationToken = default);
}
