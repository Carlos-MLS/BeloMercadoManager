using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly BeloMercadoDbContext _context;

    public StockRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Stock>> ListByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Loja)
            .Include(s => s.Produto)
            .Where(s => s.LojaId == lojaId)
            .OrderBy(s => s.Produto!.Descricao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Stock>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Loja)
            .Include(s => s.Produto)
            .OrderBy(s => s.Loja!.Designacao)
            .ThenBy(s => s.Produto!.Descricao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Stock>> ListAlertasByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Loja)
            .Include(s => s.Produto)
            .Where(s => s.LojaId == lojaId && s.Quantidade < s.QtdMinima)
            .OrderBy(s => s.Produto!.Descricao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Stock>> ListAllAlertasAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Loja)
            .Include(s => s.Produto)
            .Where(s => s.Quantidade < s.QtdMinima)
            .OrderBy(s => s.Loja!.Designacao)
            .ThenBy(s => s.Produto!.Descricao)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Stock> stocks, CancellationToken cancellationToken = default)
    {
        await _context.Stocks.AddRangeAsync(stocks, cancellationToken);
    }

    public Task<Stock?> GetByLojaProdutoAsync(Guid lojaId, Guid produtoId, CancellationToken cancellationToken = default)
        => _context.Stocks.FirstOrDefaultAsync(
            s => s.LojaId == lojaId && s.ProdutoId == produtoId, cancellationToken);

    public Task<Stock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Stocks
            .Include(s => s.Loja)
            .Include(s => s.Produto)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}
