using System.Net.Http.Json;
using BeloMercado.Application.Dtos.Catalogo;

namespace BeloMercado.PosWeb.Auth;

public class ProdutosApiClient : AuthorizedApiClient
{
    public ProdutosApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<IReadOnlyList<ProdutoResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var resp = await SendAsync(HttpMethod.Get, "api/v1/produtos", cancellationToken: ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<ProdutoResponse>();
        var list = await resp.Content.ReadFromJsonAsync<List<ProdutoResponse>>(cancellationToken: ct);
        return list ?? new List<ProdutoResponse>();
    }
}
