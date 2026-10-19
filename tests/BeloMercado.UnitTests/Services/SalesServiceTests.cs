using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Vendas;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// SalesService — UC01 (criar venda), UC02 (anular/devolver), UC03 (promoção),
/// UC04 (caixa). Cobre:
///   RN01a Não permite venda sem caixa aberta.
///   RN01b Aplica IVA "preço c/IVA incluído" por linha.
///   RN01c Decrementa stock em transação única.
///   RN01d Idempotência: mesma chave devolve mesma venda.
///   RN01e Numeração sequencial de fatura por loja/ano.
///   RN02a Anular &lt;24h por operador; &gt;24h requer gerente.
///   RN02b Devolução parcial actualiza QuantidadeDevolvida.
///   RN03a Promoção activa aplicada no preço; desconto registado.
///   RN04a Uma caixa por loja por dia; diferença &gt;10€ exige justificação.
/// </summary>
public class SalesServiceTests
{
    private static (SalesService svc,
        InMemoryCaixaRepository caixas,
        InMemoryVendaRepository vendas,
        InMemoryClienteRepository clientes,
        InMemoryIdempotencyRepository idemp,
        InMemoryStockRepository stocks,
        InMemoryProdutoRepository produtos,
        InMemoryPromocaoRepository promocoes,
        InMemoryLojaRepository lojas,
        FakeAuditService audit,
        FakeUnitOfWork uow,
        FakeClock clock,
        InMemoryConsolidacaoRepository consolidacao) Build()
    {
        var caixas = new InMemoryCaixaRepository();
        var vendas = new InMemoryVendaRepository();
        var clientes = new InMemoryClienteRepository();
        var idemp = new InMemoryIdempotencyRepository();
        var stocks = new InMemoryStockRepository();
        var produtos = new InMemoryProdutoRepository();
        var promocoes = new InMemoryPromocaoRepository();
        var lojas = new InMemoryLojaRepository();
        var audit = new FakeAuditService();
        var uow = new FakeUnitOfWork();
        var clock = new FakeClock();
        var consolidacao = new InMemoryConsolidacaoRepository();
        var consolidationSvc = new ConsolidationService(
            consolidacao, vendas, promocoes, audit, uow,
            NullLogger<ConsolidationService>.Instance, clock);
        var svc = new SalesService(caixas, vendas, clientes, idemp, stocks, produtos,
            promocoes, lojas, audit, uow, consolidationSvc, clock);
        return (svc, caixas, vendas, clientes, idemp, stocks, produtos, promocoes, lojas, audit, uow, clock, consolidacao);
    }

    private static (Guid lojaId, Guid operadorId, Guid produtoId) SeedCenario(
        InMemoryCaixaRepository caixas,
        InMemoryStockRepository stocks,
        InMemoryProdutoRepository produtos,
        InMemoryLojaRepository lojas,
        FakeClock clock,
        decimal precoUnit = 10m,
        int qtdStock = 100,
        TaxaIva taxa = TaxaIva.Normal)
    {
        var lojaId = Guid.NewGuid();
        var operadorId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        lojas.Lojas.Add(new Loja { Id = lojaId, Designacao = "Braga" });
        caixas.Caixas.Add(new CaixaDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            Data = clock.Today,
            DataHoraAbertura = clock.UtcNow,
            OperadorAberturaId = operadorId,
            FundoInicial = 50m,
            Estado = EstadoCaixa.Aberta
        });
        produtos.Produtos.Add(new Produto
        {
            Id = produtoId,
            Ean13 = "5601234567892",
            Descricao = "Produto Teste",
            Preco = precoUnit,
            TaxaIva = taxa,
            Ativo = true
        });
        stocks.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            ProdutoId = produtoId,
            Quantidade = qtdStock,
            QtdMinima = 5,
            QtdMaxima = 200
        });
        return (lojaId, operadorId, produtoId);
    }

    // ----------------- UC04 Caixa -----------------

    [Fact]
    public async Task AbrirCaixa_HappyPath_AdicionaCaixaERegistaAuditoria()
    {
        var (svc, caixas, _, _, _, _, _, _, _, audit, _, _, _) = Build();
        var operadorId = Guid.NewGuid();
        var lojaId = Guid.NewGuid();

        var resp = await svc.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 50m },
            operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        resp.Value!.FundoInicial.Should().Be(50m);
        caixas.Caixas.Should().HaveCount(1);
        audit.Contains(AuditAcoes.CaixaAberta).Should().BeTrue();
    }

    [Fact]
    public async Task AbrirCaixa_SemLojaAssociada_DevolveValidationError()
    {
        var (svc, _, _, _, _, _, _, _, _, _, _, _, _) = Build();

        var resp = await svc.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 50m },
            Guid.NewGuid(), actorLojaId: null, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CaixaErrorCodes.LojaObrigatoria);
    }

    [Fact]
    public async Task AbrirCaixa_FundoNegativo_Rejeita()
    {
        var (svc, _, _, _, _, _, _, _, _, _, _, _, _) = Build();

        var resp = await svc.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = -1m },
            Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CaixaErrorCodes.FundoInvalido);
    }

    [Fact]
    public async Task AbrirCaixa_QuandoJaExisteUmaParaOMesmoDia_DevolveConflict()
    {
        // RN04a — uma caixa por loja por dia
        var (svc, caixas, _, _, _, _, _, _, _, _, _, clock, _) = Build();
        var lojaId = Guid.NewGuid();
        caixas.Caixas.Add(new CaixaDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            Data = clock.Today,
            Estado = EstadoCaixa.Aberta
        });

        var resp = await svc.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 50m },
            Guid.NewGuid(), lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CaixaErrorCodes.CaixaJaAberta);
    }

    [Fact]
    public async Task FecharCaixa_DiferencaSuperiorA10_SemJustificacao_Rejeita()
    {
        // RN04 fluxo alternativo 6a
        var (svc, caixas, _, _, _, _, _, _, _, _, _, clock, _) = Build();
        var lojaId = Guid.NewGuid();
        var operadorId = Guid.NewGuid();
        var caixa = new CaixaDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            Data = clock.Today,
            DataHoraAbertura = clock.UtcNow,
            OperadorAberturaId = operadorId,
            FundoInicial = 50m,
            Estado = EstadoCaixa.Aberta
        };
        caixas.Caixas.Add(caixa);

        var resp = await svc.FecharCaixaAsync(caixa.Id,
            new FecharCaixaRequest { ValorContado = 100m, Justificacao = null },
            operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CaixaErrorCodes.JustificacaoObrigatoria);
    }

    [Fact]
    public async Task FecharCaixa_ComJustificacao_AceitaEMudaEstadoParaFechada()
    {
        var (svc, caixas, _, _, _, _, _, _, _, audit, _, clock, consolidacao) = Build();
        var lojaId = Guid.NewGuid();
        var operadorId = Guid.NewGuid();
        var caixa = new CaixaDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            Data = clock.Today,
            DataHoraAbertura = clock.UtcNow,
            OperadorAberturaId = operadorId,
            FundoInicial = 50m,
            Estado = EstadoCaixa.Aberta
        };
        caixas.Caixas.Add(caixa);

        var resp = await svc.FecharCaixaAsync(caixa.Id,
            new FecharCaixaRequest { ValorContado = 80m, Justificacao = "Sobra de troco." },
            operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        caixa.Estado.Should().Be(EstadoCaixa.Fechada);
        caixa.Diferenca.Should().Be(30m);
        caixa.ConsolidacaoTriggered.Should().BeTrue();
        audit.Contains(AuditAcoes.CaixaFechada).Should().BeTrue();
        // RN03 do UC04 — consolidação imediata
        consolidacao.Consolidacoes.Should().HaveCount(1);
    }

    [Fact]
    public async Task FecharCaixa_DiferencaInferiorA10_NaoExigeJustificacao()
    {
        var (svc, caixas, _, _, _, _, _, _, _, _, _, clock, _) = Build();
        var lojaId = Guid.NewGuid();
        var operadorId = Guid.NewGuid();
        var caixa = new CaixaDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            Data = clock.Today,
            DataHoraAbertura = clock.UtcNow,
            OperadorAberturaId = operadorId,
            FundoInicial = 50m,
            Estado = EstadoCaixa.Aberta
        };
        caixas.Caixas.Add(caixa);

        var resp = await svc.FecharCaixaAsync(caixa.Id,
            new FecharCaixaRequest { ValorContado = 55m },  // diff = 5€
            operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FecharCaixa_JaFechada_DevolveConflict()
    {
        var (svc, caixas, _, _, _, _, _, _, _, _, _, clock, _) = Build();
        var caixa = new CaixaDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = Guid.NewGuid(),
            Estado = EstadoCaixa.Fechada,
            Data = clock.Today
        };
        caixas.Caixas.Add(caixa);

        var resp = await svc.FecharCaixaAsync(caixa.Id,
            new FecharCaixaRequest { ValorContado = 50m },
            Guid.NewGuid(), caixa.LojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(CaixaErrorCodes.CaixaJaFechada);
    }

    // ----------------- UC01 Criar venda -----------------

    [Fact]
    public async Task CriarVenda_HappyPath_DecrementaStockEEmiteFatura()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, audit, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 50m,
            Linhas = new List<LinhaVendaRequest>
            {
                new() { ProdutoId = produtoId, Quantidade = 2 }
            }
        }, "key-1", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        vendas.Vendas.Should().HaveCount(1);
        vendas.Faturas.Should().HaveCount(1);
        stocks.Stocks[0].Quantidade.Should().Be(98); // 100 - 2
        // total = 2 * 10 = 20; troco = 50 - 20 = 30
        resp.Value!.Troco.Should().Be(30m);
        // Fatura formato FT YYYY/00001-xxxx
        resp.Value.Fatura.NumeroCompleto.Should().StartWith("FT ");
        resp.Value.Fatura.NumeroCompleto.Should().Contain("/00001-");
        audit.Contains(AuditAcoes.VendaFinalizada).Should().BeTrue();
    }

    [Fact]
    public async Task CriarVenda_Idempotencia_MesmaKeyDevolveMesmaVenda()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);

        var req = new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest>
            {
                new() { ProdutoId = produtoId, Quantidade = 1 }
            }
        };

        var r1 = await svc.CriarVendaAsync(req, "same-key", operadorId, lojaId, "127.0.0.1");
        var r2 = await svc.CriarVendaAsync(req, "same-key", operadorId, lojaId, "127.0.0.1");

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        r1.Value!.Id.Should().Be(r2.Value!.Id);
        vendas.Vendas.Should().HaveCount(1);  // só uma venda criada
        stocks.Stocks[0].Quantidade.Should().Be(99);
    }

    [Fact]
    public async Task CriarVenda_SemLinhas_Rejeita()
    {
        var (svc, caixas, _, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, _) = SeedCenario(caixas, stocks, produtos, lojas, clock);

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            Linhas = new List<LinhaVendaRequest>()
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.SemLinhas);
    }

    [Fact]
    public async Task CriarVenda_SemCaixaAberta_Rejeita()
    {
        var (svc, _, _, _, _, _, produtos, _, lojas, _, _, _, _) = Build();
        var lojaId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        lojas.Lojas.Add(new Loja { Id = lojaId });
        produtos.Produtos.Add(new Produto { Id = produtoId, Preco = 1m, TaxaIva = TaxaIva.Normal, Ativo = true });

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 10m,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", Guid.NewGuid(), lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.CaixaNaoAberta);
    }

    [Fact]
    public async Task CriarVenda_StockInsuficiente_Rejeita()
    {
        var (svc, caixas, _, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock, qtdStock: 1);

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 100m,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 5 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.StockInsuficiente);
    }

    [Fact]
    public async Task CriarVenda_ProdutoInativo_Rejeita()
    {
        var (svc, caixas, _, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);
        produtos.Produtos[0].Ativo = false;

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 100m,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.ProdutoInativo);
    }

    [Fact]
    public async Task CriarVenda_NumerarioInsuficiente_Rejeita()
    {
        var (svc, caixas, _, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Numerario,
            ValorRecebido = 5m,    // < total (10€)
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.ValorRecebidoInsuficiente);
    }

    [Fact]
    public async Task CriarVenda_ComNifInvalido_Rejeita()
    {
        var (svc, caixas, _, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } },
            Cliente = new ClienteRequest { Nif = "123456788", Nome = "X" }
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.NifInvalido);
    }

    [Fact]
    public async Task CriarVenda_NumeracaoFaturaSequencialPorLojaAno()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);

        var r1 = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k1", operadorId, lojaId, "127.0.0.1");
        var r2 = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k2", operadorId, lojaId, "127.0.0.1");

        vendas.Faturas[0].NumeroSequencial.Should().Be(1);
        vendas.Faturas[1].NumeroSequencial.Should().Be(2);
    }

    // ----------------- UC02 -----------------

    [Fact]
    public async Task AnularVenda_OperadorVendaRecente_RevertStockEEstadoAnulada()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, audit, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);
        await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 2 } }
        }, "k", operadorId, lojaId, "127.0.0.1");
        var vendaId = vendas.Vendas[0].Id;
        var stockAntes = stocks.Stocks[0].Quantidade;

        var resp = await svc.AnularVendaAsync(vendaId,
            new AnularVendaRequest { Motivo = "Cliente desistiu." },
            operadorId, lojaId, isGerente: false, isAdmin: false, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        vendas.Vendas[0].Estado.Should().Be(EstadoVenda.Anulada);
        stocks.Stocks[0].Quantidade.Should().Be(stockAntes + 2);
        audit.Contains(AuditAcoes.VendaAnulada).Should().BeTrue();
    }

    [Fact]
    public async Task AnularVenda_Mais24h_OperadorSemPermissao_Recusa()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);
        await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");
        clock.UtcNow = clock.UtcNow.AddHours(25);

        var resp = await svc.AnularVendaAsync(vendas.Vendas[0].Id,
            new AnularVendaRequest { Motivo = "x" },
            operadorId, lojaId, isGerente: false, isAdmin: false, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.ApprovalRequired);
    }

    [Fact]
    public async Task AnularVenda_Mais24h_Gerente_Aceita()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);
        await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");
        clock.UtcNow = clock.UtcNow.AddHours(25);

        var resp = await svc.AnularVendaAsync(vendas.Vendas[0].Id,
            new AnularVendaRequest { Motivo = "Gerente decidiu" },
            operadorId, lojaId, isGerente: true, isAdmin: false, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DevolucaoParcial_AtualizaQuantidadeDevolvidaEIncrementaStock()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, audit, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);
        await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 3 } }
        }, "k", operadorId, lojaId, "127.0.0.1");
        var venda = vendas.Vendas[0];
        var linhaId = venda.Linhas.First().Id;
        var stockAposVenda = stocks.Stocks[0].Quantidade;

        var resp = await svc.DevolucaoAsync(venda.Id,
            new DevolucaoRequest
            {
                Motivo = "Cliente pediu",
                Linhas = new List<LinhaDevolucaoRequest>
                {
                    new() { LinhaVendaId = linhaId, Quantidade = 1 }
                }
            },
            operadorId, lojaId, isAdmin: false, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        venda.Linhas.First().QuantidadeDevolvida.Should().Be(1);
        stocks.Stocks[0].Quantidade.Should().Be(stockAposVenda + 1);
        audit.Contains(AuditAcoes.VendaDevolucao).Should().BeTrue();
    }

    [Fact]
    public async Task Devolucao_QtdSuperiorAVendida_Rejeita()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, _, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock);
        await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        var resp = await svc.DevolucaoAsync(vendas.Vendas[0].Id,
            new DevolucaoRequest
            {
                Motivo = "x",
                Linhas = new List<LinhaDevolucaoRequest>
                {
                    new() { LinhaVendaId = vendas.Vendas[0].Linhas.First().Id, Quantidade = 5 }
                }
            },
            operadorId, lojaId, isAdmin: false, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(VendasErrorCodes.DevolucaoQtdInvalida);
    }

    // ----------------- UC03 Promoções -----------------

    [Fact]
    public async Task CriarVenda_ComPromocaoPercentagem_AplicaDesconto()
    {
        var (svc, caixas, vendas, _, _, stocks, produtos, promocoes, lojas, _, _, clock, _) = Build();
        var (lojaId, operadorId, produtoId) = SeedCenario(caixas, stocks, produtos, lojas, clock,
            precoUnit: 10m, taxa: TaxaIva.Normal);
        // -20% no produto
        promocoes.Promocoes.Add(new Promocao
        {
            Id = Guid.NewGuid(),
            Nome = "20% desc",
            ProdutoId = produtoId,
            LojaId = null,
            Tipo = TipoPromocao.Percentagem,
            Valor = 20m,
            DataInicio = clock.Today.AddDays(-1),
            DataFim = clock.Today.AddDays(7),
            Ativo = true
        });

        var resp = await svc.CriarVendaAsync(new CriarVendaRequest
        {
            MetodoPagamento = MetodoPagamento.Cartao,
            Linhas = new List<LinhaVendaRequest> { new() { ProdutoId = produtoId, Quantidade = 1 } }
        }, "k", operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        // preço efectivo: 10 - 2 = 8
        vendas.Vendas[0].Linhas.First().PrecoUnitario.Should().Be(8m);
        vendas.Vendas[0].Total.Should().Be(8m);
        promocoes.Linhas.Should().HaveCount(1);
        promocoes.Linhas[0].ValorDescontoAplicado.Should().Be(2m);
    }

    [Fact]
    public async Task CriarPromocao_PercentagemMaiorQue100_Rejeita()
    {
        var (svc, _, _, _, _, _, _, _, lojas, _, _, _, _) = Build();
        var (svc2, _, _, _, _, _, produtos, _, lojas2, _, _, _, _) = Build();
        var produtoId = Guid.NewGuid();
        produtos.Produtos.Add(new Produto { Id = produtoId });

        var resp = await svc2.CreatePromocaoAsync(new BeloMercado.Application.Dtos.Promocoes.CreatePromocaoRequest
        {
            Nome = "Inválida",
            ProdutoId = produtoId,
            LojaId = null,
            Tipo = TipoPromocao.Percentagem,
            Valor = 150m,
            DataInicio = DateOnly.FromDateTime(DateTime.UtcNow),
            DataFim = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(PromocoesErrorCodes.PercentagemInvalida);
    }

    [Fact]
    public async Task CriarPromocao_DataFimAntesDeInicio_Rejeita()
    {
        var (svc, _, _, _, _, _, produtos, _, _, _, _, _, _) = Build();
        var produtoId = Guid.NewGuid();
        produtos.Produtos.Add(new Produto { Id = produtoId });

        var resp = await svc.CreatePromocaoAsync(new BeloMercado.Application.Dtos.Promocoes.CreatePromocaoRequest
        {
            Nome = "Invertida",
            ProdutoId = produtoId,
            LojaId = null,
            Tipo = TipoPromocao.Percentagem,
            Valor = 10m,
            DataInicio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DataFim = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(PromocoesErrorCodes.DatasInvalidas);
    }
}
