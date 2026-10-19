using BeloMercado.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeloMercado.Infrastructure.Persistence;

public class BeloMercadoDbContext : DbContext
{
    public BeloMercadoDbContext(DbContextOptions<BeloMercadoDbContext> options) : base(options)
    {
    }

    public DbSet<Utilizador> Utilizadores => Set<Utilizador>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();
    public DbSet<Loja> Lojas => Set<Loja>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<HistoricoPreco> HistoricoPrecos => Set<HistoricoPreco>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<CaixaDiaria> CaixasDiarias => Set<CaixaDiaria>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<LinhaVenda> LinhasVenda => Set<LinhaVenda>();
    public DbSet<Fatura> Faturas => Set<Fatura>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<IdempotencyEntry> IdempotencyKeys => Set<IdempotencyEntry>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Encomenda> Encomendas => Set<Encomenda>();
    public DbSet<LinhaEncomenda> LinhasEncomenda => Set<LinhaEncomenda>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();
    public DbSet<LinhaVendaPromocao> LinhasVendaPromocao => Set<LinhaVendaPromocao>();
    public DbSet<ConsolidacaoDiaria> ConsolidacoesDiarias => Set<ConsolidacaoDiaria>();
    public DbSet<KPIDiario> KPIsDiarios => Set<KPIDiario>();
    public DbSet<RankingProdutoDiario> RankingProdutosDiarios => Set<RankingProdutoDiario>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BeloMercadoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
