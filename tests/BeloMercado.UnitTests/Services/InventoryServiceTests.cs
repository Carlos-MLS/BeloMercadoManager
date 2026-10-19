using BeloMercado.Application.Dtos.Stock;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.UnitTests.TestDoubles;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// InventoryService — UC06 (Gestão de Stock). Cobre:
///   ListAlertas: items com Quantidade &lt; QtdMinima.
///   UpdateLimits: rejeita negativos e Max &lt; Min.
///   Inicialização de Stock para nova Loja / novo Produto.
/// </summary>
public class InventoryServiceTests
{
    private static (InventoryService svc,
        InMemoryStockRepository stocks,
        InMemoryLojaRepository lojas,
        InMemoryProdutoRepository produtos,
        FakeUnitOfWork uow) Build()
    {
        var stocks = new InMemoryStockRepository();
        var lojas = new InMemoryLojaRepository();
        var produtos = new InMemoryProdutoRepository();
        var uow = new FakeUnitOfWork();
        var svc = new InventoryService(stocks, lojas, produtos, uow);
        return (svc, stocks, lojas, produtos, uow);
    }

    [Fact]
    public async Task ListAlertas_DevolveApenasStocksAbaixoDoMinimo()
    {
        var (svc, stocks, _, _, _) = Build();
        var lojaId = Guid.NewGuid();
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), LojaId = lojaId, Quantidade = 2, QtdMinima = 5, QtdMaxima = 50 });
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), LojaId = lojaId, Quantidade = 10, QtdMinima = 5, QtdMaxima = 50 });

        var alertas = await svc.ListAlertasAsync(lojaId);

        alertas.Should().HaveCount(1);
        alertas[0].AbaixoMinimo.Should().BeTrue();
    }

    [Fact]
    public async Task ListAlertas_SemLojaScope_DevolveDeTodasLojas()
    {
        var (svc, stocks, _, _, _) = Build();
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), LojaId = Guid.NewGuid(), Quantidade = 0, QtdMinima = 1, QtdMaxima = 10 });
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), LojaId = Guid.NewGuid(), Quantidade = 100, QtdMinima = 1, QtdMaxima = 200 });

        var alertas = await svc.ListAlertasAsync(scopeLojaId: null);

        alertas.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateLimits_LimitesNegativos_Rejeita()
    {
        var (svc, stocks, _, _, _) = Build();
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), Quantidade = 0, QtdMinima = 0, QtdMaxima = 0 });

        var resp = await svc.UpdateLimitsAsync(stocks.Stocks[0].Id,
            new UpdateStockLimitsRequest { QtdMinima = -1, QtdMaxima = 10 });

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("STOCK_LIMITES_INVALIDOS");
    }

    [Fact]
    public async Task UpdateLimits_MaxMenorQueMin_Rejeita()
    {
        var (svc, stocks, _, _, _) = Build();
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), Quantidade = 0, QtdMinima = 0, QtdMaxima = 0 });

        var resp = await svc.UpdateLimitsAsync(stocks.Stocks[0].Id,
            new UpdateStockLimitsRequest { QtdMinima = 50, QtdMaxima = 10 });

        resp.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLimits_HappyPath_AtualizaELoga()
    {
        var (svc, stocks, _, _, uow) = Build();
        var s = new Stock { Id = Guid.NewGuid(), Quantidade = 0, QtdMinima = 0, QtdMaxima = 0 };
        stocks.Stocks.Add(s);

        var resp = await svc.UpdateLimitsAsync(s.Id,
            new UpdateStockLimitsRequest { QtdMinima = 10, QtdMaxima = 100 });

        resp.IsSuccess.Should().BeTrue();
        s.QtdMinima.Should().Be(10);
        s.QtdMaxima.Should().Be(100);
        uow.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateLimits_StockInexistente_DevolveNotFound()
    {
        var (svc, _, _, _, _) = Build();

        var resp = await svc.UpdateLimitsAsync(Guid.NewGuid(),
            new UpdateStockLimitsRequest { QtdMinima = 0, QtdMaxima = 10 });

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("STOCK_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task InitializeForNewLoja_SemProdutos_NaoFazNada()
    {
        var (svc, stocks, _, _, _) = Build();

        await svc.InitializeStockForNewLojaAsync(new Loja { Id = Guid.NewGuid() });

        stocks.Stocks.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeForNewProduto_ComLojas_CriaUmStockPorLoja()
    {
        var (svc, stocks, lojas, _, _) = Build();
        lojas.Lojas.Add(new Loja { Id = Guid.NewGuid(), StockMinPorDefeito = 5, StockMaxPorDefeito = 50 });
        lojas.Lojas.Add(new Loja { Id = Guid.NewGuid(), StockMinPorDefeito = 3, StockMaxPorDefeito = 30 });

        await svc.InitializeStockForNewProdutoAsync(Guid.NewGuid());

        stocks.Stocks.Should().HaveCount(2);
        stocks.Stocks.Should().AllSatisfy(s => s.Quantidade.Should().Be(0));
    }
}
