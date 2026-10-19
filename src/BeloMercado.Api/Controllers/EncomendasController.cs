using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Encomendas;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/encomendas")]
[Authorize(Roles = $"{PerfilRoles.GerenteLoja},{PerfilRoles.AdminCadeia}")]
public class EncomendasController : ControllerBase
{
    private readonly ProcurementService _service;

    public EncomendasController(ProcurementService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EncomendaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var encomendas = await _service.ListEncomendasAsync(ResolveScopeLojaId(), cancellationToken);
        return Ok(encomendas);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EncomendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var enc = await _service.GetEncomendaByIdAsync(id, ResolveScopeLojaId(), cancellationToken);
        return enc is null ? NotFound() : Ok(enc);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EncomendaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CriarEncomendaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CriarEncomendaAsync(
            request, ResolveUtilizadorId(), ResolveLojaId(), ResolveIpAddress(), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapFailure(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EncomendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CriarEncomendaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarEncomendaRascunhoAsync(
            id, request, ResolveUtilizadorId(), ResolveLojaId(),
            User.IsInRole(PerfilRoles.AdminCadeia),
            ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.CancelarEncomendaAsync(
            id, ResolveUtilizadorId(), ResolveLojaId(),
            User.IsInRole(PerfilRoles.AdminCadeia),
            ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result);
    }

    [HttpPost("{id:guid}/submeter")]
    [ProducesResponseType(typeof(EncomendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submeter(Guid id, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(PerfilRoles.AdminCadeia);
        var result = await _service.SubmeterEncomendaAsync(
            id, ResolveUtilizadorId(), ResolveLojaId(), isAdmin,
            ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    [HttpPost("{id:guid}/rececionar")]
    [ProducesResponseType(typeof(EncomendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Rececionar(
        Guid id,
        [FromBody] RececionarEncomendaRequest request,
        CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(PerfilRoles.AdminCadeia);
        var result = await _service.RececionarEncomendaAsync(
            id, request, ResolveUtilizadorId(), ResolveLojaId(), isAdmin,
            ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    private Guid? ResolveScopeLojaId()
        => User.IsInRole(PerfilRoles.AdminCadeia) ? null : ResolveLojaId();

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
