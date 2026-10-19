using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IPromocaoRepository
{
    Task<IReadOnlyList<Promocao>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Promocao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devolve a promoção activa para o produto na loja na data dada
    /// (LojaId match exato OU NULL = "todas as lojas"). NULL se nenhuma.
    /// </summary>
    Task<Promocao?> GetActiveForProdutoAsync(
        Guid produtoId,
        Guid lojaId,
        DateOnly hoje,
        CancellationToken cancellationToken = default);

    Task AddAsync(Promocao promocao, CancellationToken cancellationToken = default);
    Task AddLinhaVendaPromocaoAsync(LinhaVendaPromocao registo, CancellationToken cancellationToken = default);

    /// <summary>Soma de descontos aplicados nas linhas indicadas (UC12 KPIs).</summary>
    Task<decimal> GetDescontoTotalForLinhasAsync(IEnumerable<Guid> linhaIds, CancellationToken cancellationToken = default);
}
