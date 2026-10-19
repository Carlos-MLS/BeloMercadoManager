using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly BeloMercadoDbContext _context;

    public IdempotencyRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
        => _context.IdempotencyKeys.FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

    public async Task AddAsync(IdempotencyEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.IdempotencyKeys.AddAsync(entry, cancellationToken);
    }
}
