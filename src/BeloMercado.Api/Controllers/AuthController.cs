using System.IdentityModel.Tokens.Jwt;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Auth;
using BeloMercado.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeloMercado.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthAccessService _authService;

    public AuthController(AuthAccessService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = ResolveIpAddress();
        var result = await _authService.LoginAsync(request, ipAddress, cancellationToken);

        if (result is null)
        {
            return Unauthorized(new AuthErrorResponse
            {
                Error = AuthErrorCodes.InvalidCredentials,
                Message = "Invalid email or password."
            });
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = ResolveIpAddress();
        var result = await _authService.RefreshAsync(request, ipAddress, cancellationToken);

        if (result is null)
        {
            return Unauthorized(new AuthErrorResponse
            {
                Error = AuthErrorCodes.InvalidRefreshToken,
                Message = "Invalid or expired refresh token."
            });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = ResolveIpAddress();
        var utilizadorId = ResolveUtilizadorId();

        await _authService.LogoutAsync(request.RefreshToken, utilizadorId, ipAddress, cancellationToken);
        return NoContent();
    }

    private string ResolveIpAddress()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrEmpty(ip) ? "unknown" : ip;
    }

    private Guid? ResolveUtilizadorId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
