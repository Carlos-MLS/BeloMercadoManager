using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class LojaRepository : ILojaRepository
{
    private readonly BeloMercadoDbContext _context;

    public LojaRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Loja>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Lojas
            .OrderBy(l => l.Designacao)
            .ToListAsync(cancellationToken);
    }

    public Task<Loja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Lojas.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> DesignacaoExistsAsync(string designacao, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Lojas.Where(l => l.Designacao == designacao);
        if (excludeId.HasValue)
        {
            query = query.Where(l => l.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Loja loja, CancellationToken cancellationToken = default)
    {
        await _context.Lojas.AddAsync(loja, cancellationToken);
    }
}
