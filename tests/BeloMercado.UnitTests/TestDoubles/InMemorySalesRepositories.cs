using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;

namespace BeloMercado.UnitTests.TestDoubles;

public class InMemoryCaixaRepository : ICaixaRepository
{
    public List<CaixaDiaria> Caixas { get; } = new();

    public Task<CaixaDiaria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Caixas.FirstOrDefault(c => c.Id == id));

    public Task<CaixaDiaria?> GetOpenForLojaAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult(Caixas.FirstOrDefault(c => c.LojaId == lojaId && c.Data == data && c.Estado == EstadoCaixa.Aberta));

    public Task<CaixaDiaria?> GetForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult(Caixas.FirstOrDefault(c => c.LojaId == lojaId && c.Data == data));

    public Task<bool> ExistsForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult(Caixas.Any(c => c.LojaId == lojaId && c.Data == data));

    public Task AddAsync(CaixaDiaria caixa, CancellationToken cancellationToken = default)
    {
        Caixas.Add(caixa);
        return Task.CompletedTask;
    }
}

public class InMemoryVendaRepository : IVendaRepository
{
    public List<Venda> Vendas { get; } = new();
    public List<Fatura> Faturas { get; } = new();

    public Task<Venda?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Vendas.FirstOrDefault(v => v.Id == id));

    public Task<Fatura?> GetFaturaForVendaAsync(Guid vendaId, CancellationToken cancellationToken = default)
        => Task.FromResult(Faturas.FirstOrDefault(f => f.VendaId == vendaId));

    public Task<int> GetMaxFaturaSequencialAsync(Guid lojaId, int ano, CancellationToken cancellationToken = default)
    {
        var max = Faturas.Where(f => f.LojaId == lojaId && f.Ano == ano).Select(f => (int?)f.NumeroSequencial).DefaultIfEmpty().Max();
        return Task.FromResult(max ?? 0);
    }

    public Task AddAsync(Venda venda, CancellationToken cancellationToken = default)
    {
        Vendas.Add(venda);
        return Task.CompletedTask;
    }

    public Task AddFaturaAsync(Fatura fatura, CancellationToken cancellationToken = default)
    {
        Faturas.Add(fatura);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Venda>> GetVendasForLojaDayAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Venda>>(Vendas.Where(v => v.LojaId == lojaId && DateOnly.FromDateTime(v.DataHora.Date) == data).ToList());
}

public class InMemoryClienteRepository : IClienteRepository
{
    public List<Cliente> Clientes { get; } = new();

    public Task<Cliente?> GetByNifAsync(string nif, CancellationToken cancellationToken = default)
        => Task.FromResult(Clientes.FirstOrDefault(c => c.Nif == nif));

    public Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        Clientes.Add(cliente);
        return Task.CompletedTask;
    }
}

public class InMemoryIdempotencyRepository : IIdempotencyRepository
{
    public List<IdempotencyEntry> Entries { get; } = new();

    public Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(Entries.FirstOrDefault(e => e.Key == key));

    public Task AddAsync(IdempotencyEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

public class InMemoryPromocaoRepository : IPromocaoRepository
{
    public List<Promocao> Promocoes { get; } = new();
    public List<LinhaVendaPromocao> Linhas { get; } = new();

    public Task<IReadOnlyList<Promocao>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Promocao>>(Promocoes.ToList());

    public Task<Promocao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Promocoes.FirstOrDefault(p => p.Id == id));

    public Task<Promocao?> GetActiveForProdutoAsync(Guid produtoId, Guid lojaId, DateOnly hoje, CancellationToken cancellationToken = default)
        => Task.FromResult(Promocoes.FirstOrDefault(p =>
            p.Ativo
            && p.ProdutoId == produtoId
            && (p.LojaId == null || p.LojaId == lojaId)
            && p.DataInicio <= hoje
            && p.DataFim >= hoje));

    public Task AddAsync(Promocao promocao, CancellationToken cancellationToken = default)
    {
        Promocoes.Add(promocao);
        return Task.CompletedTask;
    }

    public Task AddLinhaVendaPromocaoAsync(LinhaVendaPromocao registo, CancellationToken cancellationToken = default)
    {
        Linhas.Add(registo);
        return Task.CompletedTask;
    }

    public Task<decimal> GetDescontoTotalForLinhasAsync(IEnumerable<Guid> linhaIds, CancellationToken cancellationToken = default)
    {
        var set = linhaIds.ToHashSet();
        return Task.FromResult(Linhas.Where(l => set.Contains(l.LinhaVendaId)).Sum(l => l.ValorDescontoAplicado));
    }
}

public class InMemoryConsolidacaoRepository : IConsolidacaoRepository
{
    public List<ConsolidacaoDiaria> Consolidacoes { get; } = new();
    public List<KPIDiario> Kpis { get; } = new();
    public List<RankingProdutoDiario> Rankings { get; } = new();

    public Task<ConsolidacaoDiaria?> GetByLojaDataAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult(Consolidacoes.FirstOrDefault(c => c.LojaId == lojaId && c.Data == data));

    public Task AddAsync(ConsolidacaoDiaria registo, CancellationToken cancellationToken = default)
    {
        Consolidacoes.Add(registo);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ConsolidacaoDiaria>> GetPendingAsync(DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ConsolidacaoDiaria>>(
            Consolidacoes.Where(c => c.Data == data
                && (c.Estado == EstadoConsolidacao.Falha || c.Estado == EstadoConsolidacao.Pendente)).ToList());

    public Task UpsertKpiAsync(KPIDiario kpi, CancellationToken cancellationToken = default)
    {
        Kpis.RemoveAll(k => k.LojaId == kpi.LojaId && k.Data == kpi.Data);
        Kpis.Add(kpi);
        return Task.CompletedTask;
    }

    public Task ReplaceRankingAsync(Guid lojaId, DateOnly data, IEnumerable<RankingProdutoDiario> rankings, CancellationToken cancellationToken = default)
    {
        Rankings.RemoveAll(r => r.LojaId == lojaId && r.Data == data);
        Rankings.AddRange(rankings);
        return Task.CompletedTask;
    }

    public Task<KPIDiario?> GetKpiAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult(Kpis.FirstOrDefault(k => k.LojaId == lojaId && k.Data == data));

    public Task<IReadOnlyList<KPIDiario>> GetKpisAsync(Guid? lojaId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KPIDiario>>(
            Kpis.Where(k => (lojaId == null || k.LojaId == lojaId)
                && k.Data >= inicio && k.Data <= fim).ToList());

    public Task<IReadOnlyList<RankingProdutoDiario>> GetRankingsAsync(Guid lojaId, DateOnly data, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RankingProdutoDiario>>(
            Rankings.Where(r => r.LojaId == lojaId && r.Data == data).OrderBy(r => r.Posicao).ToList());
}
