using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Lojas;

namespace BeloMercado.BackofficeWeb.Auth;

public class LojasApiClient : AuthorizedApiClient
{
    public LojasApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<LojaResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/lojas", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<LojaResponse>();

        var list = await resp.Content.ReadFromJsonAsync<List<LojaResponse>>(cancellationToken: ct);
        return list ?? new List<LojaResponse>();
    }

    public async Task<LojaApiResult<LojaResponse>> CreateAsync(
        CreateLojaRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/lojas", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<LojaResponse>(cancellationToken: ct);
            return LojaApiResult<LojaResponse>.Ok(body);
        }
        return LojaApiResult<LojaResponse>.Error(
            (int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<LojaApiResult<LojaResponse>> UpdateAsync(
        Guid id, UpdateLojaRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, $"api/v1/lojas/{id}", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<LojaResponse>(cancellationToken: ct);
            return LojaApiResult<LojaResponse>.Ok(body);
        }
        return LojaApiResult<LojaResponse>.Error(
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

public class LojaApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }

    public static LojaApiResult<T> Ok(T? value) => new()
    {
        Success = true, StatusCode = 200, Value = value
    };

    public static LojaApiResult<T> Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}
