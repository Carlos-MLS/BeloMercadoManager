# Front-end — PosWeb e BackofficeWeb

Os dois apps Blazor têm a **mesma fundação técnica**, divergindo apenas no
conjunto de páginas. Ambos correm como Blazor Web App com `--interactivity Server`
(o template sucessor do antigo "Blazor Server").

## Fundação partilhada

Os ficheiros em `Auth/` são quase idênticos entre os dois apps (nomes de
classe iguais, namespaces diferentes). Cobrem:

### `TokenStore`

Wrapper sobre `ProtectedSessionStorage` (encriptação server-side via
Data Protection API). Métodos:

- `GetAccessTokenAsync()` / `GetRefreshTokenAsync()` — em prerender, captura
  `InvalidOperationException` (porque ainda não há JS interop) e devolve null.
  O `ApiAuthenticationStateProvider` interpreta isto como anonymous e
  re-pergunta no primeiro render interactivo.
- `SetAsync(access, refresh)` / `ClearAsync()`.

### `AuthApiClient`

Cliente HTTP para `/api/v1/auth`. **Não estende `AuthorizedApiClient`** porque
o login é a única operação que não precisa de Bearer.

- `LoginAsync(email, password)` → `LoginResponse?` (null em 401).
- `LogoutAsync(accessToken, refreshToken)` — best-effort; ignora exceções
  (queremos limpar o estado local mesmo se a API estiver down).

### `ApiAuthenticationStateProvider : AuthenticationStateProvider`

Subclasse do tipo Blazor que decide quem está autenticado. Estratégia:

1. Lê o JWT de `TokenStore`.
2. Se ausente → devolve `AuthenticationState` anónimo.
3. Faz **parse manual** (não usa `JwtSecurityTokenHandler`):
   - Split em `.`, descodifica o payload base64 (com `PadBase64` para os
     `=` em falta).
   - `JsonDocument.Parse` para extrair claims.
4. Verifica `exp`: se passou → devolve anónimo (login refrescará depois).
5. Constrói `ClaimsIdentity` com `nameType: "sub"`, `roleType: "role"` —
   é por isto que `<AuthorizeView Roles="AdminCadeia">` funciona com a
   claim curta `role` (em vez do URI WS-Federation que .NET usa por default).

Métodos públicos:

- `NotifyLoginAsync(access, refresh)` — chamado pelo `Login.razor` após
  receber `LoginResponse`. Persiste tokens e dispara
  `NotifyAuthenticationStateChanged` para refrescar `AuthorizeView`s.
- `NotifyLogoutAsync()` — limpa tokens e fica anónimo.

### `AuthorizedApiClient` (abstract base)

Já documentado em `architecture.md` — todos os clients tipados (`LojasApiClient`,
`VendasApiClient`, etc.) estendem-no.

### Wiring no `Program.cs`

Resumido:

```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5009/";
builder.Services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<XApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
// ... um por área

builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthenticationStateProvider>());

// AuthN cookie scheme dummy — só para satisfazer AuthorizationMiddleware
builder.Services.AddAuthentication("BlazorAuth").AddCookie("BlazorAuth", o =>
{
    o.LoginPath = "/login";
});
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
```

`Routes.razor` usa `<AuthorizeRouteView>` com `<NotAuthorized>` apontando para
`<RedirectToLogin />`, que é um component vazio que faz `Nav.NavigateTo("/login")`.

## PosWeb — páginas

URL base default: `http://localhost:5108`.

### `/login` ([Login.razor](../../src/BeloMercado.PosWeb/Components/Pages/Login.razor))

`<EditForm>` com email + password. No submit:
1. Chama `AuthApi.LoginAsync(...)`. Se null → mostra "Email ou password inválidos".
2. `AuthState.NotifyLoginAsync(access, refresh)` para persistir.
3. `Nav.NavigateTo("/")` — Home redireciona-te para o que precisares.

### `/` ([Home.razor](../../src/BeloMercado.PosWeb/Components/Pages/Home.razor))

`@attribute [Authorize]`. Mostra sub/role/lojaId do utilizador e botões para:
- **Nova Venda** → `/venda`
- **Caixa** → `/caixa`
- **Perfil** → `/perfil`
- **Sair** (logout)

### `/caixa` ([Caixa/Abrir.razor](../../src/BeloMercado.PosWeb/Components/Pages/Caixa/Abrir.razor))

Form para abrir caixa do dia. Apenas `OperadorCaixa`. Pede `FundoInicial`,
chama `CaixasApi.AbrirAsync(fundoInicial)`. Em sucesso, navega para `/caixa/{id}`.

Mensagens de erro mapeadas:
- `CAIXA_JA_ABERTA_HOJE` → "Já existe uma caixa aberta para hoje nesta loja."
- `CAIXA_FUNDO_INVALIDO` → "Fundo inicial inválido."

### `/caixa/{id}` ([Caixa/Detalhe.razor](../../src/BeloMercado.PosWeb/Components/Pages/Caixa/Detalhe.razor))

Detalhe da caixa. Mostra estado, fundo, valores. Se `Estado=Aberta`:
- Botões para **Iniciar Venda** e **Vendas do dia**.
- Form de **Fechar Caixa** (valor contado + justificação).

### `/caixa/{id}/vendas` ([Caixa/VendasDoDia.razor](../../src/BeloMercado.PosWeb/Components/Pages/Caixa/VendasDoDia.razor))

Lista as vendas registadas na caixa (via `GET /api/v1/caixas/{id}/vendas`).
Cada linha tem **Anular/Devolver** que leva a `/venda/{id}/anular`.

### `/venda` ([Venda/VendaCorrente.razor](../../src/BeloMercado.PosWeb/Components/Pages/Venda/VendaCorrente.razor))

Página mais complexa do PosWeb. **3 stages** num único componente
(`enum Stage { Carrinho, Pagamento, Concluida }`):

1. **Carrinho** — input EAN com autofocus via `ElementReference.FocusAsync()`.
   Cada scan/Enter chama `ProdutosApi.GetByEanAsync(ean)`. Se válido,
   adiciona linha; senão mostra erro inline. Permite editar quantidades
   e remover linhas. Total calculado em tempo real (com IVA incluído).
2. **Pagamento** — escolhe método (`Numerario | Cartao | MBWay`). Se Numerário,
   pede `ValorRecebido` e calcula troco em tempo real (`Math.Max(0, recebido - total)`).
   Permite adicionar NIF de cliente (validado client-side e server-side).
3. **Concluída** — mostra `FaturaResponse` (número, total, rodapé) e oferece
   "Nova Venda" (volta ao stage 1 com carrinho limpo) e "Voltar à caixa".

A `Idempotency-Key` é gerada **uma vez** quando o utilizador entra no stage 2
(`Guid.NewGuid()`). Reenviar a fatura (e.g. utilizador clica duas vezes no
botão Pagar) não cria duplicado.

### `/venda/{id}/anular` ([Venda/AnularDevolver.razor](../../src/BeloMercado.PosWeb/Components/Pages/Venda/AnularDevolver.razor))

Mostra a venda inteira (linhas, fatura, cliente). Duas zonas de ação:

- **Anular venda completa** — textarea para motivo (obrigatório), botão
  vermelho "Anular venda". Chama `VendasApi.AnularAsync(id, motivo)`.
- **Devolução parcial** — input numérico por linha (max=disponível), botão
  laranja "Devolver N unidade(s)" que faz `VendasApi.DevolucaoAsync(...)`.

Após qualquer ação faz reload da venda; se ficou anulada, esconde os formulários.

### `/perfil` ([Perfil.razor](../../src/BeloMercado.PosWeb/Components/Pages/Perfil.razor))

Três cards:
1. **Dados** — read-only (email, perfil, loja, último login).
2. **Alterar nome** — UC17 só permite mudar nome.
3. **Alterar password** — current + new + confirm. Validação client-side
   (length 8+, confirm match) e server-side (4-de-4 policy).

Mensagens de erro mapeadas: `PASSWORD_INVALIDA`, `PASSWORD_FRACA`, `PASSWORD_IGUAL`.

## BackofficeWeb — páginas

URL base default: `http://localhost:5132`.

### `/login` e `/perfil`

Idênticos aos do PosWeb (mesma estrutura, namespace diferente).

### `/` ([Home.razor](../../src/BeloMercado.BackofficeWeb/Components/Pages/Home.razor))

Grelha de **tiles** para cada área. Algumas estão envoltas em
`<AuthorizeView Roles="AdminCadeia">` (Lojas) para esconder de quem não é admin.

### `/dashboard` ([Dashboard/Dashboard.razor](../../src/BeloMercado.BackofficeWeb/Components/Pages/Dashboard/Dashboard.razor))

UC13. Filtros: data início/fim (default últimos 7 dias) e dropdown de loja
(apenas para Admin; Gerente fica scoped server-side via JWT lojaId).

- 4 cartões resumo: Total vendas, Transações, Ticket médio, Anuladas/Devoluções.
- Tabela diária com botão "Top 10" por linha — carrega o ranking inline
  via `Dashboards.GetRankingAsync(lojaId, data)`.

### `/produtos`, `/produtos/novo`, `/produtos/{id}`

UC05. Listagem com filtro por descrição/EAN, dropdown de categoria, toggle
"mostrar inativos". Para `AdminCadeia` aparece coluna de ações com Editar/Desativar.

Form de criação/edição: EAN é imutável depois de criado (chave). Mapeamento
de erros: `EAN_INVALIDO`, `EAN_DUPLICADO`, `CATEGORIA_NAO_ENCONTRADA`.

### `/categorias`

CRUD inline (form em cima, tabela em baixo). Suporta `ParentCategoriaId`
para hierarquias. Apenas leitura para não-admin.

### `/utilizadores`, `/utilizadores/novo`, `/utilizadores/{id}`

UC16. `AdminCadeia | AdminIT`. Listagem com badges de estado (Ativo/Pwd temporária).
Ações: Editar, Desativar (soft delete). O ecrã de criação **mostra a password
temporária no canto** após criar (não há email no protótipo).

Mensagens de erro: `EMAIL_EXISTS`, `PERFIL_REQUER_LOJA`, `LOJA_NAO_ENCONTRADA`.

### `/lojas`, `/lojas/nova`, `/lojas/{id}`

UC19. `AdminCadeia` only. Tabela completa, form com campos de morada/contacto/
horário e defaults de stock min/max.

### `/stock`

UC06. Read-only. Dois blocos:
1. **Painel de alertas** (fundo amarelo) com produtos abaixo do mínimo +
   coluna "falta para mín".
2. **Tabela completa** com filtros: pesquisa por descrição/EAN, dropdown de
   loja (aparece só se `lojas.Count > 1`), checkbox "só abaixo do mínimo".

Estado por linha: `OK | Abaixo do mínimo | No máximo`.

### `/promocoes`, `/promocoes/nova`, `/promocoes/{id}`

UC03. Listagem com filtros "só ativas" / "só vigentes hoje". Badges para
"Em vigor" e "Expirada".

Form: produto imutável depois de criado. Tipo `Percentagem` ou `PrecoFixo`,
com label do campo "Valor" a mudar consoante. Datas com `<InputDate>`.

### `/fornecedores`, `/fornecedores/novo`, `/fornecedores/{id}`

UC09. Form em duas secções:
- **Dados** (NIF imutável depois de criado).
- **Produtos associados** — tabela editável com `+ Adicionar produto`.
  Cada linha tem dropdown de produto, preço custo, desconto %, prazo dias.

### `/encomendas`, `/encomendas/nova`, `/encomendas/{id}`

UC11.

- **Lista** filtrada por estado (`Rascunho | Submetida | ParcialmenteRecebida | Recebida | Cancelada`).
- **Nova** (Gerente only):
  - Escolhe fornecedor → carrega os `ProdutosAssociados` na tabela.
  - Para cada produto associado, input de quantidade. Subtotal calculado
    em tempo real (`preco * qty * (1 - desconto/100)`).
  - Submeter cria como `Rascunho`. Para enviar, abre o detalhe.
- **Detalhe**:
  - Resumo (estado, fornecedor, datas, total).
  - Tabela de linhas com `Quantidade encomendada / Quantidade recebida / Subtotal`.
  - Se estado = `Rascunho` → botão "Submeter encomenda" (mostra hint sobre
    `APPROVAL_REQUIRED` se >5000€).
  - Se estado = `Submetida | ParcialmenteRecebida` (e role = Gerente) →
    inputs para "A rececionar agora" e botão "Rececionar N unidade(s)".

## API clients (resumo)

Cada client estende `AuthorizedApiClient` (excepto `AuthApiClient`).

| App | Client | Endpoints |
|-----|--------|-----------|
| Both | `AuthApiClient` | login, logout |
| PosWeb | `CaixasApiClient` | abrir, get, fechar, list-vendas |
| PosWeb | `ProdutosApiClient` | get-by-id, get-by-ean |
| PosWeb | `VendasApiClient` | create (idempotency), get, anular, devolucao |
| PosWeb | `MeApiClient` | get, update-nome, change-password |
| Backoffice | `LojasApiClient` | list, create, update |
| Backoffice | `UtilizadoresApiClient` | list, create, update, deactivate |
| Backoffice | `ProdutosApiClient` (CRUD-versão) | list, get, create, update, deactivate |
| Backoffice | `CategoriasApiClient` | list, create |
| Backoffice | `StockApiClient` | list, list-alertas |
| Backoffice | `DashboardsApiClient` | get-kpis, get-ranking |
| Backoffice | `PromocoesApiClient` | list, get, create, update, deactivate |
| Backoffice | `FornecedoresApiClient` | list, create, update |
| Backoffice | `EncomendasApiClient` | list, get, create, submeter, rececionar |
| Backoffice | `MeApiClient` | get, update-nome, change-password |

> **Nota**: alguns clients vivem no mesmo ficheiro físico para partilharem
> helpers (e.g. `CatalogoApiClient.cs` contém `ProdutosApiClient` e
> `CategoriasApiClient`; `ProcurementApiClient.cs` contém `Fornecedores` e
> `Encomendas`).

## Padrão de mensagens de erro

A maioria das páginas implementa um helper local:

```csharp
private static string MapError(string? code, int status) => code switch
{
    "EAN_DUPLICADO" => "Já existe um produto com este EAN.",
    "VENDA_JA_ANULADA" => "Esta venda já estava anulada.",
    null => $"Erro {status}.",
    _ => code  // fallback: mostra o code raw
};
```

Isto mantém o switch perto da UI, facilita traduções futuras e não polui o
backend com strings localizadas.
