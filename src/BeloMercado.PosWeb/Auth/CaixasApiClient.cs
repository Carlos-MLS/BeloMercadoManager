using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Vendas;

namespace BeloMercado.PosWeb.Auth;

public class CaixasApiClient : AuthorizedApiClient
{
    public CaixasApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<CaixaApiResult> AbrirAsync(decimal fundoInicial, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "api/v1/caixas/abrir",
            new AbrirCaixaRequest { FundoInicial = fundoInicial }, cancellationToken: ct);

        if (resp.IsSuccessStatusCode)
        {
            var caixa = await resp.Content.ReadFromJsonAsync<CaixaResponse>(cancellationToken: ct);
            return CaixaApiResult.Ok(caixa);
        }

        return CaixaApiResult.Error((int)resp.StatusCode,
            await TryReadErrorAsync(resp, ct));
    }

    public async Task<CaixaResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, $"api/v1/caixas/{id}", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<CaixaResponse>(cancellationToken: ct);
    }

    /// <summary>Caixa aberta de hoje (se houver) para a loja do operador.</summary>
    public async Task<CaixaResponse?> GetHojeAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/caixas/hoje", cancellationToken: ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<CaixaResponse>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VendaSummaryResponse>> ListVendasAsync(Guid caixaId, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, $"api/v1/caixas/{caixaId}/vendas", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<VendaSummaryResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<VendaSummaryResponse>>(cancellationToken: ct);
        return list ?? new List<VendaSummaryResponse>();
    }

    public async Task<CaixaApiResult> FecharAsync(
        Guid id, decimal valorContado, string? justificacao, CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Post, $"api/v1/caixas/{id}/fechar",
            new FecharCaixaRequest { ValorContado = valorContado, Justificacao = justificacao },
            cancellationToken: ct);

        if (resp.IsSuccessStatusCode)
        {
            var caixa = await resp.Content.ReadFromJsonAsync<CaixaResponse>(cancellationToken: ct);
            return CaixaApiResult.Ok(caixa);
        }
        return CaixaApiResult.Error((int)resp.StatusCode, await TryReadErrorAsync(resp, ct));
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

public class CaixaApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public CaixaResponse? Caixa { get; init; }
    public string? ErrorCode { get; init; }

    public static CaixaApiResult Ok(CaixaResponse? caixa) => new()
    {
        Success = true, StatusCode = 200, Caixa = caixa
    };

    public static CaixaApiResult Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}
