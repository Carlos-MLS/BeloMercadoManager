using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class ConsolidacaoRepository : IConsolidacaoRepository
{
    private readonly BeloMercadoDbContext _context;

    public ConsolidacaoRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<ConsolidacaoDiaria?> GetByLojaDataAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => _context.ConsolidacoesDiarias
            .FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Data == data, cancellationToken);

    public async Task AddAsync(ConsolidacaoDiaria registo, CancellationToken cancellationToken = default)
    {
        await _context.ConsolidacoesDiarias.AddAsync(registo, cancellationToken);
    }

    public async Task<IReadOnlyList<ConsolidacaoDiaria>> GetPendingAsync(DateOnly data, CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidacoesDiarias
            .Where(c => c.Data == data
                     && (c.Estado == EstadoConsolidacao.Pendente
                         || (c.Estado == EstadoConsolidacao.Falha && c.Tentativas < 3)))
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertKpiAsync(KPIDiario kpi, CancellationToken cancellationToken = default)
    {
        var existing = await _context.KPIsDiarios
            .FirstOrDefaultAsync(k => k.LojaId == kpi.LojaId && k.Data == kpi.Data, cancellationToken);
        if (existing is null)
        {
            await _context.KPIsDiarios.AddAsync(kpi, cancellationToken);
            return;
        }
        existing.TotalVendas = kpi.TotalVendas;
        existing.NumTransacoes = kpi.NumTransacoes;
        existing.TicketMedio = kpi.TicketMedio;
        existing.TotalIva = kpi.TotalIva;
        existing.TotalDescontos = kpi.TotalDescontos;
        existing.VendasAnuladas = kpi.VendasAnuladas;
        existing.Devolucoes = kpi.Devolucoes;
        existing.UltimaAtualizacao = kpi.UltimaAtualizacao;
    }

    public async Task ReplaceRankingAsync(Guid lojaId, DateOnly data, IEnumerable<RankingProdutoDiario> rankings, CancellationToken cancellationToken = default)
    {
        var existing = await _context.RankingProdutosDiarios
            .Where(r => r.LojaId == lojaId && r.Data == data)
            .ToListAsync(cancellationToken);
        _context.RankingProdutosDiarios.RemoveRange(existing);
        await _context.RankingProdutosDiarios.AddRangeAsync(rankings, cancellationToken);
    }

    public Task<KPIDiario?> GetKpiAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => _context.KPIsDiarios
            .FirstOrDefaultAsync(k => k.LojaId == lojaId && k.Data == data, cancellationToken);

    public async Task<IReadOnlyList<KPIDiario>> GetKpisAsync(Guid? lojaId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default)
    {
        var query = _context.KPIsDiarios.AsQueryable();
        if (lojaId.HasValue)
        {
            query = query.Where(k => k.LojaId == lojaId.Value);
        }
        return await query
            .Where(k => k.Data >= inicio && k.Data <= fim)
            .OrderBy(k => k.Data)
            .ThenBy(k => k.LojaId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RankingProdutoDiario>> GetRankingsAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
    {
        return await _context.RankingProdutosDiarios
            .Include(r => r.Produto)
            .Where(r => r.LojaId == lojaId && r.Data == data)
            .OrderBy(r => r.Posicao)
            .ToListAsync(cancellationToken);
    }
}
