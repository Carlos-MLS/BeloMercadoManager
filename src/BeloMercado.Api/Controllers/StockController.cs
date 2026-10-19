using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Stock;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/stock")]
[Authorize(Roles = $"{PerfilRoles.GerenteLoja},{PerfilRoles.AdminCadeia}")]
public class StockController : ControllerBase
{
    private readonly InventoryService _service;

    public StockController(InventoryService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var stocks = await _service.ListAsync(ResolveScopeLojaId(), cancellationToken);
        return Ok(stocks);
    }

    [HttpGet("alertas")]
    [ProducesResponseType(typeof(IReadOnlyList<StockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Alertas(CancellationToken cancellationToken)
    {
        var alertas = await _service.ListAlertasAsync(ResolveScopeLojaId(), cancellationToken);
        return Ok(alertas);
    }

    [HttpPut("{id:guid}/limites")]
    [ProducesResponseType(typeof(StockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateLimits(
        Guid id,
        [FromBody] UpdateStockLimitsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateLimitsAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
    }

    private IActionResult MapFailure<T>(OperationResult<T> result)
    {
        var statusCode = result.Status switch
        {
            OperationStatus.NotFound => StatusCodes.Status404NotFound,
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

    /// <summary>
    /// AdminCadeia sees all stores; Gerente is scoped to their JWT lojaId.
    /// </summary>
    private Guid? ResolveScopeLojaId()
    {
        if (User.IsInRole(PerfilRoles.AdminCadeia))
        {
            return null;
        }
        var lojaIdClaim = User.FindFirst(JwtClaimNames.LojaId)?.Value;
        return Guid.TryParse(lojaIdClaim, out var id) ? id : Guid.Empty;
    }
}
