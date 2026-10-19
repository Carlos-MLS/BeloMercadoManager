using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Application.Dtos.Vendas;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;

namespace BeloMercado.AcceptanceTests.Acceptance;

/// <summary>
/// Testes de aceitação para UC01 — Registar Venda.
/// Cada teste segue o padrão Given/When/Then mapeado ao caso de uso descrito
/// no relatório (Cap. 3). O critério de aceitação é exatamente o que o
/// utilizador (operador de caixa) esperaria ver no POS depois da operação.
/// </summary>
public class UC01_RegistarVendaTests
{
    /// <summary>
    /// Cenário principal (happy path).
    /// Given: existe uma loja com produto em stock e uma caixa aberta.
    /// When:  o operador finaliza uma venda em numerário com troco.
    /// Then:  a venda fica Concluída; é emitida uma fatura; o stock é
    ///        decrementado; é registado um log de auditoria VENDA_FINALIZADA.
    /// </summary>
    [Fact]
    public async Task UC01_Principal_VendaEmNumerarioComTroco_FinalizaEDecrementaStock()
    {
        // Given
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedAsync(f, precoUnit: 2m, qtdStock: 100);
        var sales = f.CreateSalesService();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 50m }, operadorId, lojaId, "127.0.0.1");

        // When — 3 unidades de 2€ = 6€; o cliente entrega 10€
        var resp = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 10m,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 3 } }
        }, "vendinha", operadorId, lojaId, "127.0.0.1");

        // Then
        resp.IsSuccess.Should().BeTrue();
        resp.Value!.Estado.Should().Be(nameof(EstadoVenda.Concluida));
        resp.Value.Total.Should().Be(6m);
        resp.Value.Troco.Should().Be(4m);
        resp.Value.Fatura.NumeroCompleto.Should().StartWith("FT ").And.Contain("/00001-");
        f.Db.Stocks.Single().Quantidade.Should().Be(97);
        f.Audit.Acoes.Should().Contain("VENDA_FINALIZADA");
    }

    /// <summary>
    /// Cenário alternativo 1: tentativa de venda sem caixa aberta.
    /// Critério: rejeita com código CAIXA_NAO_ABERTA e não cria venda.
    /// </summary>
    [Fact]
    public async Task UC01_Alt1_SemCaixaAberta_RejeitaSemCriarVenda()
    {
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedAsync(f);
        var sales = f.CreateSalesService();
        // intencionalmente: NÃO abrimos caixa

        var resp = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 100m,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("CAIXA_NAO_ABERTA");
        f.Db.Vendas.Should().BeEmpty();
    }

    /// <summary>
    /// Cenário alternativo 2: stock insuficiente.
    /// </summary>
    [Fact]
    public async Task UC01_Alt2_StockInsuficiente_RejeitaSemDecrementar()
    {
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedAsync(f, qtdStock: 2);
        var sales = f.CreateSalesService();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 0m }, operadorId, lojaId, "127.0.0.1");

        var resp = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 5 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("STOCK_INSUFICIENTE");
        f.Db.Stocks.Single().Quantidade.Should().Be(2);
        f.Db.Vendas.Should().BeEmpty();
    }

    /// <summary>
    /// Cenário alternativo 3: idempotency-key duplicada (e.g. retry do cliente).
    /// Critério: devolve a mesma venda; não duplica linhas; não decrementa stock 2x.
    /// </summary>
    [Fact]
    public async Task UC01_Alt3_RetryComMesmaIdempotencyKey_NaoDuplica()
    {
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedAsync(f, qtdStock: 10);
        var sales = f.CreateSalesService();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 0m }, operadorId, lojaId, "127.0.0.1");
        var req = new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        };

        var r1 = await sales.CriarVendaAsync(req, "same", operadorId, lojaId, "127.0.0.1");
        var r2 = await sales.CriarVendaAsync(req, "same", operadorId, lojaId, "127.0.0.1");

        r1.Value!.Id.Should().Be(r2.Value!.Id);
        f.Db.Vendas.Should().HaveCount(1);
        f.Db.Stocks.Single().Quantidade.Should().Be(9);
    }

    private static async Task<(Guid operadorId, Guid lojaId, Guid produtoId)> SeedAsync(
        IntegrationTestFixture f, decimal precoUnit = 10m, int qtdStock = 100)
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
            Descricao = "Produto",
            CategoriaId = cat.Value!.Id,
            Preco = precoUnit,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");
        var stock = f.Db.Stocks.Single();
        stock.Quantidade = qtdStock;
        await f.Db.SaveChangesAsync();
        return (operadorId, loja.Value!.Id, prod.Value!.Id);
    }
}
