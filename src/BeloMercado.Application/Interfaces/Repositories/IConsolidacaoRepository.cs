using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IConsolidacaoRepository
{
    Task<ConsolidacaoDiaria?> GetByLojaDataAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default);
    Task AddAsync(ConsolidacaoDiaria registo, CancellationToken cancellationToken = default);

    /// <summary>Devolve consolidações em estado Pendente ou Falha (com retry &lt;3) para o dia indicado.</summary>
    Task<IReadOnlyList<ConsolidacaoDiaria>> GetPendingAsync(DateOnly data, CancellationToken cancellationToken = default);

    Task UpsertKpiAsync(KPIDiario kpi, CancellationToken cancellationToken = default);
    Task ReplaceRankingAsync(Guid lojaId, DateOnly data, IEnumerable<RankingProdutoDiario> rankings, CancellationToken cancellationToken = default);

    Task<KPIDiario?> GetKpiAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KPIDiario>> GetKpisAsync(Guid? lojaId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RankingProdutoDiario>> GetRankingsAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default);
}
