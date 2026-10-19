using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly BeloMercadoDbContext _context;

    public ProdutoRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Produto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Produtos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Descricao)
            .ToListAsync(cancellationToken);
    }

    public Task<Produto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> Ean13ExistsAsync(string ean13, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Produtos.Where(p => p.Ean13 == ean13);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        await _context.Produtos.AddAsync(produto, cancellationToken);
    }

    public async Task AddHistoricoPrecoAsync(HistoricoPreco historico, CancellationToken cancellationToken = default)
    {
        await _context.HistoricoPrecos.AddAsync(historico, cancellationToken);
    }
}
