# Matriz de Rastreabilidade — SRS ↔ UC ↔ Testes

Esta matriz garante a rastreabilidade entre os requisitos do projeto e
os testes automáticos que os validam. Cada linha mostra o requisito,
o caso de uso em que se concretiza, e o(s) teste(s) que o exercita(m).

> Legenda: ✅ coberto por teste automático · 🟡 coberto parcialmente
> (regra principal coberta; ramo lateral não) · ❌ não coberto

## 1. Requisitos Funcionais (RF)

### UC01 — Registar Venda

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC01-1 | Sale requires ≥ 1 line | `SalesServiceTests.CriarVenda_SemLinhas_Rejeita`, `UC01_Principal_*` | ✅ |
| RF-UC01-2 | Cannot create sale without caixa aberta | `SalesServiceTests.CriarVenda_SemCaixaAberta_Rejeita`, `UC01_Alt1_*` | ✅ |
| RF-UC01-3 | IVA calculated per line | `SalesServiceTests.CriarVenda_HappyPath_*`, `UC01_Principal_*` | ✅ |
| RF-UC01-4 | Sequential invoice number per store/year | `SalesServiceTests.CriarVenda_NumeracaoFaturaSequencialPorLojaAno`, `SalesIntegrationTests.Venda_FaturaSequencial_*` | ✅ |
| RF-UC01-5 | Stock decrement in same transaction | `SalesIntegrationTests.FluxoCompleto_*`, `UC01_Principal_*` | ✅ |
| RF-UC01-6 | Audit `VENDA_FINALIZADA` emitted | `SalesServiceTests.CriarVenda_HappyPath_*` | ✅ |
| RF-UC01-7 | NIF validated when cliente present | `SalesServiceTests.CriarVenda_ComNifInvalido_Rejeita` | ✅ |
| RF-UC01-8 | Idempotency-Key — retry returns same venda | `SalesServiceTests.CriarVenda_Idempotencia_*`, `UC01_Alt3_*` | ✅ |
| RF-UC01-9 | Numerário: ValorRecebido ≥ Total | `SalesServiceTests.CriarVenda_NumerarioInsuficiente_Rejeita` | ✅ |
| RF-UC01-10 | Produto inativo recusado | `SalesServiceTests.CriarVenda_ProdutoInativo_Rejeita` | ✅ |

### UC02 — Anular / Devolver Venda

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC02-1 | Anular <24h por operador | `SalesServiceTests.AnularVenda_OperadorVendaRecente_*`, `UC02_Principal_*` | ✅ |
| RF-UC02-2 | Anular >24h exige Gerente | `SalesServiceTests.AnularVenda_Mais24h_*` (2 testes), `UC02_Alt1_*` | ✅ |
| RF-UC02-3 | Anulação repõe stock | `SalesServiceTests.AnularVenda_OperadorVendaRecente_*` | ✅ |
| RF-UC02-4 | Devolução parcial actualiza QuantidadeDevolvida | `SalesServiceTests.DevolucaoParcial_*`, `UC02_DevolucaoParcial_*` | ✅ |
| RF-UC02-5 | Devolução qty > vendida rejeitada | `SalesServiceTests.Devolucao_QtdSuperiorAVendida_Rejeita` | ✅ |

### UC03 — Aplicar Promoção

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC03-1 | Promoção activa aplicada automaticamente | `SalesServiceTests.CriarVenda_ComPromocaoPercentagem_AplicaDesconto` | ✅ |
| RF-UC03-2 | Registo em LinhaVendaPromocao | idem | ✅ |
| RF-UC03-3 | % entre 0 e 100 | `SalesServiceTests.CriarPromocao_PercentagemMaiorQue100_Rejeita` | ✅ |
| RF-UC03-4 | DataFim ≥ DataInicio | `SalesServiceTests.CriarPromocao_DataFimAntesDeInicio_Rejeita` | ✅ |

### UC04 — Gerir Caixa Diária

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC04-1 | 1 caixa por loja/dia | `SalesServiceTests.AbrirCaixa_QuandoJaExisteUmaParaOMesmoDia_*`, `UC04_Alt2_*` | ✅ |
| RF-UC04-2 | Diferença >10€ exige justificação | `SalesServiceTests.FecharCaixa_DiferencaSuperiorA10_*`, `UC04_Alt1_*` | ✅ |
| RF-UC04-3 | Diferença ≤10€ não exige | `SalesServiceTests.FecharCaixa_DiferencaInferiorA10_*` | ✅ |
| RF-UC04-4 | Fecho dispara consolidação imediata | `SalesServiceTests.FecharCaixa_ComJustificacao_*`, `UC04_Principal_*` | ✅ |
| RF-UC04-5 | Não permite fechar 2x | `SalesServiceTests.FecharCaixa_JaFechada_*` | ✅ |
| RF-UC04-6 | Fundo inicial ≥ 0 | `SalesServiceTests.AbrirCaixa_FundoNegativo_Rejeita` | ✅ |

### UC05 — Gerir Catálogo

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC05-1 | EAN-13 válido (mod-10) | `CatalogServiceTests.CreateProduto_Ean13Invalido_*`, `Ean13ValidatorTests.*`, `UC05_Alt1_*` | ✅ |
| RF-UC05-2 | EAN-13 único | `CatalogServiceTests.CreateProduto_Ean13Duplicado_*` | ✅ |
| RF-UC05-3 | Categoria existe | `CatalogServiceTests.CreateProduto_CategoriaInexistente_*` | ✅ |
| RF-UC05-4 | Histórico de preços materializado | `CatalogServiceTests.UpdateProduto_MudancaDePreco_*`, `UC05_Alt2_*`, `CatalogStockIntegrationTests.UpdateProduto_*` | ✅ |
| RF-UC05-5 | Stock materializado em todas as lojas | `CatalogStockIntegrationTests.CriarLojaECriarProduto_*`, `UC05_Principal_*` | ✅ |
| RF-UC05-6 | Categoria nome único | `CatalogServiceTests.CreateCategoria_NomeDuplicado_*` | ✅ |

### UC06 — Gerir Stock

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC06-1 | Alertas: Quantidade < Min | `InventoryServiceTests.ListAlertas_*` (2 testes) | ✅ |
| RF-UC06-2 | UpdateLimits: Min ≥ 0 | `InventoryServiceTests.UpdateLimits_LimitesNegativos_*` | ✅ |
| RF-UC06-3 | UpdateLimits: Max ≥ Min | `InventoryServiceTests.UpdateLimits_MaxMenorQueMin_*` | ✅ |

### UC09 — Gerir Fornecedores

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC09-1 | NIF válido | `ProcurementServiceTests.CreateFornecedor_NifInvalido_*`, `NifValidatorTests.*` | ✅ |
| RF-UC09-2 | NIF único | `ProcurementServiceTests.CreateFornecedor_NifDuplicado_*` | ✅ |
| RF-UC09-3 | Associações com Produtos | `ProcurementServiceTests.CreateFornecedor_HappyPath_*` | ✅ |

### UC10 — Gerir Encomendas

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC10-1 | Linha exige produto associado | `ProcurementServiceTests.CriarEncomenda_ProdutoNaoAssociado_*` | ✅ |
| RF-UC10-2 | >5000€ exige Admin para submeter | `ProcurementServiceTests.CriarEncomenda_SuperiorA5000_*` (2 testes) | ✅ |
| RF-UC10-3 | Fornecedor inativo recusado | `ProcurementServiceTests.CriarEncomenda_FornecedorInativo_*` | ✅ |

### UC11 — Rececionar Encomenda

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC11-1 | Receção total → Recebida | `ProcurementServiceTests.RecepcionarEncomenda_Total_*` | ✅ |
| RF-UC11-2 | Receção parcial → ParcialmenteRecebida | `ProcurementServiceTests.RecepcionarEncomenda_Parcial_*` | ✅ |
| RF-UC11-3 | Stock incrementado pela qty recebida | `ProcurementServiceTests.RecepcionarEncomenda_Total_*` | ✅ |
| RF-UC11-4 | Qty recebida não pode exceder pedida | `ProcurementServiceTests.RecepcionarEncomenda_QuantidadeRecebidaSuperior_*` | ✅ |

### UC12 — Consolidação Diária

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC12-1 | Idempotente (Concluída não re-executa) | `ConsolidationServiceTests.Execute_QuandoJaConcluida_*` | ✅ |
| RF-UC12-2 | KPIs sobre vendas Concluidas | `ConsolidationServiceTests.Execute_ComVendasConcluidas_*`, `Execute_ContaAnuladas_*` | ✅ |
| RF-UC12-3 | Max 3 tentativas | `ConsolidationServiceTests.Execute_AposTresFalhas_*` | ✅ |
| RF-UC12-4 | Sem vendas → KPI vazio mas Concluída | `ConsolidationServiceTests.Execute_SemVendas_*` | ✅ |
| RF-UC12-5 | Trigger imediato via fecho de caixa | `SalesServiceTests.FecharCaixa_ComJustificacao_*`, `SalesIntegrationTests.FluxoCompleto_*` | ✅ |

### UC15 — Autenticar

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC15-1 | Credenciais válidas → tokens | `AuthAccessServiceTests.Login_ComCredenciaisValidas_*`, `UC15_Principal_*` | ✅ |
| RF-UC15-2 | 5 falhas → lockout | `AuthAccessServiceTests.Login_AoQuintaFalha_*`, `AuthIntegrationTests.CincoFalhas_*`, `UC15_Alt2_*` | ✅ |
| RF-UC15-3 | Conta bloqueada não autentica | `AuthAccessServiceTests.Login_ContaBloqueada_*` | ✅ |
| RF-UC15-4 | Conta inativa não autentica | `AuthAccessServiceTests.Login_ContaInativa_*` | ✅ |
| RF-UC15-5 | Refresh rotaciona token | `AuthAccessServiceTests.Refresh_TokenValido_*`, `AuthIntegrationTests.LoginAndRefresh_*` | ✅ |
| RF-UC15-6 | Logout revoga refresh token | `AuthAccessServiceTests.Logout_*` | ✅ |

### UC16 — Administrar Utilizadores

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC16-1 | Operador/Gerente exige loja | `UserManagementServiceTests.Create_OperadorSemLoja_*` | ✅ |
| RF-UC16-2 | Admin não pode ter loja | `UserManagementServiceTests.Create_AdministradorComLoja_*` | ✅ |
| RF-UC16-3 | Email único | `UserManagementServiceTests.Create_EmailDuplicado_*` | ✅ |
| RF-UC16-4 | 1 Gerente ativo por loja | `UserManagementServiceTests.Create_GerenteJaExisteNaLoja_*` | ✅ |
| RF-UC16-5 | Senha temporária gerada | `UserManagementServiceTests.Create_OperadorComLoja_*` | ✅ |

### UC17 — Atualizar Perfil

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC17-1 | Update Me só altera Nome | `AuthAccessServiceTests.UpdateMe_*` | ✅ |
| RF-UC17-2 | ChangePassword exige password atual | `AuthAccessServiceTests.ChangePassword_PasswordAtualErrada_*` | ✅ |
| RF-UC17-3 | Nova password segue policy | `AuthAccessServiceTests.ChangePassword_NovaPasswordFracaRecusada` + `PasswordPolicyTests.*` (8 testes) | ✅ |
| RF-UC17-4 | Nova ≠ atual | `AuthAccessServiceTests.ChangePassword_NovaIgualAtualRecusada` | ✅ |
| RF-UC17-5 | Flag PasswordTemporaria limpa após mudança | `AuthAccessServiceTests.ChangePassword_ComPasswordAtualCerta_*` | ✅ |

### UC19 — Gerir Lojas

| Requisito | Descrição | Testes | Estado |
|-----------|-----------|--------|--------|
| RF-UC19-1 | Designação única | `StoreManagementServiceTests.Create_DesignacaoDuplicada_*` | ✅ |
| RF-UC19-2 | StockMax ≥ StockMin | `StoreManagementServiceTests.Create_StockMaxMenorQueMin_*` | ✅ |
| RF-UC19-3 | Criar loja materializa stock | `StoreManagementServiceTests.Create_HappyPath_*`, `CatalogStockIntegrationTests.CriarLojaECriarProduto_*` | ✅ |

## 2. Requisitos Não-Funcionais (RNF)

| Requisito | Descrição | Tipo de evidência | Testes | Estado |
|-----------|-----------|-------------------|--------|--------|
| RNF-SEC-01 | Password hashed com BCrypt | Code review + interface `IPasswordHasher` | n/a | ✅ |
| RNF-SEC-02 | Login response constant-time (anti enumeration) | Verify chamado mesmo p/ email inexistente | `AuthAccessServiceTests.Login_EmailDesconhecido_*` | ✅ |
| RNF-SEC-03 | 5 tentativas → lockout 15 min | Comportamento | `AuthAccessServiceTests.Login_AoQuintaFalha_*` | ✅ |
| RNF-SEC-04 | JWT 15 min access / 7 dias refresh | Settings injectados | n/a (settings) | 🟡 (testado indirectamente) |
| RNF-SEC-05 | Refresh rotation revoga antigo | Comportamento | `AuthAccessServiceTests.Refresh_*` | ✅ |
| RNF-ACID-01 | Writes em transação | UnitOfWork chamado | Verificado via `FakeUnitOfWork.SaveCallCount` | ✅ |
| RNF-AUDIT-01 | Toda escrita gera entrada de auditoria | `IAuditService.LogAsync` invocado | `FakeAuditService.Contains(...)` em todos os testes de serviço | ✅ |
| RNF-PERF-01 | Numeração sequencial de fatura sob carga | Não testado (carga) | n/a | ❌ |
| RNF-USAB-01 | Frontend POS ≤ 4 toques | Não testado (UX manual) | n/a | ❌ |

> UC07, UC08, UC14, UC18 — fora de âmbito por decisão arquitetural
> (ADR-006/relatório); não constam desta matriz.
