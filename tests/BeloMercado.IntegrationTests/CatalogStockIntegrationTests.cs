using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;

namespace BeloMercado.IntegrationTests;

/// <summary>
/// Testes de integração: usa o DbContext EF Core real (InMemory) +
/// repositórios reais. Valida que a interação Service↔Repository↔DbContext
/// está coerente e que mudanças sobrevivem entre SaveChanges.
/// </summary>
public class CatalogStockIntegrationTests
{
    [Fact]
    public async Task CriarLojaECriarProdutoMaterializaStockEmCadaLoja()
    {
        // Cenário cruzado UC05 + UC06 + UC19: criar 2 lojas, depois 2 produtos.
        // Cada novo produto materializa 2 linhas de stock (uma por loja).
        using var f = new IntegrationTestFixture();
        var stores = f.CreateStoreService();
        var catalog = f.CreateCatalogService();

        var loja1 = await stores.CreateAsync(new CreateLojaRequest
        {
            Designacao = "Braga",
            Morada = "R1",
            CodigoPostal = "4700-000",
            Contacto = "910000000",
            Email = "braga@belomercado.pt",
            StockMinPorDefeito = 5,
            StockMaxPorDefeito = 50
        }, Guid.NewGuid(), "127.0.0.1");
        var loja2 = await stores.CreateAsync(new CreateLojaRequest
        {
            Designacao = "Guimaraes",
            Morada = "R2",
            CodigoPostal = "4800-000",
            Contacto = "910000001",
            Email = "guimaraes@belomercado.pt",
            StockMinPorDefeito = 10,
            StockMaxPorDefeito = 100
        }, Guid.NewGuid(), "127.0.0.1");

        var cat = await catalog.CreateCategoriaAsync(
            new CreateCategoriaRequest { Nome = "Bebidas" }, Guid.NewGuid(), "127.0.0.1");

        var prod1 = await catalog.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "Sumo",
            CategoriaId = cat.Value!.Id,
            Preco = 2m,
            TaxaIva = TaxaIva.Reduzida,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");
        prod1.IsSuccess.Should().BeTrue();

        // Verificação direta via DbContext — confirma persistência
        f.Db.Stocks.Count(s => s.ProdutoId == prod1.Value!.Id).Should().Be(2);
        // Limites herdados da loja
        f.Db.Stocks.Single(s => s.ProdutoId == prod1.Value.Id && s.LojaId == loja1.Value!.Id)
            .QtdMinima.Should().Be(5);
        f.Db.Stocks.Single(s => s.ProdutoId == prod1.Value.Id && s.LojaId == loja2.Value!.Id)
            .QtdMinima.Should().Be(10);
    }

    [Fact]
    public async Task UpdateProduto_AlteraPrecoEMaterializaHistoricoPreco()
    {
        using var f = new IntegrationTestFixture();
        var catalog = f.CreateCatalogService();
        var cat = await catalog.CreateCategoriaAsync(
            new CreateCategoriaRequest { Nome = "X" }, Guid.NewGuid(), "127.0.0.1");
        var prod = await catalog.CreateProdutoAsync(new CreateProdutoRequest
        {
            Ean13 = "5601234567892",
            Descricao = "P",
            CategoriaId = cat.Value!.Id,
            Preco = 1m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un"
        }, Guid.NewGuid(), "127.0.0.1");

        await catalog.UpdateProdutoAsync(prod.Value!.Id, new UpdateProdutoRequest
        {
            Descricao = "P",
            CategoriaId = cat.Value.Id,
            Preco = 2.50m,
            TaxaIva = TaxaIva.Normal,
            UnidadeMedida = "un",
            Ativo = true
        }, Guid.NewGuid(), "127.0.0.1");

        // 2 entries: criação inicial + alteração (clock não avança, DataMudanca
        // pode coincidir — verificamos pela existência do novo preço).
        f.Db.HistoricoPrecos.Count(h => h.ProdutoId == prod.Value.Id).Should().Be(2);
        f.Db.HistoricoPrecos.Where(h => h.ProdutoId == prod.Value.Id)
            .Select(h => h.Preco).Should().Contain(2.50m);
    }
}
