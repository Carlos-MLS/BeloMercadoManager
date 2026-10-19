using BeloMercado.Application.Interfaces.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

/// <summary>
/// Expõe a hora aplicacional para que o frontend respeite o offset configurado
/// via /api/v1/dev/avancar-dias. Apenas leitura, exige autenticação.
/// </summary>
[ApiController]
[Route("api/v1/clock")]
[Authorize]
public class ClockController : ControllerBase
{
    private readonly IClock _clock;

    public ClockController(IClock clock)
    {
        _clock = clock;
    }

    [HttpGet("now")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetNow() => Ok(new
    {
        utcNow = _clock.UtcNow,
        now = _clock.Now,
        today = _clock.Today,
        offsetDias = _clock.OffsetDays
    });
}
