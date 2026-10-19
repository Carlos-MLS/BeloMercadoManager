using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class FornecedorRepository : IFornecedorRepository
{
    private readonly BeloMercadoDbContext _context;

    public FornecedorRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Fornecedor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Fornecedores
            .Include(f => f.ProdutosAssociados)
                .ThenInclude(pa => pa.Produto)
            .OrderBy(f => f.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Fornecedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Fornecedores
            .Include(f => f.ProdutosAssociados)
                .ThenInclude(pa => pa.Produto)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<bool> NifExistsAsync(string nif, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var q = _context.Fornecedores.Where(f => f.Nif == nif);
        if (excludeId.HasValue)
        {
            q = q.Where(f => f.Id != excludeId.Value);
        }
        return q.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        await _context.Fornecedores.AddAsync(fornecedor, cancellationToken);
    }

    public async Task RemoveAssociacoesAsync(Guid fornecedorId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<ProdutoFornecedor>()
            .Where(pa => pa.FornecedorId == fornecedorId)
            .ToListAsync(cancellationToken);
        _context.Set<ProdutoFornecedor>().RemoveRange(existing);
    }
}
