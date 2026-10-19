using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.UnitTests.TestDoubles;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// CatalogService — UC05 (Catálogo): categorias, produtos, validação EAN-13,
/// histórico de preços. Cobre regras:
///   RN05a EAN-13 sempre validado antes de criar.
///   RN05b Sem duplicado de EAN.
///   RN05c Sem duplicado de nome de categoria.
///   RN05d Categoria pai tem de existir.
///   RN05e Alteração de preço materializa um HistoricoPreco.
///   RN05f Stock é materializado para todas as lojas quando se cria um produto.
/// </summary>
public class CatalogServiceTests
{
    private static (CatalogService svc,
        InMemoryCategoriaRepository cats,
        InMemoryProdutoRepository produtos,
        InMemoryLojaRepository lojas,
        InMemoryStockRepository stocks,
        FakeAuditService audit,
        FakeUnitOfWork uow,
        FakeClock clock) Build()
    {
        var cats = new InMemoryCategoriaRepository();
        var produtos = new InMemoryProdutoRepository();
        var lojas = new InMemoryLojaRepository();
        var stocks = new InMemoryStockRepository();
        var audit = new FakeAuditService();
        var uow = new FakeUnitOfWork();
        var clock = new FakeClock();
        var inventory = new InventoryService(stocks, lojas, produtos, uow);
        var svc = new CatalogService(cats, produtos, audit, uow, inventory, clock);
        return (svc, cats, produtos, lojas, stocks, audit, uow, clock);
    }

    private static Categoria SeedCategoria(InMemoryCategoriaRepository repo, string nome = "Bebidas")
    {
        var c = new Categoria { Id = Guid.NewGuid(), Nome = nome, Ativo = true };
        repo.Categorias.Add(c);
        return c;
    }

    // ---------- Categorias ----------

    [Fact]
    public async Task CreateCategoria_NomeNovo_AdicionaERegistaAuditoria()
    {
        var (svc, cats, _, _, _, audit, uow, _) = Build();

        var resp = await svc.CreateCategoriaAsync(new CreateCategoriaRequest
        {
            Nome = "Bebidas",
            ParentCategoriaId = null
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        cats.Categorias.Should().HaveCount(1);
        audit.Contains(AuditAcoes.CategoriaCriada).Should().BeTrue();
        uow.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateCategoria_NomeDuplicado_DevolveConflict()
    {
        var (svc, cats, _, _, _, _, _, _) = Build();
        SeedCategoria(cats, "Bebidas");

        var resp = await svc.CreateCategoriaAsync(new CreateCategoriaRequest
        {
            Nome = "Bebidas"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CatalogoErrorCodes.CategoriaNomeDuplicado);
    }

    [Fact]
    public async Task CreateCategoria_ParentInexistente_DevolveValidation()
    {
        var (svc, _, _, _, _, _, _, _) = Build();

        var resp = await svc.CreateCategoriaAsync(new CreateCategoriaRequest
        {
            Nome = "Subcategoria",
            ParentCategoriaId = Guid.NewGuid()
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CatalogoErrorCodes.CategoriaParentInexistente);
    }

    // ---------- Produtos ----------

    [Fact]
    public async Task CreateProduto_HappyPath_GeraHistoricoPrecoEEntries()
    {
        var (svc, cats, produtos, lojas, stocks, audit, _, _) = Build();
        var cat = SeedCategoria(cats);
        lojas.Lojas.Add(new Loja { Id = Guid.NewGuid(), Designacao = "Braga", StockMinPorDefeito = 5, StockMaxPorDefeito = 50 });

        var resp = await svc.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "Sumo de laranja 1L",
            CategoriaId = cat.Id,
            Preco = 2.50m,
            TaxaIva = TaxaIva.Reduzida,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        produtos.Produtos.Should().HaveCount(1);
        produtos.Historico.Should().HaveCount(1);
        stocks.Stocks.Should().HaveCount(1);
        stocks.Stocks[0].Quantidade.Should().Be(0);
        stocks.Stocks[0].QtdMinima.Should().Be(5);
        stocks.Stocks[0].QtdMaxima.Should().Be(50);
        audit.Contains(AuditAcoes.ProdutoCriado).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduto_Ean13Invalido_Rejeita()
    {
        var (svc, cats, _, _, _, _, _, _) = Build();
        var cat = SeedCategoria(cats);

        var resp = await svc.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "1234567890123",  // check digit incorreto
            Descricao = "Inválido",
            CategoriaId = cat.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CatalogoErrorCodes.Ean13Invalido);
    }

    [Fact]
    public async Task CreateProduto_CategoriaInexistente_Rejeita()
    {
        var (svc, _, _, _, _, _, _, _) = Build();

        var resp = await svc.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "X",
            CategoriaId = Guid.NewGuid(),
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CatalogoErrorCodes.CategoriaNaoEncontrada);
    }

    [Fact]
    public async Task CreateProduto_Ean13Duplicado_DevolveConflict()
    {
        var (svc, cats, produtos, _, _, _, _, _) = Build();
        var cat = SeedCategoria(cats);
        produtos.Produtos.Add(new Produto { Id = Guid.NewGuid(), Ean13 = "5601234567892", Ativo = true });

        var resp = await svc.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "X",
            CategoriaId = cat.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CatalogoErrorCodes.Ean13Duplicado);
    }

    [Fact]
    public async Task UpdateProduto_MudancaDePreco_AdicionaHistoricoEAuditoria()
    {
        var (svc, cats, produtos, _, _, audit, _, _) = Build();
        var cat = SeedCategoria(cats);
        var prod = new Produto
        {
            Id = Guid.NewGuid(),
            Ean13 = "5601234567892",
            Descricao = "Sumo",
            CategoriaId = cat.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un",
            Ativo = true
        };
        produtos.Produtos.Add(prod);

        var resp = await svc.UpdateProdutoAsync(prod.Id, new UpdateProdutoRequest
        {
            Descricao = "Sumo",
            CategoriaId = cat.Id,
            Preco = 2m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un",
            Ativo = true
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        produtos.Historico.Should().HaveCount(1);
        produtos.Historico[0].Preco.Should().Be(2m);
        audit.Contains(AuditAcoes.ProdutoPrecoAlterado).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProduto_DesativarAtivoFalse_RegistaAuditoriaDesativado()
    {
        var (svc, cats, produtos, _, _, audit, _, _) = Build();
        var cat = SeedCategoria(cats);
        var prod = new Produto
        {
            Id = Guid.NewGuid(),
            Ean13 = "5601234567892",
            Descricao = "Sumo",
            CategoriaId = cat.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un",
            Ativo = true
        };
        produtos.Produtos.Add(prod);

        await svc.UpdateProdutoAsync(prod.Id, new UpdateProdutoRequest
        {
            Descricao = "Sumo",
            CategoriaId = cat.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un",
            Ativo = false
        }, Guid.NewGuid(), "127.0.0.1");

        audit.Contains(AuditAcoes.ProdutoDesativado).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateProduto_DesativaERegista()
    {
        var (svc, _, produtos, _, _, audit, _, _) = Build();
        var prod = new Produto { Id = Guid.NewGuid(), Ativo = true };
        produtos.Produtos.Add(prod);

        var resp = await svc.DeactivateProdutoAsync(prod.Id, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        prod.Ativo.Should().BeFalse();
        audit.Contains(AuditAcoes.ProdutoDesativado).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateProduto_NaoExistente_DevolveNotFound()
    {
        var (svc, _, _, _, _, _, _, _) = Build();
        var resp = await svc.DeactivateProdutoAsync(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1");
        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CatalogoErrorCodes.ProdutoNaoEncontrado);
    }
}
