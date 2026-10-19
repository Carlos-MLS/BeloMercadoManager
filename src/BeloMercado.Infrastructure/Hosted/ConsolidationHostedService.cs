using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeloMercado.Infrastructure.Hosted;

/// <summary>
/// UC12 cron + retry. Polls a cada N minutos para reprocessar consolidações
/// em Falha (tentativas&lt;3) ou Pendentes ainda não executadas. Garante
/// também que cada loja tem registo para o dia (gerado pelo trigger imediato
/// em FecharCaixaAsync; este service é mais de retry/safety net).
///
/// Backoff RNF-07 (30/60/90 min) é simulado pelo intervalo do timer e
/// pelo contador Tentativas — após 3 falhas, o registo fica em Falha
/// terminal para ser revisto manualmente.
/// </summary>
public class ConsolidationHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsolidationHostedService> _logger;

    public ConsolidationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConsolidationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConsolidationHostedService started; poll interval = {Interval}", PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndRetryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConsolidationHostedService scan failed");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (TaskCanceledException) { /* shutdown */ }
        }
    }

    private async Task ScanAndRetryAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var consolidacaoRepo = scope.ServiceProvider.GetRequiredService<IConsolidacaoRepository>();
        var service = scope.ServiceProvider.GetRequiredService<ConsolidationService>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var hoje = clock.Today;
        var pendings = await consolidacaoRepo.GetPendingAsync(hoje, cancellationToken);
        if (pendings.Count == 0)
        {
            _logger.LogDebug("No pending consolidations for {Hoje}", hoje);
            return;
        }

        foreach (var p in pendings)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await service.ExecuteForLojaDayAsync(p.LojaId, p.Data, "Agendada", cancellationToken);
        }
    }
}
