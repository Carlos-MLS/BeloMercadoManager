using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// ConsolidationService — UC12 (Consolidação Diária). Regras:
///   RN12a Idempotente: estado Concluida não re-executa.
///   RN12b KPIs calculados sobre vendas Concluidas (anuladas/devoluções excluídas).
///   RN12c Ranking top-10 por receita líquida.
///   RN12d Retries até 3 tentativas; depois é skipped.
/// </summary>
public class ConsolidationServiceTests
{
    private static (ConsolidationService svc,
        InMemoryConsolidacaoRepository consolidacao,
        InMemoryVendaRepository vendas,
        InMemoryPromocaoRepository promocoes,
        FakeAuditService audit,
        FakeUnitOfWork uow,
        FakeClock clock) Build()
    {
        var consolidacao = new InMemoryConsolidacaoRepository();
        var vendas = new InMemoryVendaRepository();
        var promocoes = new InMemoryPromocaoRepository();
        var audit = new FakeAuditService();
        var uow = new FakeUnitOfWork();
        var clock = new FakeClock();
        var svc = new ConsolidationService(consolidacao, vendas, promocoes, audit, uow,
            NullLogger<ConsolidationService>.Instance, clock);
        return (svc, consolidacao, vendas, promocoes, audit, uow, clock);
    }

    [Fact]
    public async Task Execute_SemVendas_ProduzKpiVazio_EMarcaConcluida()
    {
        var (svc, consol, _, _, audit, _, clock) = Build();
        var lojaId = Guid.NewGuid();

        var ok = await svc.ExecuteForLojaDayAsync(lojaId, clock.Today, "Cron");

        ok.Should().BeTrue();
        consol.Consolidacoes.Should().HaveCount(1);
        consol.Consolidacoes[0].Estado.Should().Be(EstadoConsolidacao.Concluida);
        consol.Kpis.Should().HaveCount(1);
        consol.Kpis[0].NumTransacoes.Should().Be(0);
        consol.Kpis[0].TotalVendas.Should().Be(0);
        consol.Rankings.Should().BeEmpty();
        audit.Contains(AuditAcoes.ConsolidacaoConcluida).Should().BeTrue();
    }

    [Fact]
    public async Task Execute_ComVendasConcluidas_AgregaTotaisETicketMedio()
    {
        var (svc, consol, vendas, _, _, _, clock) = Build();
        var lojaId = Guid.NewGuid();
        var prod1 = Guid.NewGuid();

        vendas.Vendas.Add(BuildVenda(lojaId, clock.UtcNow, EstadoVenda.Concluida, prod1, qty: 2, subtotal: 20m, iva: 3.74m, total: 20m));
        vendas.Vendas.Add(BuildVenda(lojaId, clock.UtcNow, EstadoVenda.Concluida, prod1, qty: 3, subtotal: 30m, iva: 5.61m, total: 30m));

        await svc.ExecuteForLojaDayAsync(lojaId, clock.Today, "Imediata");

        consol.Kpis[0].NumTransacoes.Should().Be(2);
        consol.Kpis[0].TotalVendas.Should().Be(50m);
        consol.Kpis[0].TicketMedio.Should().Be(25m);
        consol.Rankings.Should().HaveCount(1);
        consol.Rankings[0].Posicao.Should().Be(1);
        consol.Rankings[0].QuantidadeVendida.Should().Be(5);
    }

    [Fact]
    public async Task Execute_ContaAnuladasNoCampoVendasAnuladas_E_NaoSomaTotais()
    {
        var (svc, consol, vendas, _, _, _, clock) = Build();
        var lojaId = Guid.NewGuid();
        vendas.Vendas.Add(BuildVenda(lojaId, clock.UtcNow, EstadoVenda.Concluida, Guid.NewGuid(), 1, 10m, 1.87m, 10m));
        vendas.Vendas.Add(BuildVenda(lojaId, clock.UtcNow, EstadoVenda.Anulada, Guid.NewGuid(), 1, 20m, 3.74m, 20m));

        await svc.ExecuteForLojaDayAsync(lojaId, clock.Today, "Imediata");

        consol.Kpis[0].NumTransacoes.Should().Be(1);
        consol.Kpis[0].VendasAnuladas.Should().Be(1);
        consol.Kpis[0].TotalVendas.Should().Be(10m);
    }

    [Fact]
    public async Task Execute_QuandoJaConcluida_DevolveTrueSemReexecutar()
    {
        var (svc, consol, vendas, _, _, _, clock) = Build();
        var lojaId = Guid.NewGuid();
        consol.Consolidacoes.Add(new ConsolidacaoDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            Data = clock.Today,
            Estado = EstadoConsolidacao.Concluida
        });

        var ok = await svc.ExecuteForLojaDayAsync(lojaId, clock.Today, "Cron");

        ok.Should().BeTrue();
        consol.Kpis.Should().BeEmpty();   // não escreveu nada novo
    }

    [Fact]
    public async Task Execute_AposTresFalhas_NaoRetenta()
    {
        var (svc, consol, _, _, _, _, clock) = Build();
        var lojaId = Guid.NewGuid();
        consol.Consolidacoes.Add(new ConsolidacaoDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            Data = clock.Today,
            Estado = EstadoConsolidacao.Falha,
            Tentativas = 3
        });

        var ok = await svc.ExecuteForLojaDayAsync(lojaId, clock.Today, "Cron");

        ok.Should().BeFalse();
        consol.Consolidacoes[0].Tentativas.Should().Be(3); // não incrementou
    }

    private static Venda BuildVenda(Guid lojaId, DateTime dataHora, EstadoVenda estado, Guid produtoId,
        int qty, decimal subtotal, decimal iva, decimal total)
    {
        var v = new Venda
        {
            Id = Guid.NewGuid(),
            LojaId = lojaId,
            DataHora = dataHora,
            Estado = estado,
            TotalSemIva = total - iva,
            TotalIva = iva,
            Total = total
        };
        v.Linhas.Add(new LinhaVenda
        {
            Id = Guid.NewGuid(),
            VendaId = v.Id,
            ProdutoId = produtoId,
            Quantidade = qty,
            Subtotal = subtotal,
            ValorIva = iva,
            PrecoUnitario = total / qty,
            TaxaIvaPercentagem = 23
        });
        return v;
    }
}
