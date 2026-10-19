using BeloMercado.Application.Interfaces.Persistence;

namespace BeloMercado.UnitTests.TestDoubles;

/// <summary>
/// Fake de <see cref="IUnitOfWork"/> que apenas conta invocações.
/// Os repositórios em memória aplicam mudanças imediatamente, pelo que
/// SaveChanges não tem trabalho persistente; serve para verificar que
/// os serviços chamam efectivamente a transação no fim das operações.
/// </summary>
public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCallCount++;
        return Task.FromResult(0);
    }
}
