using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Auth;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using BeloMercado.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeloMercado.IntegrationTests.Infrastructure;

/// <summary>
/// Constrói um stack de teste com:
///   - DbContext real apoiado pelo provider EF Core InMemory (isolado por GUID por teste)
///   - Repositórios REAIS de Infrastructure (todos)
///   - UnitOfWork real
///   - Fakes para serviços não-persistentes (clock, audit, password hasher, token gen)
///
/// Disclaimer: o provider EF InMemory é "good-enough" para testar fluxo e regras
/// de negócio; não simula constraints de SQL Server (uniqueness reforçada,
/// transações distribuídas). Onde a regra depende de uniqueness, validamos
/// que o serviço já a verifica em código antes de tocar no DB.
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public BeloMercadoDbContext Db { get; }
    public IUserRepository UserRepo { get; }
    public IRefreshTokenRepository RefreshTokenRepo { get; }
    public ILojaRepository LojaRepo { get; }
    public ICategoriaRepository CategoriaRepo { get; }
    public IProdutoRepository ProdutoRepo { get; }
    public IStockRepository StockRepo { get; }
    public ICaixaRepository CaixaRepo { get; }
    public IVendaRepository VendaRepo { get; }
    public IClienteRepository ClienteRepo { get; }
    public IIdempotencyRepository IdemRepo { get; }
    public IFornecedorRepository FornecedorRepo { get; }
    public IEncomendaRepository EncomendaRepo { get; }
    public IPromocaoRepository PromocaoRepo { get; }
    public IConsolidacaoRepository ConsolidacaoRepo { get; }
    public ILogAuditoriaRepository LogRepo { get; }
    public IUnitOfWork UnitOfWork { get; }
    public TestClock Clock { get; }
    public RecordingAuditService Audit { get; }
    public TestPasswordHasher Hasher { get; }
    public TestTokenGenerator Tokens { get; }

    public IntegrationTestFixture()
    {
        var dbName = $"belomercado-test-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<BeloMercadoDbContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            .Options;
        Db = new BeloMercadoDbContext(options);

        UserRepo = new UserRepository(Db);
        RefreshTokenRepo = new RefreshTokenRepository(Db);
        LojaRepo = new LojaRepository(Db);
        CategoriaRepo = new CategoriaRepository(Db);
        ProdutoRepo = new ProdutoRepository(Db);
        StockRepo = new StockRepository(Db);
        CaixaRepo = new CaixaRepository(Db);
        VendaRepo = new VendaRepository(Db);
        ClienteRepo = new ClienteRepository(Db);
        IdemRepo = new IdempotencyRepository(Db);
        FornecedorRepo = new FornecedorRepository(Db);
        EncomendaRepo = new EncomendaRepository(Db);
        PromocaoRepo = new PromocaoRepository(Db);
        ConsolidacaoRepo = new ConsolidacaoRepository(Db);
        LogRepo = new LogAuditoriaRepository(Db);
        UnitOfWork = new UnitOfWork(Db);
        Clock = new TestClock();
        Audit = new RecordingAuditService();
        Hasher = new TestPasswordHasher();
        Tokens = new TestTokenGenerator();
    }

    public InventoryService CreateInventoryService() =>
        new(StockRepo, LojaRepo, ProdutoRepo, UnitOfWork);

    public CatalogService CreateCatalogService() =>
        new(CategoriaRepo, ProdutoRepo, Audit, UnitOfWork, CreateInventoryService(), Clock);

    public StoreManagementService CreateStoreService() =>
        new(LojaRepo, Audit, UnitOfWork, CreateInventoryService(), Clock);

    public ConsolidationService CreateConsolidationService() =>
        new(ConsolidacaoRepo, VendaRepo, PromocaoRepo, Audit, UnitOfWork,
            NullLogger<ConsolidationService>.Instance, Clock);

    public SalesService CreateSalesService() =>
        new(CaixaRepo, VendaRepo, ClienteRepo, IdemRepo, StockRepo, ProdutoRepo,
            PromocaoRepo, LojaRepo, Audit, UnitOfWork, CreateConsolidationService(), Clock);

    public void Dispose() => Db.Dispose();
}

// ---------- pequenas implementações de teste ----------

public class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    public DateTime Now => UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.Date);
    public int OffsetDays { get; set; }
    public Task SetOffsetAsync(int offsetDays, CancellationToken cancellationToken = default) { OffsetDays = offsetDays; return Task.CompletedTask; }
    public Task ShiftAsync(int deltaDays, CancellationToken cancellationToken = default) { OffsetDays += deltaDays; UtcNow = UtcNow.AddDays(deltaDays); return Task.CompletedTask; }
}

public class RecordingAuditService : IAuditService
{
    public List<string> Acoes { get; } = new();
    public Task LogAsync(string acao, string resultado, Guid? utilizadorId, string ipAddress,
        string? targetEntidade = null, Guid? targetId = null, string? detalhes = null,
        CancellationToken cancellationToken = default)
    {
        Acoes.Add(acao);
        return Task.CompletedTask;
    }
}

public class TestPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;
    public bool Verify(string password, string? hash)
        => !string.IsNullOrEmpty(hash) && hash == "hashed:" + password;
}

public class TestTokenGenerator : ITokenGenerator
{
    private int _counter;
    public string GenerateAccessToken(Utilizador user) => $"AT:{user.Id}";
    public GeneratedRefreshToken GenerateRefreshToken()
    {
        _counter++;
        var raw = $"RT-{_counter}";
        return new GeneratedRefreshToken(raw, HashRefreshToken(raw));
    }
    public string HashRefreshToken(string rawToken) => "H:" + rawToken;
}
