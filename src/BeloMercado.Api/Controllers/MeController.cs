using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common;
using BeloMercado.Application.Dtos.Me;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly AuthAccessService _service;

    public MeController(AuthAccessService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var me = await _service.GetMeAsync(ResolveUtilizadorId(), cancellationToken);
        return me is null ? NotFound() : Ok(me);
    }

    [HttpPut]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateMeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateMeAsync(
            ResolveUtilizadorId(), request, ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    [HttpPut("password")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ChangePasswordAsync(
            ResolveUtilizadorId(), request, ResolveIpAddress(), cancellationToken);
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
