# Backend — Services, Repositories, Endpoints

## Aplicação por serviço

Os serviços vivem em `BeloMercado.Application/Services/`. Cada um agrega
um conjunto coerente de UCs e usa apenas interfaces (`I*Repository`,
`IUnitOfWork`, `IAuditService`, hashers) — nunca o `DbContext` directamente.

### `AuthAccessService` (UC15, UC17)

Login, refresh token, logout, get/update do próprio perfil.

| Método | O que faz |
|--------|-----------|
| `LoginAsync(email, password, ip)` | Procura utilizador, valida password com **BCrypt timing-safe** (corre verify contra hash dummy mesmo quando o user não existe — mitigação CWE-203). Conta tentativas falhadas em memória; ao 5º falha bloqueia. Devolve `LoginResponse` com access token (15 min) + refresh token (7 dias). |
| `RefreshAsync(refreshToken)` | Valida o RT contra a tabela `RefreshTokens`, **rotaciona** (gera novo, marca o antigo como `Revogado`). Se o RT já tinha sido usado antes (replay), revoga toda a família. |
| `LogoutAsync(refreshToken)` | Marca o RT corrente como `Revogado`. |
| `GetMeAsync(actorId)` | Devolve `MeResponse` com nome, email, perfil, lojaId, último login. |
| `UpdateMeAsync(actorId, request, ip)` | Só aceita alterar **nome** (RN01 UC17: email/perfil read-only). Audit. |
| `ChangePasswordAsync(actorId, request, ip)` | Verifica password atual, valida nova com `PasswordPolicy` (8+ caracteres, 4-de-4: minúscula/maiúscula/dígito/símbolo), faz hash BCrypt, marca `PasswordTemporaria=false`. Bloqueia password idêntica à atual. |

### `UserManagementService` (UC16)

CRUD de utilizadores pelo Admin/IT.

| Método | O que faz |
|--------|-----------|
| `ListAsync()` | Devolve todos os utilizadores. |
| `CreateAsync(request, actorId, ip)` | Valida email único, valida que `Perfil ∈ {OperadorCaixa, GerenteLoja}` requer `LojaId`. Gera **password temporária aleatória** (12 chars, 4-de-4), hasheia, persiste com `PasswordTemporaria=true`. Devolve a password em claro **apenas na resposta da criação** (ADR: protótipo académico, sem email). |
| `UpdateAsync(id, request, actorId, ip)` | Atualiza nome/perfil/loja/Ativo. Valida transições. Audit. |
| `DeactivateAsync(id, actorId, ip)` | Soft delete (`Ativo=false`). |

### `StoreManagementService` (UC19)

CRUD de lojas. Sem DELETE — só soft delete via `UpdateAsync(... Ativo=false)`.

| Método | O que faz |
|--------|-----------|
| `ListAsync()` | Todas as lojas (ativas e inativas). |
| `CreateAsync(request, actorId, ip)` | Valida designação única, `StockMax >= StockMin`. Após persistir, **chama `InventoryService.InitializeStockForNewLojaAsync`** para materializar linhas de stock para todos os produtos existentes (qty=0). |
| `UpdateAsync(id, request, actorId, ip)` | Atualiza dados. |

### `CatalogService` (UC05, UC06 produtos)

Categorias e produtos.

| Método | O que faz |
|--------|-----------|
| `ListCategoriasAsync()` / `CreateCategoriaAsync(...)` | Categorias. Hierárquicas via `ParentCategoriaId`. |
| `ListProdutosAsync()` / `GetProdutoByIdAsync(...)` | Listagem e fetch. |
| `CreateProdutoAsync(request, ...)` | Valida **EAN-13** com `Ean13Validator` (dígito de controlo mod-10). Após persistir, **chama `InventoryService.InitializeStockForNewProdutoAsync`** para materializar uma linha de stock por loja. |
| `UpdateProdutoAsync(id, request, ...)` | EAN é imutável. Quando `Preco` muda, escreve uma linha nova em `HistoricoPreco`. |
| `DeactivateProdutoAsync(id, ...)` | Soft delete. |

### `InventoryService` (UC06 stock)

Consulta de stock.

| Método | O que faz |
|--------|-----------|
| `ListAsync(scopeLojaId, ...)` | Devolve stock. Se `scopeLojaId` é fornecido, filtra (Gerente fica preso à própria loja; Admin recebe `null` = todas). |
| `ListAlertasAsync(scopeLojaId, ...)` | Apenas linhas com `Quantidade < QtdMinima`. |
| `InitializeStockForNewLojaAsync(lojaId, ...)` | Cria linhas qty=0 para todos os produtos. Chamado por `StoreManagementService.CreateAsync`. |
| `InitializeStockForNewProdutoAsync(produtoId, ...)` | Cria linhas qty=0 em todas as lojas. Chamado por `CatalogService.CreateProdutoAsync`. |

### `SalesService` (UC01, UC02, UC03, UC04)

Maior serviço (≈ 850 linhas). Divide-se internamente em secções:

#### UC04 — Caixa diária

| Método | O que faz |
|--------|-----------|
| `GetCaixaByIdAsync(id, scopeLojaId, ...)` | Fetch com scope. |
| `AbrirCaixaAsync(request, actorId, lojaId, ip, ...)` | Valida que **não existe outra caixa aberta** (uma por dia per loja). Persiste com estado `Aberta` e `FundoInicial`. |
| `FecharCaixaAsync(id, request, actorId, ...)` | Calcula `ValorEsperado = FundoInicial + SUM(Vendas em Numerario)`. Compara com `ValorContado`. Se `|Diferenca| > 10€`, exige `Justificacao` (RN: `CashDiscrepancyThreshold = 10m`). Marca caixa `Fechada`. **Dispara consolidação imediata** via `ConsolidationService.ProcessAsync`. |
| `ListVendasByCaixaAsync(caixaId, scopeLojaId, ...)` | Devolve `VendaSummaryResponse[]` para o ecrã de anular/devolver. Filtra por `CaixaDiariaId` e ordena por hora desc. Apenas usado pelo PosWeb. |

#### UC01 — Registar venda (`CriarVendaAsync`)

Método mais complexo. Em alto nível:

1. **Idempotência**: olha em `IdempotencyEntries` pela tupla `(LojaId, IdempotencyKey)`. Se existe, devolve a `VendaResponse` cacheada **sem reprocessar**.
2. **Caixa aberta**: tem de existir uma caixa do dia para a loja com `Estado=Aberta`.
3. **Linhas**: para cada linha do request:
   - Procura o produto pelo EAN-13 ou pelo ID. Se não existe ou `Ativo=false`, falha.
   - Procura **promoção aplicável** com precedência `loja-específica > global`, mais recente como tie-breaker (`ListPromocoesActivesByProduto`).
   - Calcula `precoUnitario` (com promo aplicada se houver) e `subtotal`.
   - Calcula `valorIva` com **fórmula IVA-incluído**: `subtotal * (taxa / (100 + taxa))`.
   - Decrementa stock atomicamente (validate-then-decrement; falha se qty insuficiente).
4. **Cliente**: se `request.Cliente.Nif` presente, valida com `NifValidator` (mod-11), faz upsert em `Clientes`.
5. **Fatura**: gera número sequencial **MAX(seq)+1 per (LojaId, Ano)**. Constraint único previne duplicados em race; o catch retentas uma vez.
6. **Persistência**: tudo numa transação EF (RNF-06 ACID). Adiciona `IdempotencyEntry` com a serialização da resposta para idempotência futura.
7. Devolve `VendaResponse` completa (linhas + fatura).

#### UC02 — Anular e devolução

| Método | O que faz |
|--------|-----------|
| `AnularVendaAsync(id, request, actorId, lojaId, isGerente, isAdmin, ip, ...)` | RN01: operadores só podem anular nas primeiras 24h. Gerente/Admin sem limite. Reverte stock **apenas pela parte ainda não devolvida** (`Quantidade - QuantidadeDevolvida` por linha). Estado da venda passa a `Anulada`, audit completo. |
| `DevolucaoAsync(id, request, actorId, lojaId, isAdmin, ip, ...)` | Devolução parcial: incrementa `QuantidadeDevolvida` na linha (cumulativo), incrementa stock, mantém venda `Concluida`. Bloqueia se `Quantidade pedida > Quantidade - QuantidadeDevolvida`. |

#### UC03 — Promoções

| Método | O que faz |
|--------|-----------|
| `ListPromocoesAsync()` / `GetPromocaoByIdAsync(...)` | Listagem/fetch. |
| `CreatePromocaoAsync(request, ...)` | Valida `Tipo` ∈ `{Percentagem, PrecoFixo}` e `Valor` no domínio do tipo. |
| `UpdatePromocaoAsync(id, request, ...)` | Produto não é editável (chave). |
| `DeactivatePromocaoAsync(id, ...)` | Soft delete. |

A aplicação efetiva da promoção acontece dentro de `CriarVendaAsync` —
o controller `/api/v1/promocoes` é só CRUD.

### `ProcurementService` (UC09, UC10, UC11)

Fornecedores e encomendas.

| Método | O que faz |
|--------|-----------|
| `ListAsync()` (fornecedores) | Devolve `FornecedorResponse[]` com `ProdutosAssociados` carregados. |
| `CreateAsync(request, ...)` | Valida NIF mod-11, persiste fornecedor + ProdutosFornecedor (preço custo, desconto, prazo). |
| `UpdateAsync(id, request, ...)` | NIF imutável. **Substitui** o conjunto de ProdutosAssociados (RN03). |
| `ListEncomendasAsync(scopeLojaId, ...)` | Lista por loja (Gerente) ou todas (Admin). |
| `CriarEncomendaAsync(request, actorId, lojaId, ip, ...)` | Estado inicial `Rascunho`. Calcula total estimado com desconto da associação. Falha se algum `ProdutoId` não está associado a este fornecedor. |
| `SubmeterEncomendaAsync(id, actorId, lojaId, isAdmin, ip, ...)` | Estado passa a `Submetida`. **Se total > 5000€ e actor não é Admin** → retorna `Forbidden` com `errorCode=APPROVAL_REQUIRED`. |
| `RececionarEncomendaAsync(id, request, ...)` | Aceita receção parcial ou total. Para cada linha: incrementa `QuantidadeRecebida`, **incrementa stock da loja**, valida que não excede `Quantidade`. Estado passa a `Recebida` se todas as linhas completas, senão `ParcialmenteRecebida`. |

### `ConsolidationService` (UC12)

Materializa `KPIDiario` e `RankingProdutoDiario`.

| Método | O que faz |
|--------|-----------|
| `ProcessAsync(lojaId, data, ...)` | Lê todas as vendas de `(loja, dia)`, agrega: `TotalVendas` (excluindo anuladas), `NumTransacoes`, `TicketMedio`, `TotalIva`, `TotalDescontos`, `VendasAnuladas`, `Devolucoes`. Faz upsert em `KPIDiario`. Lê linhas de venda agregadas por produto, calcula receita líquida (subtraindo devoluções), **top-10** por receita → upsert em `RankingProdutoDiario`. Cria/atualiza `ConsolidacaoDiaria` com estado `Processada`. Em caso de excepção, marca `Falha` e incrementa contador. |

### `ConsolidationHostedService` (BackgroundService, Infrastructure)

Faz polling a cada 30 minutos. Para cada `ConsolidacaoDiaria` em estado
`Pendente` ou `Falha` (até 3 tentativas), reinvoca `ProcessAsync`. O hook
imediato no fecho de caixa garante que a maioria dos dias é processada
no momento; este serviço apanha falhas transientes (DB down, etc.).

### `ReportingService` (UC13)

Apenas leitura, alimenta o dashboard.

| Método | O que faz |
|--------|-----------|
| `GetKpisAsync(scopeLojaId, dataInicio, dataFim, ...)` | Lê `KPIDiarios` filtrados. Gerente só vê a sua loja. |
| `GetRankingAsync(lojaId, data, ...)` | Lê top-10 de `RankingProdutoDiarios`. |

### `AuditService` (transversal)

Single method: `LogAsync(actorId, ipAddress, acao, recurso, detalhes, ...)`.
Insere em `LogAuditoria`. Chamado de praticamente todos os outros serviços
em qualquer operação que muda estado.

## Repositórios

Todos em `Infrastructure/Repositories/`. Cada `I<Entity>Repository` tem um
contrato pequeno (Get/Add/Update + queries específicas). **Nunca chamam
`SaveChangesAsync`** — isso é responsabilidade do `IUnitOfWork` (que os
serviços chamam exactamente uma vez por operação).

Exemplos de queries não-triviais:

- `IVendaRepository.GetVendasForLojaDayAsync(lojaId, data)` — usado pelo
  `ConsolidationService` e pelo novo `SalesService.ListVendasByCaixaAsync`.
- `IVendaRepository.GetMaxFaturaSequencialAsync(lojaId, ano)` — para
  numeração sequencial de fatura.
- `IPromocaoRepository.ListActivesByProdutoAsync(produtoId, lojaId, hoje)` —
  ordenado para que loja-específica fique antes da global.

## Catálogo de endpoints (`/api/v1/...`)

Todos esperam `Authorization: Bearer <jwt>` excepto onde indicado.

### `auth` — sem autenticação obrigatória nos POST

```
POST   /auth/login              { email, password }              → 200 + tokens / 401
POST   /auth/refresh            { refreshToken }                  → 200 + tokens / 401
POST   /auth/logout             { refreshToken } (Authorization:) → 204
```

### `me` — qualquer autenticado

```
GET    /me                                                         → MeResponse
PUT    /me                      { nome }                           → MeResponse
PUT    /me/password             { currentPassword, newPassword }   → MeResponse
```

### `lojas` — `AdminCadeia` apenas

```
GET    /lojas                                                      → LojaResponse[]
POST   /lojas                   CreateLojaRequest                  → 201 LojaResponse
PUT    /lojas/{id}              UpdateLojaRequest                  → LojaResponse
```

### `utilizadores` — `AdminCadeia | AdminIT`

```
GET    /utilizadores                                               → UtilizadorResponse[]
POST   /utilizadores            CreateUtilizadorRequest            → 201 + tempPassword
PUT    /utilizadores/{id}       UpdateUtilizadorRequest            → UtilizadorResponse
DELETE /utilizadores/{id}                                          → 204 (soft delete)
```

### `categorias` — qualquer autenticado lê; `AdminCadeia` cria

```
GET    /categorias                                                 → CategoriaResponse[]
POST   /categorias              CreateCategoriaRequest             → 201
```

### `produtos` — qualquer autenticado lê; `AdminCadeia` muta

```
GET    /produtos                                                   → ProdutoResponse[]
GET    /produtos/{id}                                              → ProdutoResponse
POST   /produtos                CreateProdutoRequest               → 201
PUT    /produtos/{id}           UpdateProdutoRequest               → ProdutoResponse
DELETE /produtos/{id}                                              → 204 (soft delete)
```

### `stock` — `GerenteLoja | AdminCadeia` (Gerente scoped via JWT)

```
GET    /stock                                                      → StockResponse[]
GET    /stock/alertas                                              → StockResponse[]  (abaixo do mín.)
```

### `caixas` — caixeiros e gerentes

```
GET    /caixas/{id}                                                → CaixaResponse
GET    /caixas/{id}/vendas                                         → VendaSummaryResponse[]
POST   /caixas/abrir            AbrirCaixaRequest                  → 201 CaixaResponse
POST   /caixas/{id}/fechar      FecharCaixaRequest                 → CaixaResponse
```

### `vendas` — `OperadorCaixa | GerenteLoja`

```
GET    /vendas/{id}                                                → VendaResponse
POST   /vendas                  CriarVendaRequest                  → 201 VendaResponse
                                Idempotency-Key: <guid> obrigatório
POST   /vendas/{id}/anular      AnularVendaRequest                 → VendaResponse
POST   /vendas/{id}/devolucao   DevolucaoRequest                   → VendaResponse
```

### `promocoes` — qualquer autenticado lê; `AdminCadeia` muta

```
GET    /promocoes                                                  → PromocaoResponse[]
GET    /promocoes/{id}                                             → PromocaoResponse
POST   /promocoes               CreatePromocaoRequest               → 201
PUT    /promocoes/{id}          UpdatePromocaoRequest               → PromocaoResponse
DELETE /promocoes/{id}                                             → 204 (soft delete)
```

### `fornecedores` — `GerenteLoja | AdminCadeia` lêem; `AdminCadeia` muta

```
GET    /fornecedores                                               → FornecedorResponse[]
POST   /fornecedores            CreateFornecedorRequest             → 201
PUT    /fornecedores/{id}       UpdateFornecedorRequest             → FornecedorResponse
```

### `encomendas` — `GerenteLoja | AdminCadeia`

```
GET    /encomendas                                                 → EncomendaResponse[]
GET    /encomendas/{id}                                            → EncomendaResponse
POST   /encomendas              CriarEncomendaRequest               → 201 (Gerente apenas)
POST   /encomendas/{id}/submeter                                   → EncomendaResponse
                                                                     (403 APPROVAL_REQUIRED se >5000€ e !Admin)
POST   /encomendas/{id}/rececionar  RececionarEncomendaRequest      → EncomendaResponse
                                                                     (Gerente apenas)
```

### `dashboards` — `GerenteLoja | AdminCadeia` (Gerente scoped)

```
GET    /dashboards/kpi?lojaId=&dataInicio=&dataFim=                → KPIDiarioResponse[]
GET    /dashboards/kpi/{lojaId}/{data}/ranking                     → RankingProdutoResponse[]
```

## Mapeamento de errores HTTP

A maioria dos endpoints devolve um `ProblemDetails` (RFC 7807) em caso de
erro, com a chave `extensions.errorCode` populada com um dos códigos abaixo.
O front-end usa este código para mostrar mensagens amigáveis em PT.

| HTTP | Quando |
|------|--------|
| 400 | Body mal formado ou `Idempotency-Key` em falta no `POST /vendas` |
| 401 | JWT em falta / inválido / expirado, ou login falhado |
| 403 | Role insuficiente (`[Authorize(Roles=...)]`) ou regra `Forbidden` (ex: `APPROVAL_REQUIRED`) |
| 404 | Entidade não encontrada (ou Gerente a tentar aceder a outra loja) |
| 409 | Conflito de estado (`CAIXA_JA_FECHADA`, `VENDA_JA_ANULADA`, ...) ou unique (`EAN_DUPLICADO`) |
| 422 | Validação semântica falhou (`EAN_INVALIDO`, `STOCK_INSUFICIENTE`, ...) |

Catálogo completo dos errorCodes em `BeloMercado.Application/Common/Constants/`:
`AuthErrorCodes`, `CaixaErrorCodes`, `VendasErrorCodes`, etc.
