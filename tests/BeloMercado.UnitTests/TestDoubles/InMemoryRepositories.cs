using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;

namespace BeloMercado.UnitTests.TestDoubles;

// ---------- User ----------

public class InMemoryUserRepository : IUserRepository
{
    public List<Utilizador> Users { get; } = new();

    public Task<Utilizador?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<Utilizador?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<Utilizador>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Utilizador>>(Users.ToList());

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.Id != excludeId));

    public Task<bool> ActiveGerenteExistsForLojaAsync(Guid lojaId, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.Any(u =>
            u.LojaId == lojaId &&
            u.Ativo &&
            u.Perfil == Perfil.GerenteLoja &&
            u.Id != excludeId));

    public Task AddAsync(Utilizador utilizador, CancellationToken cancellationToken = default)
    {
        Users.Add(utilizador);
        return Task.CompletedTask;
    }
}

// ---------- RefreshToken ----------

public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = new();

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
}

// ---------- Loja ----------

public class InMemoryLojaRepository : ILojaRepository
{
    public List<Loja> Lojas { get; } = new();

    public Task<IReadOnlyList<Loja>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Loja>>(Lojas.ToList());

    public Task<Loja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Lojas.FirstOrDefault(l => l.Id == id));

    public Task<bool> DesignacaoExistsAsync(string designacao, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Lojas.Any(l =>
            l.Designacao.Equals(designacao, StringComparison.OrdinalIgnoreCase) && l.Id != excludeId));

    public Task AddAsync(Loja loja, CancellationToken cancellationToken = default)
    {
        Lojas.Add(loja);
        return Task.CompletedTask;
    }
}
