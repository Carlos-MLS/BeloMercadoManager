using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<Utilizador?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Utilizador?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Utilizador>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ActiveGerenteExistsForLojaAsync(Guid lojaId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Utilizador utilizador, CancellationToken cancellationToken = default);
}
