using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface ICategoriaRepository
{
    Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NomeExistsAsync(string nome, CancellationToken cancellationToken = default);
    Task AddAsync(Categoria categoria, CancellationToken cancellationToken = default);
}
