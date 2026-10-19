using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Catalogo;

namespace BeloMercado.BackofficeWeb.Auth;

public class ProdutosApiClient : AuthorizedApiClient
{
    public ProdutosApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<ProdutoResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/produtos", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<ProdutoResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<ProdutoResponse>>(cancellationToken: ct);
        return list ?? new List<ProdutoResponse>();
    }

    public async Task<ProdutoResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, $"api/v1/produtos/{id}", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<ProdutoResponse>(cancellationToken: ct);
    }

    public async Task<CatalogoApiResult<ProdutoResponse>> CreateAsync(
        CreateProdutoRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/produtos", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<ProdutoResponse>(cancellationToken: ct);
            return CatalogoApiResult<ProdutoResponse>.Ok(body);
        }
        return CatalogoApiResult<ProdutoResponse>.Error(
            (int)resp.StatusCode, await CatalogoApiHelper.TryReadErrorAsync(resp, ct));
    }

    public async Task<CatalogoApiResult<ProdutoResponse>> UpdateAsync(
        Guid id, UpdateProdutoRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, $"api/v1/produtos/{id}", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<ProdutoResponse>(cancellationToken: ct);
            return CatalogoApiResult<ProdutoResponse>.Ok(body);
        }
        return CatalogoApiResult<ProdutoResponse>.Error(
            (int)resp.StatusCode, await CatalogoApiHelper.TryReadErrorAsync(resp, ct));
    }

    public async Task<CatalogoApiResult<object>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Delete, $"api/v1/produtos/{id}", cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            return CatalogoApiResult<object>.Ok(null);
        }
        return CatalogoApiResult<object>.Error(
            (int)resp.StatusCode, await CatalogoApiHelper.TryReadErrorAsync(resp, ct));
    }
}

public class CategoriasApiClient : AuthorizedApiClient
{
    public CategoriasApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<CategoriaResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/categorias", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<CategoriaResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<CategoriaResponse>>(cancellationToken: ct);
        return list ?? new List<CategoriaResponse>();
    }

    public async Task<CatalogoApiResult<CategoriaResponse>> CreateAsync(
        CreateCategoriaRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/categorias", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<CategoriaResponse>(cancellationToken: ct);
            return CatalogoApiResult<CategoriaResponse>.Ok(body);
        }
        return CatalogoApiResult<CategoriaResponse>.Error(
            (int)resp.StatusCode, await CatalogoApiHelper.TryReadErrorAsync(resp, ct));
    }
}

public class CatalogoApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }

    public static CatalogoApiResult<T> Ok(T? value) => new()
    {
        Success = true, StatusCode = 200, Value = value
    };

    public static CatalogoApiResult<T> Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}

internal static class CatalogoApiHelper
{
    private class ApiProblem
    {
        public string? Title { get; set; }
        public string? ErrorCode { get; set; }
    }

    public static async Task<string?> TryReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
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
}
