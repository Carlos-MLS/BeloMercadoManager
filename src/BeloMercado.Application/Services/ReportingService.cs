using BeloMercado.Application.Dtos.Dashboards;
using BeloMercado.Application.Interfaces.Repositories;

namespace BeloMercado.Application.Services;

/// <summary>UC13 — leitura dos agregados materializados pelo UC12.</summary>
public class ReportingService
{
    private readonly IConsolidacaoRepository _consolidacaoRepository;

    public ReportingService(IConsolidacaoRepository consolidacaoRepository)
    {
        _consolidacaoRepository = consolidacaoRepository;
    }

    public async Task<IReadOnlyList<KPIDiarioResponse>> GetKpisAsync(
        Guid? lojaId,
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancellationToken = default)
    {
        var kpis = await _consolidacaoRepository.GetKpisAsync(lojaId, inicio, fim, cancellationToken);
        return kpis.Select(k => new KPIDiarioResponse
        {
            LojaId = k.LojaId,
            Data = k.Data,
            TotalVendas = k.TotalVendas,
            NumTransacoes = k.NumTransacoes,
            TicketMedio = k.TicketMedio,
            TotalIva = k.TotalIva,
            TotalDescontos = k.TotalDescontos,
            VendasAnuladas = k.VendasAnuladas,
            Devolucoes = k.Devolucoes,
            UltimaAtualizacao = k.UltimaAtualizacao
        }).ToList();
    }

    public async Task<IReadOnlyList<RankingProdutoResponse>> GetRankingAsync(
        Guid lojaId,
        DateOnly data,
        CancellationToken cancellationToken = default)
    {
        var rankings = await _consolidacaoRepository.GetRankingsAsync(lojaId, data, cancellationToken);
        return rankings.Select(r => new RankingProdutoResponse
        {
            Posicao = r.Posicao,
            ProdutoId = r.ProdutoId,
            ProdutoEan13 = r.Produto?.Ean13 ?? string.Empty,
            ProdutoDescricao = r.Produto?.Descricao ?? string.Empty,
            QuantidadeVendida = r.QuantidadeVendida,
            Receita = r.Receita
        }).ToList();
    }
}
