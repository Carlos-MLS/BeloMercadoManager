using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeloMercado.Application.Services;

/// <summary>
/// UC12 — calcula e materializa KPIDiario + RankingProdutoDiario para
/// uma (loja, data). É chamado:
///  - inline pelo SalesService.FecharCaixaAsync (consolidação imediata)
///  - pelo ConsolidationHostedService (cron diário + retry de Falhas)
/// </summary>
public class ConsolidationService
{
    private const int MaxTentativas = 3;
    private const int TopRanking = 10;

    private readonly IConsolidacaoRepository _consolidacaoRepository;
    private readonly IVendaRepository _vendaRepository;
    private readonly IPromocaoRepository _promocaoRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConsolidationService> _logger;
    private readonly IClock _clock;

    public ConsolidationService(
        IConsolidacaoRepository consolidacaoRepository,
        IVendaRepository vendaRepository,
        IPromocaoRepository promocaoRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<ConsolidationService> logger,
        IClock clock)
    {
        _consolidacaoRepository = consolidacaoRepository;
        _vendaRepository = vendaRepository;
        _promocaoRepository = promocaoRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _clock = clock;
    }

    public async Task<bool> ExecuteForLojaDayAsync(
        Guid lojaId,
        DateOnly data,
        string origem,
        CancellationToken cancellationToken = default)
    {
        var registo = await _consolidacaoRepository.GetByLojaDataAsync(lojaId, data, cancellationToken);
        if (registo is null)
        {
            registo = new ConsolidacaoDiaria
            {
                Id = Guid.NewGuid(),
                LojaId = lojaId,
                Data = data,
                Estado = EstadoConsolidacao.Pendente,
                Origem = origem
            };
            await _consolidacaoRepository.AddAsync(registo, cancellationToken);
        }

        if (registo.Estado == EstadoConsolidacao.Concluida)
        {
            return true; // already done; idempotent
        }

        if (registo.Tentativas >= MaxTentativas)
        {
            _logger.LogWarning("Consolidation for {LojaId} {Data} skipped — max attempts exceeded", lojaId, data);
            return false;
        }

        registo.Estado = EstadoConsolidacao.EmExecucao;
        registo.DataInicio = _clock.UtcNow;
        registo.Tentativas++;

        await _auditService.LogAsync(
            AuditAcoes.ConsolidacaoIniciada, AuditResultados.Sucesso, null, "system",
            targetEntidade: nameof(ConsolidacaoDiaria), targetId: registo.Id,
            detalhes: $"loja={lojaId} data={data} origem={origem} tentativa={registo.Tentativas}",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var vendas = await _vendaRepository.GetVendasForLojaDayAsync(lojaId, data, cancellationToken);
            var concluidas = vendas.Where(v => v.Estado == EstadoVenda.Concluida).ToList();
            var anuladas = vendas.Count(v => v.Estado == EstadoVenda.Anulada);
            var devolucoes = concluidas.Count(v => v.Linhas.Any(l => l.QuantidadeDevolvida > 0));

            // KPIs líquidos: por venda, considera total - devoluções (proporcional)
            decimal totalVendas = 0m, totalIva = 0m;
            int numTransacoes = concluidas.Count;
            var todasLinhasIds = new List<Guid>();

            foreach (var v in concluidas)
            {
                totalVendas += v.Total;
                totalIva += v.TotalIva;
                todasLinhasIds.AddRange(v.Linhas.Select(l => l.Id));
            }

            var totalDescontos = todasLinhasIds.Count == 0
                ? 0m
                : await _promocaoRepository.GetDescontoTotalForLinhasAsync(todasLinhasIds, cancellationToken);

            var ticketMedio = numTransacoes == 0 ? 0m : Math.Round(totalVendas / numTransacoes, 2);

            await _consolidacaoRepository.UpsertKpiAsync(new KPIDiario
            {
                LojaId = lojaId,
                Data = data,
                TotalVendas = Math.Round(totalVendas, 2),
                NumTransacoes = numTransacoes,
                TicketMedio = ticketMedio,
                TotalIva = Math.Round(totalIva, 2),
                TotalDescontos = Math.Round(totalDescontos, 2),
                VendasAnuladas = anuladas,
                Devolucoes = devolucoes,
                UltimaAtualizacao = _clock.UtcNow
            }, cancellationToken);

            // Ranking: agrupa linhas das vendas Concluídas por produto, ordena por receita líquida.
            var rankings = concluidas
                .SelectMany(v => v.Linhas)
                .GroupBy(l => l.ProdutoId)
                .Select(g =>
                {
                    var qtdLiquida = g.Sum(l => l.Quantidade - l.QuantidadeDevolvida);
                    var receita = g.Sum(l => l.Subtotal); // já reflete devoluções? não — ajusta:
                    // proporcional: (qty-dev)/qty * subtotal
                    receita = g.Sum(l => l.Quantidade == 0
                        ? 0m
                        : Math.Round(l.Subtotal * (l.Quantidade - l.QuantidadeDevolvida) / (decimal)l.Quantidade, 2));
                    return new { ProdutoId = g.Key, Qtd = qtdLiquida, Receita = receita };
                })
                .Where(r => r.Qtd > 0)
                .OrderByDescending(r => r.Receita)
                .ThenByDescending(r => r.Qtd)
                .Take(TopRanking)
                .Select((r, i) => new RankingProdutoDiario
                {
                    LojaId = lojaId,
                    Data = data,
                    ProdutoId = r.ProdutoId,
                    Posicao = i + 1,
                    QuantidadeVendida = r.Qtd,
                    Receita = r.Receita
                })
                .ToList();

            await _consolidacaoRepository.ReplaceRankingAsync(lojaId, data, rankings, cancellationToken);

            registo.Estado = EstadoConsolidacao.Concluida;
            registo.DataFim = _clock.UtcNow;
            registo.UltimaMensagemErro = null;

            await _auditService.LogAsync(
                AuditAcoes.ConsolidacaoConcluida, AuditResultados.Sucesso, null, "system",
                targetEntidade: nameof(ConsolidacaoDiaria), targetId: registo.Id,
                detalhes: $"vendas={numTransacoes} totalVendas={totalVendas:0.00}",
                cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Consolidation OK loja={LojaId} data={Data} vendas={NumVendas} total={Total}",
                lojaId, data, numTransacoes, totalVendas);
            return true;
        }
        catch (Exception ex)
        {
            registo.Estado = EstadoConsolidacao.Falha;
            registo.DataFim = _clock.UtcNow;
            registo.UltimaMensagemErro = ex.Message;

            await _auditService.LogAsync(
                AuditAcoes.ConsolidacaoFalha, AuditResultados.Falha, null, "system",
                targetEntidade: nameof(ConsolidacaoDiaria), targetId: registo.Id,
                detalhes: ex.Message,
                cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex,
                "Consolidation FAIL loja={LojaId} data={Data} attempt={Attempt}",
                lojaId, data, registo.Tentativas);
            return false;
        }
    }
}
