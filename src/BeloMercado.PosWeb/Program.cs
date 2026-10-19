using System.Globalization;
using BeloMercado.PosWeb.Auth;
using BeloMercado.PosWeb.Components;
using Microsoft.AspNetCore.Components.Authorization;

// Forçar cultura pt-PT em todo o processo: mensagens de validação dos
// DataAnnotations (Required/Range/...) saem localizadas em português.
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
builder.Services.AddHttpClient<CaixasApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ProdutosApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<VendasApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<MeApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));

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
    app.UseHsts();
}

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
