using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly BeloMercadoDbContext _context;

    public RefreshTokenRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
}
