using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Application.Dtos.Vendas;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;

namespace BeloMercado.AcceptanceTests.Acceptance;

/// <summary>
/// UC02 — Anular ou Devolver Venda.
/// Critérios: anulação repõe o stock e marca a venda Anulada; devolução
/// parcial actualiza QuantidadeDevolvida e incrementa stock; >24h
/// requer perfil Gerente para anular.
/// </summary>
public class UC02_AnularEDevolverTests
{
    [Fact]
    public async Task UC02_Principal_AnularRecente_ColocaEstadoAnuladaEReverteStock()
    {
        // Given
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedAsync(f);
        var sales = f.CreateSalesService();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 0m }, operadorId, lojaId, "127.0.0.1");
        var venda = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 2 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        // When
        var resp = await sales.AnularVendaAsync(venda.Value!.Id,
            new AnularVendaRequest { Motivo = "Cliente desistiu" },
            operadorId, lojaId, isGerente: false, isAdmin: false, "127.0.0.1");

        // Then
        resp.IsSuccess.Should().BeTrue();
        f.Db.Vendas.Single().Estado.Should().Be(EstadoVenda.Anulada);
        f.Db.Stocks.Single().Quantidade.Should().Be(100);  // back to original
        f.Audit.Acoes.Should().Contain("VENDA_ANULADA");
    }

    [Fact]
    public async Task UC02_Alt1_Anular24hPorOperador_RejeitaComApprovalRequired()
    {
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedAsync(f);
        var sales = f.CreateSalesService();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 0m }, operadorId, lojaId, "127.0.0.1");
        var v = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");
        f.Clock.UtcNow = f.Clock.UtcNow.AddHours(25);

        var resp = await sales.AnularVendaAsync(v.Value!.Id,
            new AnularVendaRequest { Motivo = "tardia" },
            operadorId, lojaId, isGerente: false, isAdmin: false, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("APPROVAL_REQUIRED");
    }

    [Fact]
    public async Task UC02_DevolucaoParcial_AtualizaQuantidadeDevolvidaEStock()
    {
        using var f = new IntegrationTestFixture();
        var (operadorId, lojaId, produtoId) = await SeedAsync(f);
        var sales = f.CreateSalesService();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 0m }, operadorId, lojaId, "127.0.0.1");
        var v = await sales.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 3 } }
        }, "k", operadorId, lojaId, "127.0.0.1");
        var linhaId = f.Db.LinhasVenda.Single().Id;

        var resp = await sales.DevolucaoAsync(v.Value!.Id,
            new DevolucaoRequest
            {
                Motivo = "Embalagem partida",
                Linhas = new List<LinhaDevolucaoRequest>
                {
                    new() { LinhaVendaId = linhaId, Quantidade = 1 }
                }
            }, operadorId, lojaId, isAdmin: false, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        f.Db.LinhasVenda.Single().QuantidadeDevolvida.Should().Be(1);
        f.Db.Stocks.Single().Quantidade.Should().Be(98); // 100 - 3 + 1
    }

    private static async Task<(Guid operadorId, Guid lojaId, Guid produtoId)> SeedAsync(IntegrationTestFixture f)
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
            new CreateCategoriaRequest { Nome = "X" }, Guid.NewGuid(), "127.0.0.1");
        var prod = await catalog.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "P",
            CategoriaId = cat.Value!.Id,
            Preco = 10m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");
        var stock = f.Db.Stocks.Single();
        stock.Quantidade = 100;
        await f.Db.SaveChangesAsync();
        return (operadorId, loja.Value!.Id, prod.Value!.Id);
    }
}
