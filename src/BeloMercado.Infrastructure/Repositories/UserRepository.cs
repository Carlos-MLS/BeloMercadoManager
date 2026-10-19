using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly BeloMercadoDbContext _context;

    public UserRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<Utilizador?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Utilizadores.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<Utilizador?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Utilizadores.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Utilizador>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Utilizadores
            .OrderBy(u => u.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Utilizadores.Where(u => u.Email == email);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ActiveGerenteExistsForLojaAsync(Guid lojaId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Utilizadores.Where(u =>
            u.LojaId == lojaId
            && u.Perfil == Perfil.GerenteLoja
            && u.Ativo);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Utilizador utilizador, CancellationToken cancellationToken = default)
    {
        await _context.Utilizadores.AddAsync(utilizador, cancellationToken);
    }
}
