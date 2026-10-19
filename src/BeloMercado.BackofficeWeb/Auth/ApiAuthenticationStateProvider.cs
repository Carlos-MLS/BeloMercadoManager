using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace BeloMercado.BackofficeWeb.Auth;

/// <summary>
/// AuthenticationStateProvider que lê o JWT guardado em TokenStore,
/// parse-ia as claims (sub, role, lojaId, exp) e cria um ClaimsPrincipal.
/// </summary>
public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly TokenStore _tokenStore;

    public ApiAuthenticationStateProvider(TokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        var claims = ParseClaimsFromJwt(token).ToList();
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (long.TryParse(expClaim, out var expSeconds))
        {
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            if (expiresAt < DateTimeOffset.UtcNow)
            {
                // Token expired — treat as anonymous; o login refresca depois.
                return Anonymous;
            }
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt",
            nameType: "name", roleType: "role");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyLoginAsync(string accessToken, string refreshToken)
    {
        await _tokenStore.SetAsync(accessToken, refreshToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLogoutAsync()
    {
        await _tokenStore.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) yield break;

        var payload = parts[1];
        var bytes = Convert.FromBase64String(PadBase64(payload));
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            // JWT standard pode ter arrays para alguns claims; aqui assumimos
            // valores escalares (suficiente para sub/role/lojaId/exp/iat/nbf/aud/iss/jti).
            yield return new Claim(prop.Name, prop.Value.ToString());
        }
    }

    private static string PadBase64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
    }
}
