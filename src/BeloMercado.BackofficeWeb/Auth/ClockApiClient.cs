using System.Net.Http.Json;

namespace BeloMercado.BackofficeWeb.Auth;

public class ClockApiClient : AuthorizedApiClient
{
    public ClockApiClient(HttpClient http, TokenStore tokens) : base(http, tokens) { }

    public async Task<DateOnly> GetTodayAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await SendAsync(HttpMethod.Get, "api/v1/clock/now", cancellationToken: ct);
            if (!resp.IsSuccessStatusCode) return DateOnly.FromDateTime(DateTime.Now);
            var body = await resp.Content.ReadFromJsonAsync<ClockNowResponse>(cancellationToken: ct);
            return body?.Today ?? DateOnly.FromDateTime(DateTime.Now);
        }
        catch
        {
            return DateOnly.FromDateTime(DateTime.Now);
        }
    }

    private class ClockNowResponse
    {
        public DateTime UtcNow { get; set; }
        public DateTime Now { get; set; }
        public DateOnly Today { get; set; }
        public int OffsetDias { get; set; }
    }
}
