using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Auth;

namespace BeloMercado.PosWeb.Auth;

/// <summary>
/// Base para typed HttpClients que precisam de Bearer token. Cada chamada
/// lê o token corrente do TokenStore — assim, refreshes ou logouts são
/// vistos imediatamente sem reciclar o HttpClient.
/// Em caso de 401, tenta uma vez fazer refresh do access token e repete a chamada.
/// </summary>
public abstract class AuthorizedApiClient
{
    protected HttpClient Http { get; }
    private readonly TokenStore _tokens;

    protected AuthorizedApiClient(HttpClient http, TokenStore tokens)
    {
        Http = http;
        _tokens = tokens;
    }

    protected async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body = null,
        IDictionary<string, string>? extraHeaders = null,
        CancellationToken cancellationToken = default)
    {
        var resp = await SendOnceAsync(method, path, body, extraHeaders, cancellationToken);
        if (resp.StatusCode != HttpStatusCode.Unauthorized)
        {
            return resp;
        }

        if (!await TryRefreshAccessTokenAsync(cancellationToken))
        {
            return resp;
        }
        resp.Dispose();
        return await SendOnceAsync(method, path, body, extraHeaders, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string path,
        object? body,
        IDictionary<string, string>? extraHeaders,
        CancellationToken cancellationToken)
    {
        var req = new HttpRequestMessage(method, path);
        var token = await _tokens.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (body is not null)
        {
            req.Content = JsonContent.Create(body);
        }
        if (extraHeaders is not null)
        {
            foreach (var kv in extraHeaders)
            {
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }
        }
        return await Http.SendAsync(req, cancellationToken);
    }

    private async Task<bool> TryRefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        var refresh = await _tokens.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refresh))
        {
            return false;
        }
        try
        {
            var resp = await Http.PostAsJsonAsync(
                "api/v1/auth/refresh",
                new RefreshRequest { RefreshToken = refresh },
                cancellationToken);
            if (!resp.IsSuccessStatusCode) return false;
            var body = await resp.Content.ReadFromJsonAsync<RefreshResponse>(cancellationToken: cancellationToken);
            if (body is null || string.IsNullOrEmpty(body.AccessToken)) return false;
            await _tokens.SetAsync(body.AccessToken, body.RefreshToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
