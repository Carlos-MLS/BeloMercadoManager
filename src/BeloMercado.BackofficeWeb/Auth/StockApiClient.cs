using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Stock;

namespace BeloMercado.BackofficeWeb.Auth;

public class StockApiClient : AuthorizedApiClient
{
    public StockApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<StockResponse>> ListAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/stock", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<StockResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<StockResponse>>(cancellationToken: ct);
        return list ?? new List<StockResponse>();
    }

    public async Task<IReadOnlyList<StockResponse>> ListAlertasAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/stock/alertas", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<StockResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<StockResponse>>(cancellationToken: ct);
        return list ?? new List<StockResponse>();
    }

    public async Task<StockApiResult> UpdateLimitesAsync(
        Guid stockId, int qtdMinima, int qtdMaxima, CancellationToken ct = default)
    {
        var resp = await SendAsync(
            HttpMethod.Put,
            $"api/v1/stock/{stockId}/limites",
            body: new UpdateStockLimitsRequest { QtdMinima = qtdMinima, QtdMaxima = qtdMaxima },
            cancellationToken: ct);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<StockResponse>(cancellationToken: ct);
            return StockApiResult.Ok(body);
        }
        string? errorCode = null;
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: ct);
            errorCode = problem?.ErrorCode ?? problem?.Title;
        }
        catch { /* ignore */ }
        return StockApiResult.Error((int)resp.StatusCode, errorCode);
    }

    private class ApiProblem
    {
        public string? Title { get; set; }
        public string? ErrorCode { get; set; }
    }
}

public class StockApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public StockResponse? Value { get; init; }
    public string? ErrorCode { get; init; }

    public static StockApiResult Ok(StockResponse? value) => new()
    {
        Success = true, StatusCode = 200, Value = value
    };

    public static StockApiResult Error(int statusCode, string? errorCode) => new()
    {
        Success = false, StatusCode = statusCode, ErrorCode = errorCode
    };
}
