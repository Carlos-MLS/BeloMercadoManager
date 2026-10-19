using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/produtos")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly CatalogService _service;

    public ProdutosController(CatalogService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProdutoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var produtos = await _service.ListProdutosAsync(cancellationToken);
        return Ok(produtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var produto = await _service.GetProdutoByIdAsync(id, cancellationToken);
        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpPost]
    [Authorize(Roles = PerfilRoles.AdminCadeia)]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProdutoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateProdutoAsync(request, ResolveUtilizadorId(), ResolveIpAddress(), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapFailure(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = PerfilRoles.AdminCadeia)]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProdutoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateProdutoAsync(id, request, ResolveUtilizadorId(), ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = PerfilRoles.AdminCadeia)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeactivateProdutoAsync(id, ResolveUtilizadorId(), ResolveIpAddress(), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result);
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
