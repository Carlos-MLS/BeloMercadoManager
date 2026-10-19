using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/lojas")]
[Authorize(Roles = PerfilRoles.AdminCadeia)]
public class LojasController : ControllerBase
{
    private readonly StoreManagementService _service;

    public LojasController(StoreManagementService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LojaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var lojas = await _service.ListAsync(cancellationToken);
        return Ok(lojas);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LojaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLojaRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = ResolveUtilizadorId();
        var ipAddress = ResolveIpAddress();
        var result = await _service.CreateAsync(request, actorId, ipAddress, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { id = result.Value!.Id }, result.Value)
            : MapFailure(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LojaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLojaRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = ResolveUtilizadorId();
        var ipAddress = ResolveIpAddress();
        var result = await _service.UpdateAsync(id, request, actorId, ipAddress, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : MapFailure(result);
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
}
