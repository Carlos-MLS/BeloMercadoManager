using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface IFornecedorRepository
{
    Task<IReadOnlyList<Fornecedor>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Fornecedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NifExistsAsync(string nif, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task RemoveAssociacoesAsync(Guid fornecedorId, CancellationToken cancellationToken = default);
}
