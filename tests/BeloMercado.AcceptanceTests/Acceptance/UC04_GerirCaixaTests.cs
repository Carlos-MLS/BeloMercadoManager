using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;

namespace BeloMercado.AcceptanceTests.Acceptance;

/// <summary>
/// UC04 — Gerir Caixa Diária.
/// Critérios: uma caixa por loja por dia; fecho com diferença &gt; 10€ exige
/// justificação; fecho dispara consolidação imediata (UC12).
/// </summary>
public class UC04_GerirCaixaTests
{
    [Fact]
    public async Task UC04_Principal_AbrirEFecharCaixa_EstadoFechadaEConsolidacaoConcluida()
    {
        using var f = new IntegrationTestFixture();
        var lojaId = await SeedLojaAsync(f);
        var sales = f.CreateSalesService();
        var operadorId = Guid.NewGuid();

        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 50m }, operadorId, lojaId, "127.0.0.1");
        var caixaId = f.Db.CaixasDiarias.Single().Id;

        var resp = await sales.FecharCaixaAsync(caixaId,
            new FecharCaixaRequest { ValorContado = 50m },
            operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        f.Db.CaixasDiarias.Single().Estado.Should().Be(EstadoCaixa.Fechada);
        f.Db.ConsolidacoesDiarias.Single().Estado.Should().Be(EstadoConsolidacao.Concluida);
    }

    [Fact]
    public async Task UC04_Alt1_DiferencaSuperiorA10SemJustificacao_Rejeita()
    {
        using var f = new IntegrationTestFixture();
        var lojaId = await SeedLojaAsync(f);
        var sales = f.CreateSalesService();
        var operadorId = Guid.NewGuid();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 50m }, operadorId, lojaId, "127.0.0.1");
        var caixaId = f.Db.CaixasDiarias.Single().Id;

        var resp = await sales.FecharCaixaAsync(caixaId,
            new FecharCaixaRequest { ValorContado = 100m, Justificacao = null },
            operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("CAIXA_JUSTIFICACAO_OBRIGATORIA");
        f.Db.CaixasDiarias.Single().Estado.Should().Be(EstadoCaixa.Aberta);
    }

    [Fact]
    public async Task UC04_Alt2_TentarAbrirSegundaCaixaNoMesmoDia_Rejeita()
    {
        using var f = new IntegrationTestFixture();
        var lojaId = await SeedLojaAsync(f);
        var sales = f.CreateSalesService();
        var operadorId = Guid.NewGuid();
        await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 50m }, operadorId, lojaId, "127.0.0.1");

        var resp = await sales.AbrirCaixaAsync(new AbrirCaixaRequest { FundoInicial = 30m },
            operadorId, lojaId, "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be("CAIXA_JA_ABERTA");
        f.Db.CaixasDiarias.Should().HaveCount(1);
    }

    private static async Task<Guid> SeedLojaAsync(IntegrationTestFixture f)
    {
        var stores = f.CreateStoreService();
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
        return loja.Value!.Id;
    }
}
