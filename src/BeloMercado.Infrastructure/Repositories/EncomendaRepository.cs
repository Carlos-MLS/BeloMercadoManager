using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class EncomendaRepository : IEncomendaRepository
{
    private readonly BeloMercadoDbContext _context;

    public EncomendaRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<Encomenda?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Encomendas
            .Include(e => e.Linhas)
            .Include(e => e.Fornecedor)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Encomenda>> ListByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default)
    {
        return await _context.Encomendas
            .Include(e => e.Linhas)
            .Include(e => e.Fornecedor)
            .Where(e => e.LojaId == lojaId)
            .OrderByDescending(e => e.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Encomenda>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Encomendas
            .Include(e => e.Linhas)
            .Include(e => e.Fornecedor)
            .OrderByDescending(e => e.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Encomenda encomenda, CancellationToken cancellationToken = default)
    {
        await _context.Encomendas.AddAsync(encomenda, cancellationToken);
    }

    public void RemoveLinhas(IEnumerable<LinhaEncomenda> linhas)
    {
        _context.LinhasEncomenda.RemoveRange(linhas);
    }

    public async Task AddLinhasAsync(IEnumerable<LinhaEncomenda> linhas, CancellationToken cancellationToken = default)
    {
        // AddRange força estado Added — necessário para Guids gerados na app,
        // que a heurística do EF interpretaria como "entity existente que
        // precisa de UPDATE" se passassem só pelo navigation collection.
        await _context.LinhasEncomenda.AddRangeAsync(linhas, cancellationToken);
    }

    public void Remove(Encomenda encomenda)
    {
        _context.Encomendas.Remove(encomenda);
    }
}
