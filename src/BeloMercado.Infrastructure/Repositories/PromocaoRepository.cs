using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class PromocaoRepository : IPromocaoRepository
{
    private readonly BeloMercadoDbContext _context;

    public PromocaoRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Promocao>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Promocoes
            .Include(p => p.Produto)
            .OrderByDescending(p => p.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public Task<Promocao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Promocoes
            .Include(p => p.Produto)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Promocao?> GetActiveForProdutoAsync(
        Guid produtoId,
        Guid lojaId,
        DateOnly hoje,
        CancellationToken cancellationToken = default)
    {
        // Promoções específicas da loja têm precedência sobre globais (LojaId NULL).
        // Devolve a melhor (preferindo loja-específica e a de criação mais recente
        // como tie-breaker simples).
        var candidatos = await _context.Promocoes
            .Where(p => p.ProdutoId == produtoId
                     && p.Ativo
                     && p.DataInicio <= hoje
                     && p.DataFim >= hoje
                     && (p.LojaId == lojaId || p.LojaId == null))
            .ToListAsync(cancellationToken);

        return candidatos
            .OrderByDescending(p => p.LojaId.HasValue) // loja-específica primeiro
            .ThenByDescending(p => p.DataCriacao)
            .FirstOrDefault();
    }

    public async Task AddAsync(Promocao promocao, CancellationToken cancellationToken = default)
    {
        await _context.Promocoes.AddAsync(promocao, cancellationToken);
    }

    public async Task AddLinhaVendaPromocaoAsync(LinhaVendaPromocao registo, CancellationToken cancellationToken = default)
    {
        await _context.LinhasVendaPromocao.AddAsync(registo, cancellationToken);
    }

    public async Task<decimal> GetDescontoTotalForLinhasAsync(IEnumerable<Guid> linhaIds, CancellationToken cancellationToken = default)
    {
        var ids = linhaIds.ToList();
        if (ids.Count == 0) return 0m;
        return await _context.LinhasVendaPromocao
            .Where(lp => ids.Contains(lp.LinhaVendaId))
            .SumAsync(lp => (decimal?)lp.ValorDescontoAplicado, cancellationToken)
            ?? 0m;
    }
}
