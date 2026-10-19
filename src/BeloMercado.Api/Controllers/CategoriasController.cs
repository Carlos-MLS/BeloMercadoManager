using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly CatalogService _service;

    public CategoriasController(CatalogService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var cats = await _service.ListCategoriasAsync(cancellationToken);
        return Ok(cats);
    }

    [HttpPost]
    [Authorize(Roles = PerfilRoles.AdminCadeia)]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoriaRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = ResolveUtilizadorId();
        var ipAddress = ResolveIpAddress();
        var result = await _service.CreateCategoriaAsync(request, actorId, ipAddress, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { id = result.Value!.Id }, result.Value)
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
