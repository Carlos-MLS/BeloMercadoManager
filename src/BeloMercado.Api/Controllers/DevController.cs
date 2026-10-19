using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Interfaces.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

/// <summary>
/// Endpoints só para ambiente de desenvolvimento — máquina do tempo que altera
/// o offset (em dias) lido por todo o código de negócio através de IClock.
/// Não toca no relógio do SO nem nos timestamps do JWT/auth. Bloqueado em
/// produção.
/// </summary>
[ApiController]
[Route("api/v1/dev")]
[Authorize(Roles = PerfilRoles.AdminCadeia)]
public class DevController : ControllerBase
{
    private readonly IClock _clock;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DevController> _logger;

    public DevController(
        IClock clock,
        IWebHostEnvironment env,
        ILogger<DevController> logger)
    {
        _clock = clock;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Avança o relógio aplicacional em N dias. As operações seguintes
    /// (abrir caixa, registar venda, etc.) passam a usar a data deslocada.
    /// Os dados existentes não são alterados — ficam naturalmente "no passado"
    /// relativamente ao novo agora.
    /// </summary>
    [HttpPost("avancar-dias")]
    public async Task<IActionResult> AvancarDias(
        [FromQuery] int dias = 1,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        if (dias <= 0 || dias > 365)
        {
            return BadRequest("Indica um número de dias entre 1 e 365.");
        }

        var before = _clock.OffsetDays;
        await _clock.ShiftAsync(dias, cancellationToken);
        _logger.LogWarning(
            "DEV: relógio avançado em {Dias} dias (offset {Before} → {After}). NÃO usar em produção.",
            dias, before, _clock.OffsetDays);

        return Ok(new
        {
            mensagem = $"Relógio avançado em {dias} dia(s). Recarrega o frontend.",
            offsetDias = _clock.OffsetDays,
            agora = _clock.UtcNow
        });
    }

    /// <summary>
    /// Recua o relógio aplicacional em N dias. Útil para anular um avanço.
    /// </summary>
    [HttpPost("recuar-dias")]
    public async Task<IActionResult> RecuarDias(
        [FromQuery] int dias = 1,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        if (dias <= 0 || dias > 365)
        {
            return BadRequest("Indica um número de dias entre 1 e 365.");
        }

        var before = _clock.OffsetDays;
        await _clock.ShiftAsync(-dias, cancellationToken);
        _logger.LogWarning(
            "DEV: relógio recuado em {Dias} dias (offset {Before} → {After}).",
            dias, before, _clock.OffsetDays);

        return Ok(new
        {
            mensagem = $"Relógio recuado em {dias} dia(s). Recarrega o frontend.",
            offsetDias = _clock.OffsetDays,
            agora = _clock.UtcNow
        });
    }

    /// <summary>Repõe o offset a zero — relógio aplicacional = relógio real.</summary>
    [HttpPost("reset-relogio")]
    public async Task<IActionResult> ResetRelogio(CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }
        await _clock.SetOffsetAsync(0, cancellationToken);
        return Ok(new { mensagem = "Relógio aplicacional reposto.", offsetDias = 0 });
    }
}
