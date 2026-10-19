using BeloMercado.Application.Interfaces.Persistence;

namespace BeloMercado.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly BeloMercadoDbContext _context;

    public UnitOfWork(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
