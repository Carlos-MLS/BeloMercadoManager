using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Dashboards;

namespace BeloMercado.BackofficeWeb.Auth;

public class DashboardsApiClient : AuthorizedApiClient
{
    public DashboardsApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<KPIDiarioResponse>> GetKpisAsync(
        Guid? lojaId,
        DateOnly dataInicio,
        DateOnly dataFim,
        CancellationToken ct = default)
    {
        var qs = $"?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}";
        if (lojaId.HasValue) qs += $"&lojaId={lojaId.Value}";

        var resp = await SendAsync(HttpMethod.Get, $"api/v1/dashboards/kpi{qs}", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<KPIDiarioResponse>();

        var list = await resp.Content.ReadFromJsonAsync<List<KPIDiarioResponse>>(cancellationToken: ct);
        return list ?? new List<KPIDiarioResponse>();
    }

    public async Task<IReadOnlyList<RankingProdutoResponse>> GetRankingAsync(
        Guid lojaId, DateOnly data, CancellationToken ct = default)
    {
        var resp = await SendAsync(
            HttpMethod.Get,
            $"api/v1/dashboards/kpi/{lojaId}/{data:yyyy-MM-dd}/ranking",
            cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<RankingProdutoResponse>();

        var list = await resp.Content.ReadFromJsonAsync<List<RankingProdutoResponse>>(cancellationToken: ct);
        return list ?? new List<RankingProdutoResponse>();
    }
}
