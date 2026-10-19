using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Repositories;

public interface ILogAuditoriaRepository
{
    Task AddAsync(LogAuditoria log, CancellationToken cancellationToken = default);
}
