using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Encomendas;
using BeloMercado.Application.Dtos.Fornecedores;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.UnitTests.TestDoubles;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// ProcurementService — UC09 (Fornecedores), UC10 (Encomendas), UC11 (Receção).
/// Regras:
///   RN09a NIF validado.
///   RN09b NIF único.
///   RN10a Linha exige produto associado ao fornecedor.
///   RN10b Encomenda &gt; 5000 € requer aprovação Admin.
///   RN10c Transições válidas: Rascunho → Submetida → Recebida/ParcialmenteRecebida.
///   RN11a Receção total → estado Recebida; parcial → ParcialmenteRecebida.
///   RN11b Receção incrementa stock pela quantidade recebida.
/// </summary>
public class ProcurementServiceTests
{
    private static (ProcurementService svc,
        InMemoryFornecedorRepository fornecedores,
        InMemoryProdutoRepository produtos,
        InMemoryEncomendaRepository encomendas,
        InMemoryStockRepository stocks,
        FakeAuditService audit,
        FakeUnitOfWork uow,
        FakeClock clock) Build()
    {
        var fornecedores = new InMemoryFornecedorRepository();
        var produtos = new InMemoryProdutoRepository();
        var encomendas = new InMemoryEncomendaRepository();
        var stocks = new InMemoryStockRepository();
        var audit = new FakeAuditService();
        var uow = new FakeUnitOfWork();
        var clock = new FakeClock();
        var svc = new ProcurementService(fornecedores, produtos, encomendas, stocks, audit, uow, clock);
        return (svc, fornecedores, produtos, encomendas, stocks, audit, uow, clock);
    }

    [Fact]
    public async Task CreateFornecedor_NifInvalido_Rejeita()
    {
        var (svc, _, _, _, _, _, _, _) = Build();

        var resp = await svc.CreateAsync(new CreateFornecedorRequest
        {
            Nome = "Fornec X",
            Nif = "111111111",  // check digit incorreto
            Morada = "R",
            Contacto = "9",
            Email = "x@f.pt"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(FornecedoresErrorCodes.NifInvalido);
    }

    [Fact]
    public async Task CreateFornecedor_NifDuplicado_Rejeita()
    {
        var (svc, fornecedores, _, _, _, _, _, _) = Build();
        fornecedores.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nif = "123456789" });

        var resp = await svc.CreateAsync(new CreateFornecedorRequest
        {
            Nome = "Outro",
            Nif = "123456789",
            Morada = "R",
            Contacto = "9",
            Email = "outro@f.pt"
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(FornecedoresErrorCodes.NifDuplicado);
    }

    [Fact]
    public async Task CreateFornecedor_HappyPath_CriaEAuditoria()
    {
        var (svc, fornecedores, produtos, _, _, audit, _, _) = Build();
        var prodId = Guid.NewGuid();
        produtos.Produtos.Add(new Produto { Id = prodId });

        var resp = await svc.CreateAsync(new CreateFornecedorRequest
        {
            Nome = "Fornec",
            Nif = "123456789",
            Morada = "R",
            Contacto = "9",
            Email = "f@f.pt",
            ProdutosAssociados = new List<ProdutoFornecedorRequest>
            {
                new() { ProdutoId = prodId, PrecoCusto = 5m, DescontoPercentagem = 10m, PrazoEntregaDias = 7 }
            }
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        fornecedores.Fornecedores.Should().HaveCount(1);
        fornecedores.Fornecedores[0].ProdutosAssociados.Should().HaveCount(1);
        audit.Contains(AuditAcoes.FornecedorCriado).Should().BeTrue();
    }

    [Fact]
    public async Task CriarEncomenda_SuperiorA5000_Submeter_RequerAdmin()
    {
        // RN10b
        var (svc, fornecedores, produtos, _, _, _, _, _) = Build();
        var (lojaId, fornecedor, produto) = SeedEncomendaPrereqs(fornecedores, produtos,
            precoCusto: 100m, desconto: 0m);
        // 100 * 60 = 6000€  → acima de 5000€
        var encomendaResp = await svc.CriarEncomendaAsync(new CriarEncomendaRequest
        {
            FornecedorId = fornecedor.Id,
            LojaId = lojaId,
            Linhas = new List<LinhaEncomendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 60 }
            }
        }, Guid.NewGuid(), lojaId, "127.0.0.1");
        encomendaResp.IsSuccess.Should().BeTrue();

        var subm = await svc.SubmeterEncomendaAsync(
            Guid.Parse(encomendaResp.Value!.Id.ToString()),
            Guid.NewGuid(), lojaId, isAdmin: false, "127.0.0.1");

        subm.IsSuccess.Should().BeFalse();
        subm.ErrorCode.Should().Be(EncomendasErrorCodes.ApprovalRequired);
    }

    [Fact]
    public async Task CriarEncomenda_SuperiorA5000_SubmeterPorAdmin_Aceita()
    {
        var (svc, fornecedores, produtos, _, _, _, _, _) = Build();
        var (lojaId, fornecedor, produto) = SeedEncomendaPrereqs(fornecedores, produtos);
        var encomendaResp = await svc.CriarEncomendaAsync(new CriarEncomendaRequest
        {
            FornecedorId = fornecedor.Id,
            LojaId = lojaId,
            Linhas = new List<LinhaEncomendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 60 }
            }
        }, Guid.NewGuid(), lojaId, "127.0.0.1");

        var subm = await svc.SubmeterEncomendaAsync(
            encomendaResp.Value!.Id, Guid.NewGuid(), null, isAdmin: true, "127.0.0.1");

        subm.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecepcionarEncomenda_QuantidadeRecebidaSuperior_Rejeita()
    {
        var (svc, fornecedores, produtos, encomendas, stocks, _, _, _) = Build();
        var (lojaId, fornecedor, produto) = SeedEncomendaPrereqs(fornecedores, produtos);
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), LojaId = lojaId, ProdutoId = produto.Id, Quantidade = 0 });

        var resp = await svc.CriarEncomendaAsync(new CriarEncomendaRequest
        {
            FornecedorId = fornecedor.Id,
            LojaId = lojaId,
            Linhas = new List<LinhaEncomendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 5 }
            }
        }, Guid.NewGuid(), lojaId, "127.0.0.1");
        await svc.SubmeterEncomendaAsync(resp.Value!.Id, Guid.NewGuid(), lojaId, isAdmin: false, "127.0.0.1");
        var linhaId = encomendas.Encomendas[0].Linhas.First().Id;

        var rec = await svc.RececionarEncomendaAsync(resp.Value.Id,
            new RececionarEncomendaRequest
            {
                Linhas = new List<LinhaRececionadaRequest>
                {
                    new() { LinhaId = linhaId, QuantidadeRecebida = 100 }
                }
            }, Guid.NewGuid(), lojaId, isAdmin: false, "127.0.0.1");

        rec.IsSuccess.Should().BeFalse();
        rec.ErrorCode.Should().Be(EncomendasErrorCodes.QuantidadeRecebidaInvalida);
    }

    [Fact]
    public async Task RecepcionarEncomenda_Total_IncrementaStockEAtualizaEstado()
    {
        var (svc, fornecedores, produtos, encomendas, stocks, audit, _, _) = Build();
        var (lojaId, fornecedor, produto) = SeedEncomendaPrereqs(fornecedores, produtos);
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), LojaId = lojaId, ProdutoId = produto.Id, Quantidade = 0 });

        var resp = await svc.CriarEncomendaAsync(new CriarEncomendaRequest
        {
            FornecedorId = fornecedor.Id,
            LojaId = lojaId,
            Linhas = new List<LinhaEncomendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 5 }
            }
        }, Guid.NewGuid(), lojaId, "127.0.0.1");
        await svc.SubmeterEncomendaAsync(resp.Value!.Id, Guid.NewGuid(), lojaId, isAdmin: false, "127.0.0.1");
        var linhaId = encomendas.Encomendas[0].Linhas.First().Id;

        var rec = await svc.RececionarEncomendaAsync(resp.Value.Id,
            new RececionarEncomendaRequest
            {
                Linhas = new List<LinhaRececionadaRequest>
                {
                    new() { LinhaId = linhaId, QuantidadeRecebida = 5 }
                }
            }, Guid.NewGuid(), lojaId, isAdmin: false, "127.0.0.1");

        rec.IsSuccess.Should().BeTrue();
        stocks.Stocks[0].Quantidade.Should().Be(5);
        encomendas.Encomendas[0].Estado.Should().Be(EstadoEncomenda.Recebida);
        audit.Contains(AuditAcoes.EncomendaRecebida).Should().BeTrue();
    }

    [Fact]
    public async Task RecepcionarEncomenda_Parcial_DeixaEstadoEmParcialmenteRecebida()
    {
        var (svc, fornecedores, produtos, encomendas, stocks, audit, _, _) = Build();
        var (lojaId, fornecedor, produto) = SeedEncomendaPrereqs(fornecedores, produtos);
        stocks.Stocks.Add(new Stock { Id = Guid.NewGuid(), LojaId = lojaId, ProdutoId = produto.Id, Quantidade = 0 });

        var resp = await svc.CriarEncomendaAsync(new CriarEncomendaRequest
        {
            FornecedorId = fornecedor.Id,
            LojaId = lojaId,
            Linhas = new List<LinhaEncomendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 10 }
            }
        }, Guid.NewGuid(), lojaId, "127.0.0.1");
        await svc.SubmeterEncomendaAsync(resp.Value!.Id, Guid.NewGuid(), lojaId, isAdmin: false, "127.0.0.1");
        var linhaId = encomendas.Encomendas[0].Linhas.First().Id;

        var rec = await svc.RececionarEncomendaAsync(resp.Value.Id,
            new RececionarEncomendaRequest
            {
                Linhas = new List<LinhaRececionadaRequest>
                {
                    new() { LinhaId = linhaId, QuantidadeRecebida = 6 }
                }
            }, Guid.NewGuid(), lojaId, isAdmin: false, "127.0.0.1");

        rec.IsSuccess.Should().BeTrue();
        stocks.Stocks[0].Quantidade.Should().Be(6);
        encomendas.Encomendas[0].Estado.Should().Be(EstadoEncomenda.ParcialmenteRecebida);
        audit.Contains(AuditAcoes.EncomendaParcialmenteRecebida).Should().BeTrue();
    }

    [Fact]
    public async Task CriarEncomenda_ProdutoNaoAssociado_Rejeita()
    {
        var (svc, fornecedores, produtos, _, _, _, _, _) = Build();
        var (lojaId, fornecedor, _) = SeedEncomendaPrereqs(fornecedores, produtos);
        var produtoOutro = new Produto { Id = Guid.NewGuid(), Ean13 = "5601234567892" };
        produtos.Produtos.Add(produtoOutro);

        var resp = await svc.CriarEncomendaAsync(new CriarEncomendaRequest
        {
            FornecedorId = fornecedor.Id,
            LojaId = lojaId,
            Linhas = new List<LinhaEncomendaRequest>
            {
                new() { ProdutoId = produtoOutro.Id, Quantidade = 1 }
            }
        }, Guid.NewGuid(), lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(EncomendasErrorCodes.ProdutoSemAssociacao);
    }

    [Fact]
    public async Task CriarEncomenda_FornecedorInativo_Rejeita()
    {
        var (svc, fornecedores, produtos, _, _, _, _, _) = Build();
        var (lojaId, fornecedor, produto) = SeedEncomendaPrereqs(fornecedores, produtos);
        fornecedor.Ativo = false;

        var resp = await svc.CriarEncomendaAsync(new CriarEncomendaRequest
        {
            FornecedorId = fornecedor.Id,
            LojaId = lojaId,
            Linhas = new List<LinhaEncomendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1 }
            }
        }, Guid.NewGuid(), lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(EncomendasErrorCodes.FornecedorInativo);
    }

    private static (Guid lojaId, Fornecedor fornecedor, Produto produto) SeedEncomendaPrereqs(
        InMemoryFornecedorRepository fornecedores,
        InMemoryProdutoRepository produtos,
        decimal precoCusto = 100m,
        decimal desconto = 0m)
    {
        var lojaId = Guid.NewGuid();
        var produto = new Produto { Id = Guid.NewGuid(), Ean13 = "5601234567892", Descricao = "P" };
        produtos.Produtos.Add(produto);
        var fornecedor = new Fornecedor
        {
            Id = Guid.NewGuid(),
            Nif = "123456789",
            Nome = "Fornec",
            Ativo = true
        };
        fornecedor.ProdutosAssociados.Add(new ProdutoFornecedor
        {
            FornecedorId = fornecedor.Id,
            ProdutoId = produto.Id,
            PrecoCusto = precoCusto,
            DescontoPercentagem = desconto,
            PrazoEntregaDias = 7
        });
        fornecedores.Fornecedores.Add(fornecedor);
        return (lojaId, fornecedor, produto);
    }
}
