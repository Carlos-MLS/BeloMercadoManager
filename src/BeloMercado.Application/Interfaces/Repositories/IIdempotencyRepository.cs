using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IIdempotencyRepository
{
    Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task AddAsync(IdempotencyEntry entry, CancellationToken cancellationToken = default);
}
