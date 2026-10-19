using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly BeloMercadoDbContext _context;

    public CategoriaRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categorias
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Categorias.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NomeExistsAsync(string nome, CancellationToken cancellationToken = default)
        => _context.Categorias.AnyAsync(c => c.Nome == nome, cancellationToken);

    public async Task AddAsync(Categoria categoria, CancellationToken cancellationToken = default)
    {
        await _context.Categorias.AddAsync(categoria, cancellationToken);
    }
}
