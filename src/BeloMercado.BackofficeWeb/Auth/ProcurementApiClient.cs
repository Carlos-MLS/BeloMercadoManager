using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Encomendas;
using BeloMercado.Application.Dtos.Fornecedores;

namespace BeloMercado.BackofficeWeb.Auth;

public class FornecedoresApiClient : AuthorizedApiClient
{
    public FornecedoresApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<FornecedorResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/fornecedores", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<FornecedorResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<FornecedorResponse>>(cancellationToken: ct);
        return list ?? new List<FornecedorResponse>();
    }

    public async Task<ProcurementApiResult<FornecedorResponse>> CreateAsync(
        CreateFornecedorRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/fornecedores", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<FornecedorResponse>(cancellationToken: ct);
            return ProcurementApiResult<FornecedorResponse>.Ok(body);
        }
        return ProcurementApiResult<FornecedorResponse>.Error(
            (int)resp.StatusCode, await ProcurementApiHelper.TryReadErrorAsync(resp, ct));
    }

    public async Task<ProcurementApiResult<FornecedorResponse>> UpdateAsync(
        Guid id, UpdateFornecedorRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, $"api/v1/fornecedores/{id}", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<FornecedorResponse>(cancellationToken: ct);
            return ProcurementApiResult<FornecedorResponse>.Ok(body);
        }
        return ProcurementApiResult<FornecedorResponse>.Error(
            (int)resp.StatusCode, await ProcurementApiHelper.TryReadErrorAsync(resp, ct));
    }
}

public class EncomendasApiClient : AuthorizedApiClient
{
    public EncomendasApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<EncomendaResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/encomendas", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<EncomendaResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<EncomendaResponse>>(cancellationToken: ct);
        return list ?? new List<EncomendaResponse>();
    }

    public async Task<EncomendaResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, $"api/v1/encomendas/{id}", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<EncomendaResponse>(cancellationToken: ct);
    }

    public async Task<ProcurementApiResult<EncomendaResponse>> CreateAsync(
        CriarEncomendaRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/encomendas", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<EncomendaResponse>(cancellationToken: ct);
            return ProcurementApiResult<EncomendaResponse>.Ok(body);
        }
        return ProcurementApiResult<EncomendaResponse>.Error(
            (int)resp.StatusCode, await ProcurementApiHelper.TryReadErrorAsync(resp, ct));
    }

    public async Task<ProcurementApiResult<EncomendaResponse>> UpdateAsync(
        Guid id, CriarEncomendaRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Put, $"api/v1/encomendas/{id}", body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<EncomendaResponse>(cancellationToken: ct);
            return ProcurementApiResult<EncomendaResponse>.Ok(body);
        }
        return ProcurementApiResult<EncomendaResponse>.Error(
            (int)resp.StatusCode, await ProcurementApiHelper.TryReadErrorAsync(resp, ct));
    }

    public async Task<ProcurementApiResult<EncomendaResponse>> CancelarAsync(
        Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Delete, $"api/v1/encomendas/{id}", cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            return ProcurementApiResult<EncomendaResponse>.Ok(null);
        }
        return ProcurementApiResult<EncomendaResponse>.Error(
            (int)resp.StatusCode, await ProcurementApiHelper.TryReadErrorAsync(resp, ct));
    }

    public async Task<ProcurementApiResult<EncomendaResponse>> SubmeterAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, $"api/v1/encomendas/{id}/submeter", cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<EncomendaResponse>(cancellationToken: ct);
            return ProcurementApiResult<EncomendaResponse>.Ok(body);
        }
        return ProcurementApiResult<EncomendaResponse>.Error(
            (int)resp.StatusCode, await ProcurementApiHelper.TryReadErrorAsync(resp, ct));
    }

    public async Task<ProcurementApiResult<EncomendaResponse>> RececionarAsync(
        Guid id, RececionarEncomendaRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, $"api/v1/encomendas/{id}/rececionar",
            body: request, cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<EncomendaResponse>(cancellationToken: ct);
            return ProcurementApiResult<EncomendaResponse>.Ok(body);
        }
        return ProcurementApiResult<EncomendaResponse>.Error(
            (int)resp.StatusCode, await ProcurementApiHelper.TryReadErrorAsync(resp, ct));
    }
}

public class ProcurementApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }

    public static ProcurementApiResult<T> Ok(T? value) => new()
    {
        Success = true, StatusCode = 200, Value = value
    };

    public static ProcurementApiResult<T> Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}

internal static class ProcurementApiHelper
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
