# Arquitetura

## Vista de conjunto

A solução `BeloMercadoManager.sln` tem **6 projetos** organizados em camadas
e dois front-ends Blazor. As setas representam dependências de projeto
(`<ProjectReference>`), no sentido único Domain ← Application ← Infrastructure ← Api.

```
┌─────────────────────────────────────────────────┐
│                   Front-ends                     │
│  ┌──────────────┐         ┌──────────────────┐  │
│  │ PosWeb       │         │ BackofficeWeb    │  │
│  │ (Blazor      │         │ (Blazor          │  │
│  │  Server)     │         │  Server)         │  │
│  └──────┬───────┘         └────────┬─────────┘  │
│         │ HTTP/JSON + Bearer       │             │
└─────────┼──────────────────────────┼────────────┘
          ▼                          ▼
   ┌──────────────────────────────────────────┐
   │              BeloMercado.Api              │
   │ Controllers + JwtBearer + Program.cs DI   │
   └──────────────────┬───────────────────────┘
                      ▼
   ┌──────────────────────────────────────────┐
   │         BeloMercado.Infrastructure        │
   │  EF DbContext + Repositórios + Hosted     │
   │  Services + JwtTokenGenerator + BCrypt    │
   └──────────────────┬───────────────────────┘
                      ▼
   ┌──────────────────────────────────────────┐
   │          BeloMercado.Application          │
   │   Services (regras de negócio) + DTOs +   │
   │   Interfaces + Validation + Constants     │
   └──────────────────┬───────────────────────┘
                      ▼
   ┌──────────────────────────────────────────┐
   │            BeloMercado.Domain             │
   │      Entities + Value Objects + Enums     │
   └──────────────────────────────────────────┘
```

PosWeb e BackofficeWeb também referenciam `BeloMercado.Application` (apenas
para reusar os DTOs em request/response — decisão registada como **ADR-009**).

## Estrutura de pastas

```
src/
├── BeloMercado.Domain/
│   ├── Entities/           Utilizador, Loja, Produto, Venda, ...
│   └── Enums/              Perfil, TaxaIva, MetodoPagamento, ...
│
├── BeloMercado.Application/
│   ├── Common/
│   │   ├── Constants/      JwtClaimNames, PerfilRoles, *ErrorCodes
│   │   └── Validation/     Ean13Validator, NifValidator, PasswordPolicy
│   ├── Dtos/<feature>/     Requests + Responses por área
│   ├── Interfaces/         IUnitOfWork, IPasswordHasher, I*Repository, ...
│   └── Services/           AuthAccessService, SalesService, ...
│
├── BeloMercado.Infrastructure/
│   ├── Auth/               BCryptPasswordHasher, JwtTokenGenerator, ...
│   ├── Hosted/             ConsolidationHostedService (BackgroundService)
│   ├── Persistence/        BeloMercadoDbContext + UnitOfWork
│   │   └── Configurations/ IEntityTypeConfiguration<T> por entidade
│   ├── Repositories/       Implementações dos I*Repository
│   ├── Migrations/         EF Core migrations
│   └── DependencyInjection.cs (AddInfrastructure)
│
├── BeloMercado.Api/
│   ├── Controllers/        Um por área (Auth, Lojas, Produtos, ...)
│   └── Program.cs          Pipeline + DI + JwtBearer + ProblemDetails
│
├── BeloMercado.PosWeb/
│   ├── Auth/               TokenStore, AuthApiClient, *ApiClient typed
│   ├── Components/
│   │   ├── Layout/         MainLayout
│   │   ├── Pages/          Login, Caixa, Venda, Perfil
│   │   ├── Routes.razor    AuthorizeRouteView + RedirectToLogin
│   │   └── App.razor
│   └── Program.cs          DI dos HttpClients + AuthN scheme dummy
│
└── BeloMercado.BackofficeWeb/
    └── (estrutura idêntica ao PosWeb, namespace BeloMercado.BackofficeWeb)
```

## Abstrações transversais

### `OperationResult<T>` (Application)

Wrapper de retorno para os métodos dos services. Em vez de excepções,
devolve um envelope com:

- `IsSuccess` (bool)
- `Value` (T?, presente em caso de sucesso)
- `Status` (`OperationStatus` enum: `Ok | NotFound | Conflict | Validation | Forbidden`)
- `ErrorCode` (string?, ex: `"EAN_DUPLICADO"`, `"PASSWORD_INVALIDA"`)
- `ErrorMessage` (string?, descrição amigável)

Os controllers fazem o mapeamento `OperationStatus → HTTP status`:

```csharp
// Padrão repetido em vários controllers:
private IActionResult MapFailure<T>(OperationResult<T> result) =>
    result.Status switch
    {
        OperationStatus.NotFound => /* 404 */,
        OperationStatus.Conflict => /* 409 */,
        OperationStatus.Validation => /* 422 */,
        OperationStatus.Forbidden => /* 403 */,
        _ => /* 500 */
    };
```

O `errorCode` aparece no body como **RFC 7807 ProblemDetails** com a chave
`extensions.errorCode` — é o que o front-end usa para mostrar mensagens
amigáveis em PT.

### `IUnitOfWork` (Application)

Interface mínima com `SaveChangesAsync(CancellationToken)`. Implementação
em `Infrastructure.Persistence.UnitOfWork` que delega para o `DbContext`.
Os services chamam-no **uma vez por operação** para garantir atomicidade
ACID (RNF-06). Repositórios não fazem `SaveChanges` por si só.

### `IAuditService` (Application)

Escreve linhas em `LogAuditoria` para cada operação relevante (criar/anular
venda, alterar utilizador, etc.). Recebe `actorId`, `ipAddress`, ação,
recurso, detalhes em JSON. Implementação em `Infrastructure.Auth.AuditService`.

### `AuthorizedApiClient` (PosWeb / BackofficeWeb)

Base abstract class para os typed HttpClients que precisam de Bearer token.
Cada chamada **lê o token corrente do TokenStore** — assim refreshes ou
logouts são vistos imediatamente sem reciclar o HttpClient.

```csharp
protected async Task<HttpResponseMessage> SendAsync(
    HttpMethod method, string path, object? body = null,
    IDictionary<string, string>? extraHeaders = null,
    CancellationToken cancellationToken = default)
{
    var req = new HttpRequestMessage(method, path);
    var token = await _tokens.GetAccessTokenAsync();
    if (!string.IsNullOrEmpty(token))
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (body is not null) req.Content = JsonContent.Create(body);
    foreach (var kv in extraHeaders ?? new Dictionary<string,string>())
        req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
    return await Http.SendAsync(req, cancellationToken);
}
```

### Pattern dos typed API clients

Em vez de usar `HttpClient` directamente em cada Razor component, cada área
da API tem um typed client (ex: `LojasApiClient`, `VendasApiClient`). É:

- registado em `Program.cs` com `AddHttpClient<T>(c => c.BaseAddress = ...)`;
- injetado por `@inject` na página;
- expõe métodos com nomes de domínio (`AbrirAsync`, `AnularAsync`, ...);
- devolve um `XApiResult<T>` com `Success | StatusCode | Value | ErrorCode`
  para que a página mostre mensagens de erro mapeadas.

## Pipeline da API (Program.cs resumido)

1. `AddDbContext<BeloMercadoDbContext>` (Connection string em User Secrets)
2. `AddInfrastructure(...)` regista repos + UoW + hashers + token generator
3. `AddControllers().AddJsonOptions(...)` com `JsonStringEnumConverter`
   global (enum chega ao client como string, não int)
4. `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
   - **`MapInboundClaims = false`** — sem isto, `[Authorize(Roles="...")]` falha
     silenciosamente porque .NET reescreve `role` para o URI WS-Federation
5. `AddAuthorization()` + `AddSwaggerGen` com Bearer scheme
6. Pipeline: `UseAuthentication → UseAuthorization → MapControllers`
7. ConsolidationHostedService arranca como `IHostedService`

## Pipeline dos Blazor apps (PosWeb / Backoffice idênticos)

1. `AddRazorComponents().AddInteractiveServerComponents()`
2. `AddHttpClient<XApiClient>(c => c.BaseAddress = ApiBaseUrl)` para cada client
3. `AddScoped<TokenStore>` + `AddScoped<ApiAuthenticationStateProvider>`
4. **AddAuthentication("BlazorAuth").AddCookie(...)** — cookie scheme dummy.
   Sem isto a `AuthorizationMiddleware` queixa-se que falta `IAuthenticationService`,
   mas a auth real corre por `ApiAuthenticationStateProvider`, não pelo cookie.
5. `AddAuthorizationCore` + `AddCascadingAuthenticationState`
6. `Routes.razor` envolve as páginas em `<AuthorizeRouteView>` com fallback
   para `<RedirectToLogin>` quando não autenticado.

## ADRs relevantes

Todas no `docs/CLAUDE.md`:

- **ADR-005** API path `/api/v1/...` (URL versioning)
- **ADR-006** Login policy: 401 sempre, sem distinguir email errado de password errada (RN02 UC15)
- **ADR-009** Web projects podem referenciar Application para reusar DTOs
- **ADR-010** Idempotency-Key obrigatório em `POST /vendas`

## Dependências externas

| Pacote | Para quê |
|--------|----------|
| `Microsoft.EntityFrameworkCore.SqlServer` | Persistência |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Validação de tokens |
| `BCrypt.Net-Next` | Hash de passwords (cost 11) |
| `Swashbuckle.AspNetCore` | Swagger UI |
| `Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage` | Token store encriptado |
