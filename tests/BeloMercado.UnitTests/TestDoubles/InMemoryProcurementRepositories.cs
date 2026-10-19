using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;

namespace BeloMercado.UnitTests.TestDoubles;

public class InMemoryFornecedorRepository : IFornecedorRepository
{
    public List<Fornecedor> Fornecedores { get; } = new();

    public Task<IReadOnlyList<Fornecedor>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Fornecedor>>(Fornecedores.ToList());

    public Task<Fornecedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Fornecedores.FirstOrDefault(f => f.Id == id));

    public Task<bool> NifExistsAsync(string nif, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Fornecedores.Any(f => f.Nif == nif && f.Id != excludeId));

    public Task AddAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        Fornecedores.Add(fornecedor);
        return Task.CompletedTask;
    }

    public Task RemoveAssociacoesAsync(Guid fornecedorId, CancellationToken cancellationToken = default)
    {
        var f = Fornecedores.FirstOrDefault(x => x.Id == fornecedorId);
        f?.ProdutosAssociados.Clear();
        return Task.CompletedTask;
    }
}

public class InMemoryEncomendaRepository : IEncomendaRepository
{
    public List<Encomenda> Encomendas { get; } = new();

    public Task<Encomenda?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Encomendas.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<Encomenda>> ListByLojaAsync(Guid lojaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Encomenda>>(Encomendas.Where(e => e.LojaId == lojaId).ToList());

    public Task<IReadOnlyList<Encomenda>> ListAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Encomenda>>(Encomendas.ToList());

    public Task AddAsync(Encomenda encomenda, CancellationToken cancellationToken = default)
    {
        Encomendas.Add(encomenda);
        return Task.CompletedTask;
    }

    public Task AddLinhasAsync(IEnumerable<LinhaEncomenda> linhas, CancellationToken cancellationToken = default)
    {
        // adicionamos directamente à colecção do parent
        foreach (var l in linhas)
        {
            var enc = Encomendas.FirstOrDefault(e => e.Id == l.EncomendaId);
            enc?.Linhas.Add(l);
        }
        return Task.CompletedTask;
    }

    public void RemoveLinhas(IEnumerable<LinhaEncomenda> linhas)
    {
        var ids = linhas.Select(l => l.Id).ToHashSet();
        foreach (var enc in Encomendas)
        {
            var toRemove = enc.Linhas.Where(l => ids.Contains(l.Id)).ToList();
            foreach (var l in toRemove) enc.Linhas.Remove(l);
        }
    }

    public void Remove(Encomenda encomenda) => Encomendas.Remove(encomenda);
}
