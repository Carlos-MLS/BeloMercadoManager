using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Application.Dtos.Vendas;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;

namespace BeloMercado.IntegrationTests;

/// <summary>
/// Integração de UC04 + UC01 + UC12: abre caixa, cria duas vendas reais,
/// fecha caixa (dispara consolidação imediata) e verifica KPIs persistidos.
/// Toca o fluxo end-to-end (Service → Repository → DbContext → InMemory provider).
/// </summary>
public class SalesIntegrationTests
{
    [Fact]
    public async Task FluxoCompleto_Caixa_VendasMultiplas_FechoEConsolidacao()
    {
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedScenarioAsync(f);
        var sales = f.CreateSalesService();

        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 100m },
            operadorId, lojaId, "127.0.0.1");

        var v1 = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 50m,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 2 } }
        }, "k1", operadorId, lojaId, "127.0.0.1");
        v1.IsSuccess.Should().BeTrue();

        var v2 = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k2", operadorId, lojaId, "127.0.0.1");
        v2.IsSuccess.Should().BeTrue();

        // Sanity: stock decreased to 100 - 3 = 97
        f.Db.Stocks.Single(s => s.LojaId == lojaId).Quantidade.Should().Be(97);

        // Fechar caixa (sem diferença material)
        var caixa = f.Db.CaixasDiarias.Single();
        var fechar = await sales.FecharCaixaAsync(caixa.Id,
            new FecharCaixaRequest { ValorContado = 100m, Justificacao = null },
            operadorId, lojaId, "127.0.0.1");
        fechar.IsSuccess.Should().BeTrue();

        // Consolidação deve ter sido executada (RN03 UC04)
        var consol = f.Db.ConsolidacoesDiarias.Single();
        consol.Estado.Should().Be(EstadoConsolidacao.Concluida);

        var kpi = f.Db.KPIsDiarios.Single(k => k.LojaId == lojaId && k.Data == f.Clock.Today);
        kpi.NumTransacoes.Should().Be(2);
        kpi.TotalVendas.Should().Be(30m);  // 2 * 10 + 1 * 10
    }

    [Fact]
    public async Task Venda_FaturaSequencial_PorLojaPorAno()
    {
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedScenarioAsync(f);
        var sales = f.CreateSalesService();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 0m }, operadorId, lojaId, "127.0.0.1");

        for (var i = 0; i < 3; i++)
        {
            await sales.CriarVendaAsync(new CriarVendaRequest
            {
                MetodoPagamento = MetodoPagamento.Cartao,
                Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
            }, $"k-{i}", operadorId, lojaId, "127.0.0.1");
        }

        var facturas = f.Db.Faturas.OrderBy(x => x.NumeroSequencial).ToList();
        facturas.Should().HaveCount(3);
        facturas.Select(x => x.NumeroSequencial).Should().Equal(1, 2, 3);
        facturas.Should().AllSatisfy(x => x.LojaId.Should().Be(lojaId));
    }

    private static async Task<(Guid operadorId, Guid lojaId, Guid produtoId)> SeedScenarioAsync(IntegrationTestFixture f)
    {
        var stores = f.CreateStoreService();
        var catalog = f.CreateCatalogService();
        var operadorId = Guid.NewGuid();
        var loja = await stores.CreateAsync(new CreateLojaRequest
        {
            Designacao = "Braga",
            Morada = "R",
            CodigoPostal = "4700-000",
            Contacto = "9",
            Email = "braga@belomercado.pt",
            StockMinPorDefeito = 0,
            StockMaxPorDefeito = 1000
        }, Guid.NewGuid(), "127.0.0.1");
        var cat = await catalog.CreateCategoriaAsync(
            new CreateCategoriaRequest { Nome = "Cat" }, Guid.NewGuid(), "127.0.0.1");
        var prod = await catalog.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "P",
            CategoriaId = cat.Value!.Id,
            Preco = 10m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        // Materialize stock with 100 quantity
        var stock = f.Db.Stocks.Single(s => s.LojaId == loja.Value!.Id && s.ProdutoId == prod.Value!.Id);
        stock.Quantidade = 100;
        await f.Db.SaveChangesAsync();

        return (operadorId, loja.Value.Id, prod.Value.Id);
    }
}
