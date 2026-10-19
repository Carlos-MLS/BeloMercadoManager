using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;

namespace BeloMercado.Infrastructure.Repositories;

public class LogAuditoriaRepository : ILogAuditoriaRepository
{
    private readonly BeloMercadoDbContext _context;

    public LogAuditoriaRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LogAuditoria log, CancellationToken cancellationToken = default)
    {
        await _context.LogsAuditoria.AddAsync(log, cancellationToken);
    }
}
