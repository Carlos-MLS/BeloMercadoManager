using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.UnitTests.TestDoubles;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// StoreManagementService — UC19 (Gestão de Lojas). Regras:
///   RN19a Designação única (case-insensitive).
///   RN19b StockMax >= StockMin.
///   RN19c Criar loja materializa Stock para todos os produtos.
///   RN19d Transição ativo→inativo regista LojaDesativada distinto.
/// </summary>
public class StoreManagementServiceTests
{
    private static (StoreManagementService svc,
        InMemoryLojaRepository lojas,
        InMemoryProdutoRepository produtos,
        InMemoryStockRepository stocks,
        FakeAuditService audit,
        FakeUnitOfWork uow) Build()
    {
        var lojas = new InMemoryLojaRepository();
        var produtos = new InMemoryProdutoRepository();
        var stocks = new InMemoryStockRepository();
        var audit = new FakeAuditService();
        var uow = new FakeUnitOfWork();
        var clock = new FakeClock();
        var inventory = new InventoryService(stocks, lojas, produtos, uow);
        var svc = new StoreManagementService(lojas, audit, uow, inventory, clock);
        return (svc, lojas, produtos, stocks, audit, uow);
    }

    [Fact]
    public async Task Create_HappyPath_AdicionaLojaEMaterializaStockParaProdutosExistentes()
    {
        var (svc, lojas, produtos, stocks, audit, uow) = Build();
        produtos.Produtos.Add(new Produto { Id = Guid.NewGuid(), Ean13 = "5601234567892", Ativo = true });
        produtos.Produtos.Add(new Produto { Id = Guid.NewGuid(), Ean13 = "4006381333931", Ativo = true });

        var resp = await svc.CreateAsync(new CreateLojaRequest
        {
            Designacao = "Braga",
            Morada = "R. X",
            CodigoPostal = "4700-000",
            Contacto = "910000000",
            Email = "braga@belomercado.pt",
            StockMinPorDefeito = 5,
            StockMaxPorDefeito = 50
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        lojas.Lojas.Should().HaveCount(1);
        stocks.Stocks.Should().HaveCount(2);   // materializado para os 2 produtos existentes
        audit.Contains(AuditAcoes.LojaCriada).Should().BeTrue();
        uow.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_StockMaxMenorQueMin_Rejeita()
    {
        var (svc, _, _, _, _, _) = Build();

        var resp = await svc.CreateAsync(new CreateLojaRequest
        {
            Designacao = "Y",
            Morada = "R",
            CodigoPostal = "4700-000",
            Contacto = "9",
            Email = "y@belomercado.pt",
            StockMinPorDefeito = 50,
            StockMaxPorDefeito = 10
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(LojasErrorCodes.StockBoundsInvalidos);
    }

    [Fact]
    public async Task Create_DesignacaoDuplicada_DevolveConflict()
    {
        var (svc, lojas, _, _, _, _) = Build();
        lojas.Lojas.Add(new Loja { Id = Guid.NewGuid(), Designacao = "Braga" });

        var resp = await svc.CreateAsync(new CreateLojaRequest
        {
            Designacao = "Braga",
            Morada = "R",
            CodigoPostal = "4700-000",
            Contacto = "9",
            Email = "braga2@belomercado.pt",
            StockMinPorDefeito = 0,
            StockMaxPorDefeito = 10
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(LojasErrorCodes.DesignacaoDuplicada);
    }

    [Fact]
    public async Task Update_DesativarLoja_RegistaLojaDesativada()
    {
        var (svc, lojas, _, _, audit, _) = Build();
        var loja = new Loja
        {
            Id = Guid.NewGuid(),
            Designacao = "Braga",
            Morada = "R",
            CodigoPostal = "4700-000",
            Contacto = "9",
            Email = "braga@belomercado.pt",
            StockMinPorDefeito = 0,
            StockMaxPorDefeito = 10,
            Ativo = true
        };
        lojas.Lojas.Add(loja);

        await svc.UpdateAsync(loja.Id, new UpdateLojaRequest
        {
            Designacao = "Braga",
            Morada = "R",
            CodigoPostal = "4700-000",
            Contacto = "9",
            Email = "braga@belomercado.pt",
            StockMinPorDefeito = 0,
            StockMaxPorDefeito = 10,
            Ativo = false
        }, Guid.NewGuid(), "127.0.0.1");

        audit.Contains(AuditAcoes.LojaDesativada).Should().BeTrue();
    }

    [Fact]
    public async Task Update_NaoEncontrada_DevolveNotFound()
    {
        var (svc, _, _, _, _, _) = Build();

        var resp = await svc.UpdateAsync(Guid.NewGuid(), new UpdateLojaRequest
        {
            Designacao = "X",
            Morada = "R",
            CodigoPostal = "0000-000",
            Contacto = "0",
            Email = "x@x.pt",
            StockMinPorDefeito = 0,
            StockMaxPorDefeito = 0
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(LojasErrorCodes.NaoEncontrada);
    }
}
