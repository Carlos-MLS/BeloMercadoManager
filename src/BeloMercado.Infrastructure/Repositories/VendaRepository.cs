using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class VendaRepository : IVendaRepository
{
    private readonly BeloMercadoDbContext _context;

    public VendaRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<Venda?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Vendas
            .Include(v => v.Linhas)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Fatura?> GetFaturaForVendaAsync(Guid vendaId, CancellationToken cancellationToken = default)
        => _context.Faturas
            .Include(f => f.Cliente)
            .FirstOrDefaultAsync(f => f.VendaId == vendaId, cancellationToken);

    public async Task<int> GetMaxFaturaSequencialAsync(Guid lojaId, int ano, CancellationToken cancellationToken = default)
    {
        var max = await _context.Faturas
            .Where(f => f.LojaId == lojaId && f.Ano == ano)
            .Select(f => (int?)f.NumeroSequencial)
            .MaxAsync(cancellationToken);
        return max ?? 0;
    }

    public async Task AddAsync(Venda venda, CancellationToken cancellationToken = default)
    {
        await _context.Vendas.AddAsync(venda, cancellationToken);
    }

    public async Task AddFaturaAsync(Fatura fatura, CancellationToken cancellationToken = default)
    {
        await _context.Faturas.AddAsync(fatura, cancellationToken);
    }

    public async Task<IReadOnlyList<Venda>> GetVendasForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
    {
        // Filter via DataHora (DateTime). Convert DateOnly to date range.
        var inicio = data.ToDateTime(TimeOnly.MinValue);
        var fim = data.ToDateTime(TimeOnly.MaxValue);
        return await _context.Vendas
            .Include(v => v.Linhas)
            .Where(v => v.LojaId == lojaId
                     && v.DataHora >= inicio
                     && v.DataHora <= fim)
            .ToListAsync(cancellationToken);
    }
}
