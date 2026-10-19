using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IVendaRepository
{
    Task<Venda?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Fatura?> GetFaturaForVendaAsync(Guid vendaId, CancellationToken cancellationToken = default);
    Task<int> GetMaxFaturaSequencialAsync(Guid lojaId, int ano, CancellationToken cancellationToken = default);
    Task AddAsync(Venda venda, CancellationToken cancellationToken = default);
    Task AddFaturaAsync(Fatura fatura, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devolve as vendas de uma loja num dado dia (independente do estado).
    /// Inclui as Linhas. Usado pelo ConsolidationService (UC12).
    /// </summary>
    Task<IReadOnlyList<Venda>> GetVendasForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default);
}
