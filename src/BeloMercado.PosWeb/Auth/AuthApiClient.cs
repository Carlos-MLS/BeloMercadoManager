using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Auth;

namespace BeloMercado.PosWeb.Auth;

/// <summary>HTTP client for /api/v1/auth.</summary>
public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/v1/auth/login",
            new LoginRequest { Email = email, Password = password },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
    }

    public async Task LogoutAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout")
        {
            Content = JsonContent.Create(new RefreshRequest { RefreshToken = refreshToken })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        try
        {
            await _http.SendAsync(request, cancellationToken);
        }
        catch
        {
            // Logout é melhor-esforço; se a API estiver indisponível, ainda
            // queremos limpar o estado local.
        }
    }
}
