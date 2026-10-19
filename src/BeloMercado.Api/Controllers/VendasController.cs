using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Vendas;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/vendas")]
[Authorize(Roles = $"{PerfilRoles.OperadorCaixa},{PerfilRoles.GerenteLoja}")]
public class VendasController : ControllerBase
{
    private const string IdempotencyHeader = "Idempotency-Key";

    private readonly SalesService _service;

    public VendasController(SalesService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var venda = await _service.GetVendaByIdAsync(id, ResolveLojaId(), cancellationToken);
        return venda is null ? NotFound() : Ok(venda);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VendaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CriarVendaRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var idemValues)
            || string.IsNullOrWhiteSpace(idemValues.ToString()))
        {
            return MakeProblem(
                StatusCodes.Status400BadRequest,
                VendasErrorCodes.IdempotencyKeyObrigatoria,
                $"Header {IdempotencyHeader} é obrigatório em POST /api/v1/vendas.");
        }

        var idemKey = idemValues.ToString();
        var result = await _service.CriarVendaAsync(
            request, idemKey, ResolveUtilizadorId(), ResolveLojaId(), ResolveIpAddress(), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/anular")]
    [ProducesResponseType(typeof(VendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(
        Guid id,
        [FromBody] AnularVendaRequest request,
        CancellationToken cancellationToken)
    {
        var isGerente = User.IsInRole(PerfilRoles.GerenteLoja);
        var isAdmin = User.IsInRole(PerfilRoles.AdminCadeia);
        var result = await _service.AnularVendaAsync(
            id, request, ResolveUtilizadorId(), ResolveLojaId(), isGerente, isAdmin,
            ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    [HttpPost("{id:guid}/devolucao")]
    [ProducesResponseType(typeof(VendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Devolucao(
        Guid id,
        [FromBody] DevolucaoRequest request,
        CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(PerfilRoles.AdminCadeia);
        var result = await _service.DevolucaoAsync(
            id, request, ResolveUtilizadorId(), ResolveLojaId(), isAdmin,
            ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    private IActionResult MapFailure<T>(OperationResult<T> result)
    {
        var statusCode = result.Status switch
        {
            OperationStatus.NotFound => StatusCodes.Status404NotFound,
            OperationStatus.Conflict => StatusCodes.Status409Conflict,
            OperationStatus.Validation => StatusCodes.Status422UnprocessableEntity,
            OperationStatus.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
        return MakeProblem(statusCode, result.ErrorCode, result.ErrorMessage);
    }

    private IActionResult MakeProblem(int statusCode, string? errorCode, string? message)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = errorCode,
            Detail = message
        };
        problem.Extensions["errorCode"] = errorCode;
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
