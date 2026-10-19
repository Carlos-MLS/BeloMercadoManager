using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class CaixaRepository : ICaixaRepository
{
    private readonly BeloMercadoDbContext _context;

    public CaixaRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<CaixaDiaria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CaixasDiarias.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CaixaDiaria?> GetOpenForLojaAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => _context.CaixasDiarias.FirstOrDefaultAsync(
            c => c.LojaId == lojaId && c.Data == data && c.Estado == EstadoCaixa.Aberta,
            cancellationToken);

    public Task<CaixaDiaria?> GetForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => _context.CaixasDiarias.FirstOrDefaultAsync(
            c => c.LojaId == lojaId && c.Data == data, cancellationToken);

    public Task<bool> ExistsForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => _context.CaixasDiarias.AnyAsync(c => c.LojaId == lojaId && c.Data == data, cancellationToken);

    public async Task AddAsync(CaixaDiaria caixa, CancellationToken cancellationToken = default)
    {
        await _context.CaixasDiarias.AddAsync(caixa, cancellationToken);
    }
}
