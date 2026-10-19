using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Vendas;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/caixas")]
[Authorize]
public class CaixasController : ControllerBase
{
    private readonly SalesService _service;

    public CaixasController(SalesService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{PerfilRoles.OperadorCaixa},{PerfilRoles.GerenteLoja}")]
    [ProducesResponseType(typeof(CaixaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scopeLojaId = ResolveLojaId();
        var caixa = await _service.GetCaixaByIdAsync(id, scopeLojaId, cancellationToken);
        return caixa is null ? NotFound() : Ok(caixa);
    }

    /// <summary>
    /// Devolve a caixa aberta de hoje para a loja do utilizador autenticado.
    /// Resposta 204 No Content se não houver caixa aberta hoje.
    /// </summary>
    [HttpGet("hoje")]
    [Authorize(Roles = $"{PerfilRoles.OperadorCaixa},{PerfilRoles.GerenteLoja}")]
    [ProducesResponseType(typeof(CaixaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetHoje(CancellationToken cancellationToken)
    {
        var lojaId = ResolveLojaId();
        if (lojaId is null) return NoContent();
        var caixa = await _service.GetCaixaHojeAsync(lojaId.Value, cancellationToken);
        return caixa is null ? NoContent() : Ok(caixa);
    }

    [HttpGet("{id:guid}/vendas")]
    [Authorize(Roles = $"{PerfilRoles.OperadorCaixa},{PerfilRoles.GerenteLoja}")]
    [ProducesResponseType(typeof(IReadOnlyList<VendaSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVendas(Guid id, CancellationToken cancellationToken)
    {
        var vendas = await _service.ListVendasByCaixaAsync(id, ResolveLojaId(), cancellationToken);
        return Ok(vendas);
    }

    [HttpPost("abrir")]
    [Authorize(Roles = PerfilRoles.OperadorCaixa)]
    [ProducesResponseType(typeof(CaixaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Abrir(
        [FromBody] AbrirCaixaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AbrirCaixaAsync(
            request, ResolveUtilizadorId(), ResolveLojaId(), ResolveIpAddress(), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/fechar")]
    [Authorize(Roles = PerfilRoles.OperadorCaixa)]
    [ProducesResponseType(typeof(CaixaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Fechar(
        Guid id,
        [FromBody] FecharCaixaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.FecharCaixaAsync(
            id, request, ResolveUtilizadorId(), ResolveLojaId(), ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    private IActionResult MapFailure<T>(OperationResult<T> result)
    {
        var statusCode = result.Status switch
        {
            OperationStatus.NotFound => StatusCodes.Status404NotFound,
            OperationStatus.Conflict => StatusCodes.Status409Conflict,
            OperationStatus.Validation => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = result.ErrorCode,
            Detail = result.ErrorMessage
        };
        problem.Extensions["errorCode"] = result.ErrorCode;
        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    private string ResolveIpAddress()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrEmpty(ip) ? "unknown" : ip;
    }

    private Guid ResolveUtilizadorId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private Guid? ResolveLojaId()
    {
        var lojaIdClaim = User.FindFirst(JwtClaimNames.LojaId)?.Value;
        return Guid.TryParse(lojaIdClaim, out var id) ? id : null;
    }
}
