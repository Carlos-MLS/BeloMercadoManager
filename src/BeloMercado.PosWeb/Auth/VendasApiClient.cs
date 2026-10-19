using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Vendas;

namespace BeloMercado.PosWeb.Auth;

public class VendasApiClient : AuthorizedApiClient
{
    public VendasApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<VendaApiResult> CreateAsync(
        CriarVendaRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/vendas",
            body: request,
            extraHeaders: new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey },
            cancellationToken: ct);

        if (resp.IsSuccessStatusCode)
        {
            var venda = await resp.Content.ReadFromJsonAsync<VendaResponse>(cancellationToken: ct);
            return VendaApiResult.Ok(venda);
        }
        return VendaApiResult.Error((int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<VendaResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, $"api/v1/vendas/{id}", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<VendaResponse>(cancellationToken: ct);
    }

    public async Task<VendaApiResult> AnularAsync(Guid id, string motivo, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, $"api/v1/vendas/{id}/anular",
            body: new AnularVendaRequest { Motivo = motivo }, cancellationToken: ct);

        if (resp.IsSuccessStatusCode)
        {
            var venda = await resp.Content.ReadFromJsonAsync<VendaResponse>(cancellationToken: ct);
            return VendaApiResult.Ok(venda);
        }
        return VendaApiResult.Error((int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    public async Task<VendaApiResult> DevolucaoAsync(
        Guid id, DevolucaoRequest request, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, $"api/v1/vendas/{id}/devolucao",
            body: request, cancellationToken: ct);

        if (resp.IsSuccessStatusCode)
        {
            var venda = await resp.Content.ReadFromJsonAsync<VendaResponse>(cancellationToken: ct);
            return VendaApiResult.Ok(venda);
        }
        return VendaApiResult.Error((int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: ct);
            return problem?.ErrorCode ?? problem?.Error ?? problem?.Title;
        }
        catch
        {
            return null;
        }
    }

    private class ApiProblem
    {
        public string? Title { get; set; }
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }
    }
}

public class VendaApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public VendaResponse? Venda { get; init; }
    public string? ErrorCode { get; init; }

    public static VendaApiResult Ok(VendaResponse? venda) => new()
    {
        Success = true, StatusCode = 200, Venda = venda
    };

    public static VendaApiResult Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}
