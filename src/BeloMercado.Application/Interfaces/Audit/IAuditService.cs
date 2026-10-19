namespace BeloMercado.Application.Interfaces.Audit;

public interface IAuditService
{
    /// <summary>
    /// Adds an audit log entry to the unit of work. Does NOT save changes —
    /// the caller is responsible for committing the transaction via
    /// <see cref="Persistence.IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    Task LogAsync(
        string acao,
        string resultado,
        Guid? utilizadorId,
        string ipAddress,
        string? targetEntidade = null,
        Guid? targetId = null,
        string? detalhes = null,
        CancellationToken cancellationToken = default);
}
