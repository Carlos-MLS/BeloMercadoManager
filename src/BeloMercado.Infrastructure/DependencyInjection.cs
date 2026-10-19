using BeloMercado.Application.Interfaces.Auth;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Infrastructure.Auth;
using BeloMercado.Infrastructure.Hosted;
using BeloMercado.Infrastructure.Persistence;
using BeloMercado.Infrastructure.Repositories;
using BeloMercado.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeloMercado.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Set it via dotnet user-secrets in BeloMercado.Api.");

        services.AddDbContext<BeloMercadoDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ILogAuditoriaRepository, LogAuditoriaRepository>();
        services.AddScoped<ILojaRepository, LojaRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<ICaixaRepository, CaixaRepository>();
        services.AddScoped<IVendaRepository, VendaRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IFornecedorRepository, FornecedorRepository>();
        services.AddScoped<IEncomendaRepository, EncomendaRepository>();
        services.AddScoped<IPromocaoRepository, PromocaoRepository>();
        services.AddScoped<IConsolidacaoRepository, ConsolidacaoRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordGenerator, RandomPasswordGenerator>();
        services.AddSingleton<IClock, ShiftableClock>();

        services.AddHostedService<ConsolidationHostedService>();

        return services;
    }
}
