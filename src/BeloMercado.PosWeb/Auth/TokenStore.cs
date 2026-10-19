using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BeloMercado.PosWeb.Auth;

/// <summary>
/// Wraps Blazor's ProtectedSessionStorage to persist JWT tokens for the
/// duration of the browser session. Server-side encrypted (data protection
/// API), so tokens are not visible in DevTools storage as plaintext.
/// </summary>
public class TokenStore
{
    private const string AccessTokenKey = "bm_access_token";
    private const string RefreshTokenKey = "bm_refresh_token";

    private readonly ProtectedSessionStorage _storage;

    public TokenStore(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var result = await _storage.GetAsync<string>(AccessTokenKey);
            return result.Success ? result.Value : null;
        }
        catch (InvalidOperationException)
        {
            // Chamado durante prerender (sem JS interop ainda). O componente
            // ApiAuthenticationStateProvider trata isto como "anonymous" e
            // re-pergunta no primeiro render interactivo.
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            var result = await _storage.GetAsync<string>(RefreshTokenKey);
            return result.Success ? result.Value : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task SetAsync(string accessToken, string refreshToken)
    {
        await _storage.SetAsync(AccessTokenKey, accessToken);
        await _storage.SetAsync(RefreshTokenKey, refreshToken);
    }

    public async Task ClearAsync()
    {
        await _storage.DeleteAsync(AccessTokenKey);
        await _storage.DeleteAsync(RefreshTokenKey);
    }
}
