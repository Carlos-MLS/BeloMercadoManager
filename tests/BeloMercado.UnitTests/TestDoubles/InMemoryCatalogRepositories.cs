using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;

namespace BeloMercado.UnitTests.TestDoubles;

public class InMemoryCategoriaRepository : ICategoriaRepository
{
    public List<Categoria> Categorias { get; } = new();

    public Task<IReadOnlyList<Categoria>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Categoria>>(Categorias.ToList());

    public Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id));

    public Task<bool> NomeExistsAsync(string nome, CancellationToken cancellationToken = default)
        => Task.FromResult(Categorias.Any(c => c.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Categoria categoria, CancellationToken cancellationToken = default)
    {
        Categorias.Add(categoria);
        return Task.CompletedTask;
    }
}

public class InMemoryProdutoRepository : IProdutoRepository
{
    public List<Produto> Produtos { get; } = new();
    public List<HistoricoPreco> Historico { get; } = new();

    public Task<IReadOnlyList<Produto>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Produto>>(Produtos.ToList());

    public Task<Produto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Produtos.FirstOrDefault(p => p.Id == id));

    public Task<bool> Ean13ExistsAsync(string ean13, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Produtos.Any(p => p.Ean13 == ean13 && p.Id != excludeId));

    public Task AddAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        Produtos.Add(produto);
        return Task.CompletedTask;
    }

    public Task AddHistoricoPrecoAsync(HistoricoPreco historico, CancellationToken cancellationToken = default)
    {
        Historico.Add(historico);
        return Task.CompletedTask;
    }
}

public class InMemoryStockRepository : IStockRepository
{
    public List<Stock> Stocks { get; } = new();

    public Task<IReadOnlyList<Stock>> ListByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Stock>>(Stocks.Where(s => s.LojaId == lojaId).ToList());

    public Task<IReadOnlyList<Stock>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Stock>>(Stocks.ToList());

    public Task<IReadOnlyList<Stock>> ListAlertasByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Stock>>(
            Stocks.Where(s => s.LojaId == lojaId && s.Quantidade < s.QtdMinima).ToList());

    public Task<IReadOnlyList<Stock>> ListAllAlertasAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Stock>>(Stocks.Where(s => s.Quantidade < s.QtdMinima).ToList());

    public Task AddRangeAsync(IEnumerable<Stock> stocks, CancellationToken cancellationToken = default)
    {
        Stocks.AddRange(stocks);
        return Task.CompletedTask;
    }

    public Task<Stock?> GetByLojaProdutoAsync(Guid lojaId, Guid produtoId, CancellationToken cancellationToken = default)
        => Task.FromResult(Stocks.FirstOrDefault(s => s.LojaId == lojaId && s.ProdutoId == produtoId));

    public Task<Stock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Stocks.FirstOrDefault(s => s.Id == id));
}
