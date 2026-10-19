using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly BeloMercadoDbContext _context;

    public ClienteRepository(BeloMercadoDbContext context)
    {
        _context = context;
    }

    public Task<Cliente?> GetByNifAsync(string nif, CancellationToken cancellationToken = default)
        => _context.Clientes.FirstOrDefaultAsync(c => c.Nif == nif, cancellationToken);

    public async Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _context.Clientes.AddAsync(cliente, cancellationToken);
    }
}
