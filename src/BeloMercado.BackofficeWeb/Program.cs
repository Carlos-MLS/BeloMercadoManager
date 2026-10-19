using System.Globalization;
using BeloMercado.BackofficeWeb.Auth;
using BeloMercado.BackofficeWeb.Components;
using Microsoft.AspNetCore.Components.Authorization;

// Forçar cultura pt-PT em todo o processo: faz com que as mensagens de
// validação dos DataAnnotations (Required/Range/StringLength, etc.) saiam
// localizadas em português em vez do default inglês.
var ptPt = new CultureInfo("pt-PT");
CultureInfo.DefaultThreadCurrentCulture = ptPt;
CultureInfo.DefaultThreadCurrentUICulture = ptPt;

var builder = WebApplication.CreateBuilder(args);

// Razor Components + Interactive Server (Blazor Server architecture)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// API base URL — read from configuration so dev/prod can override.
// Default points to the locally-running API on port 5009.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5009/";

// HTTP clients typed por área da API
builder.Services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<DashboardsApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<LojasApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<UtilizadoresApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ProdutosApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<CategoriasApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<StockApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<PromocoesApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<FornecedoresApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<EncomendasApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<MeApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ClockApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));

// Auth: state provider + token store. ProtectedSessionStorage is registered
// automatically by AddRazorComponents when interactivity Server is enabled.
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthenticationStateProvider>());

// IAuthenticationService é exigido pela AuthorizationMiddleware (mesmo
// em Blazor Server com custom AuthenticationStateProvider). Registamos
// um cookie scheme fictício; a auth real é gerida pelo AuthenticationStateProvider.
builder.Services.AddAuthentication("BlazorAuth")
    .AddCookie("BlazorAuth", options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Aplica request localization com pt-PT como cultura única suportada para
// que cada request use o pt-PT (ResourceManager + formatos numéricos).
var supportedCultures = new[] { new CultureInfo("pt-PT") };
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("pt-PT"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
