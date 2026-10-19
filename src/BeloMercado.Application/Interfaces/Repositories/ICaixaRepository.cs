using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface ICaixaRepository
{
    Task<CaixaDiaria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CaixaDiaria?> GetOpenForLojaAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default);
    Task<CaixaDiaria?> GetForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default);
    Task<bool> ExistsForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default);
    Task AddAsync(CaixaDiaria caixa, CancellationToken cancellationToken = default);
}
