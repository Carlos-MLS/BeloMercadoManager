using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Promocoes;

namespace BeloMercado.BackofficeWeb.Auth;

public class PromocoesApiClient : AuthorizedApiClient
{
    public PromocoesApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<PromocaoResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/promocoes", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<PromocaoResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<PromocaoResponse>>(cancellationToken: ct);
        return list ?? new List<PromocaoResponse>();
    }

    public async Task<PromocaoResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, $"api/v1/promocoes/{id}", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<PromocaoResponse>(cancellationToken: ct);
    }

    public async Task<PromocoesApiResult<PromocaoResponse>> CreateAsync(
        CreatePromocaoRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/promocoes", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<PromocaoResponse>(cancellationToken: ct);
            return PromocoesApiResult<PromocaoResponse>.Ok(body);
        }
        return PromocoesApiResult<PromocaoResponse>.Error(
            (int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<PromocoesApiResult<PromocaoResponse>> UpdateAsync(
        Guid id, UpdatePromocaoRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, $"api/v1/promocoes/{id}", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<PromocaoResponse>(cancellationToken: ct);
            return PromocoesApiResult<PromocaoResponse>.Ok(body);
        }
        return PromocoesApiResult<PromocaoResponse>.Error(
            (int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<PromocoesApiResult<object>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Delete, $"api/v1/promocoes/{id}", cancellationToken: ct);
        if (resp.IsSuccessStatusCode) return PromocoesApiResult<object>.Ok(null);
        return PromocoesApiResult<object>.Error(
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

public class PromocoesApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }

    public static PromocoesApiResult<T> Ok(T? value) => new()
    {
        Success = true, StatusCode = 200, Value = value
    };

    public static PromocoesApiResult<T> Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}
