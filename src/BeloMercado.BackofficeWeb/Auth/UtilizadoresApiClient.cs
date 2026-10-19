using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Utilizadores;

namespace BeloMercado.BackofficeWeb.Auth;

public class UtilizadoresApiClient : AuthorizedApiClient
{
    public UtilizadoresApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<UtilizadorResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/utilizadores", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<UtilizadorResponse>();

        var list = await resp.Content.ReadFromJsonAsync<List<UtilizadorResponse>>(cancellationToken: ct);
        return list ?? new List<UtilizadorResponse>();
    }

    public async Task<UtilizadorApiResult<CreateUtilizadorResponse>> CreateAsync(
        CreateUtilizadorRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/utilizadores", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<CreateUtilizadorResponse>(cancellationToken: ct);
            return UtilizadorApiResult<CreateUtilizadorResponse>.Ok(body);
        }
        return UtilizadorApiResult<CreateUtilizadorResponse>.Error(
            (int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<UtilizadorApiResult<UtilizadorResponse>> UpdateAsync(
        Guid id, UpdateUtilizadorRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, $"api/v1/utilizadores/{id}", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<UtilizadorResponse>(cancellationToken: ct);
            return UtilizadorApiResult<UtilizadorResponse>.Ok(body);
        }
        return UtilizadorApiResult<UtilizadorResponse>.Error(
            (int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<UtilizadorApiResult<object>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Delete, $"api/v1/utilizadores/{id}", cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            return UtilizadorApiResult<object>.Ok(null);
        }
        return UtilizadorApiResult<object>.Error(
            (int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: ct);
            return problem?.ErrorCode ?? problem?.Title;
        }
        catch
        {
            return null;
        }
    }

    private class ApiProblem
    {
        public string? Title { get; set; }
        public string? ErrorCode { get; set; }
    }
}

public class UtilizadorApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }

    public static UtilizadorApiResult<T> Ok(T? value) => new()
    {
        Success = true, StatusCode = 200, Value = value
    };

    public static UtilizadorApiResult<T> Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}
