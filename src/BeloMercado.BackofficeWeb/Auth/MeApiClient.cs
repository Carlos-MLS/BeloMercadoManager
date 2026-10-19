using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Me;

namespace BeloMercado.BackofficeWeb.Auth;

public class MeApiClient : AuthorizedApiClient
{
    public MeApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<MeResponse?> GetAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/me", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<MeResponse>(cancellationToken: ct);
    }

    public async Task<MeApiResult<MeResponse>> UpdateNomeAsync(string nome, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, "api/v1/me",
            body: new UpdateMeRequest { Nome = nome }, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<MeResponse>(cancellationToken: ct);
            return MeApiResult<MeResponse>.Ok(body);
        }
        return MeApiResult<MeResponse>.Error((int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<MeApiResult<MeResponse>> ChangePasswordAsync(
        string current, string @new, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, "api/v1/me/password",
            body: new ChangePasswordRequest { CurrentPassword = current, NewPassword = @new },
            cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<MeResponse>(cancellationToken: ct);
            return MeApiResult<MeResponse>.Ok(body);
        }
        return MeApiResult<MeResponse>.Error((int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: ct);
            return problem?.ErrorCode ?? problem?.Title;
        }
        catch { return null; }
    }

    private class ApiProblem
    {
        public string? Title { get; set; }
        public string? ErrorCode { get; set; }
    }
}

public class MeApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }

    public static MeApiResult<T> Ok(T? value) => new() { Success = true, StatusCode = 200, Value = value };
    public static MeApiResult<T> Error(int statusCode, string? errorCode) =>
        new() { Success = false, StatusCode = statusCode, ErrorCode = errorCode };
}
