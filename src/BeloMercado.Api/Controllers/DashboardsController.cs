using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Dashboards;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/dashboards")]
[Authorize(Roles = $"{PerfilRoles.GerenteLoja},{PerfilRoles.AdminCadeia}")]
public class DashboardsController : ControllerBase
{
    private readonly ReportingService _service;
    private readonly IClock _clock;

    public DashboardsController(ReportingService service, IClock clock)
    {
        _service = service;
        _clock = clock;
    }

    /// <summary>
    /// UC13 — KPIs por loja num intervalo de datas. Gerente vê só a sua
    /// loja; Admin pode passar lojaId opcional para filtrar (omitir
    /// = todas as lojas).
    /// </summary>
    [HttpGet("kpi")]
    [ProducesResponseType(typeof(IReadOnlyList<KPIDiarioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpis(
        [FromQuery] Guid? lojaId,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        CancellationToken cancellationToken)
    {
        var hoje = _clock.Today;
        var inicio = dataInicio ?? hoje.AddDays(-30);
        var fim = dataFim ?? hoje;

        // Gerente forçado à própria loja
        var scopeLojaId = User.IsInRole(PerfilRoles.AdminCadeia)
            ? lojaId
            : ResolveLojaId();

        var kpis = await _service.GetKpisAsync(scopeLojaId, inicio, fim, cancellationToken);
        return Ok(kpis);
    }

    /// <summary>
    /// UC13 — Ranking top-10 produtos para uma (loja, data). Gerente
    /// limitado à sua loja.
    /// </summary>
    [HttpGet("kpi/{lojaId:guid}/{data}/ranking")]
    [ProducesResponseType(typeof(IReadOnlyList<RankingProdutoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRanking(
        Guid lojaId,
        DateOnly data,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(PerfilRoles.AdminCadeia)
            && ResolveLojaId() != lojaId)
        {
            return Forbid();
        }

        var ranking = await _service.GetRankingAsync(lojaId, data, cancellationToken);
        return Ok(ranking);
    }

    private Guid? ResolveLojaId()
    {
        var lojaIdClaim = User.FindFirst(JwtClaimNames.LojaId)?.Value;
        return Guid.TryParse(lojaIdClaim, out var id) ? id : null;
    }
}
