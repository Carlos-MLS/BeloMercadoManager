using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;

namespace BeloMercado.AcceptanceTests.Acceptance;

/// <summary>
/// UC05 — Gerir Catálogo.
/// Critérios: criar produto exige EAN-13 válido e único, e categoria
/// existente; alteração de preço materializa HistoricoPreco; criação de
/// produto materializa Stock em todas as lojas.
/// </summary>
public class UC05_GerirCatalogoTests
{
    [Fact]
    public async Task UC05_Principal_CriarProdutoValido_GeraHistoricoEStock()
    {
        using var f = new IntegrationTestFixture();
        await f.CreateStoreService().CreateAsync(new CreateLojaRequest
        {
            Designacao = "L",
            Morada = "R",
            CodigoPostal = "0000-000",
            Contacto = "9",
            Email = "l@belomercado.pt"
        }, Guid.NewGuid(), "127.0.0.1");
        var catalog = f.CreateCatalogService();
        var cat = await catalog.CreateCategoriaAsync(
            new CreateCategoriaRequest { Nome = "X" }, Guid.NewGuid(), "127.0.0.1");

        var resp = await catalog.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "Produto",
            CategoriaId = cat.Value!.Id,
            Preco = 1.99m,
            TaxaIva = TaxaIva.Reduzida,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        f.Db.Produtos.Should().HaveCount(1);
        f.Db.HistoricoPrecos.Should().HaveCount(1);
        f.Db.Stocks.Count(s => s.ProdutoId == resp.Value!.Id).Should().Be(1);
    }

    [Fact]
    public async Task UC05_Alt1_Ean13Invalido_Rejeita()
    {
        using var f = new IntegrationTestFixture();
        var catalog = f.CreateCatalogService();
        var cat = await catalog.CreateCategoriaAsync(
            new CreateCategoriaRequest { Nome = "X" }, Guid.NewGuid(), "127.0.0.1");

        var resp = await catalog.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "1111111111111",  // check incorreto
            Descricao = "X",
            CategoriaId = cat.Value!.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("PRODUTO_EAN13_INVALIDO");
    }

    [Fact]
    public async Task UC05_Alt2_AlterarPreco_AdicionaEntradaAoHistorico()
    {
        using var f = new IntegrationTestFixture();
        var catalog = f.CreateCatalogService();
        var cat = await catalog.CreateCategoriaAsync(
            new CreateCategoriaRequest { Nome = "X" }, Guid.NewGuid(), "127.0.0.1");
        var p = await catalog.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "P",
            CategoriaId = cat.Value!.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        await catalog.UpdateProdutoAsync(p.Value!.Id, new UpdateProdutoRequest
        {
            Descricao = "P",
            CategoriaId = cat.Value.Id,
            Preco = 1.50m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un",
            Ativo = true
        }, Guid.NewGuid(), "127.0.0.1");

        f.Db.HistoricoPrecos.Count(h => h.ProdutoId == p.Value.Id).Should().Be(2);
    }
}
