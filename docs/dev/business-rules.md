# Regras de Negócio (RNs) por UC

Resumo das regras de negócio por caso de uso, na forma como ficaram
implementadas. As RNs canónicas estão no relatório §3.7 e Anexo B —
este ficheiro é só uma cábula rápida.

## UC01 — Registar Venda

| RN | Implementação |
|----|---------------|
| RN01 EAN-13 válido (mod-10) | `Ean13Validator.IsValid(...)` no `CatalogService.CreateProdutoAsync` e quando se procura produto na venda. |
| RN02 Venda exige caixa aberta | `SalesService.CriarVendaAsync` falha com `CAIXA_NAO_ABERTA` se não houver `CaixaDiaria.Estado=Aberta` para `(loja, hoje)`. |
| RN03 IVA incluído | `valorIva = subtotal * (taxa / (100m + taxa))`. Aplica-se por linha. |
| RN04 Numeração sequencial de fatura | `MAX(seq)+1 per (LojaId, Ano)` com unique constraint. Race race-condition: o catch sobre constraint volta a calcular. |
| RN05 NIF cliente válido (mod-11) | `NifValidator.IsValid(...)` em `CriarVendaAsync` antes de fazer upsert em `Clientes`. |
| RN06 Idempotência | Chave `(LojaId, IdempotencyKey)` em `IdempotencyEntries`. Reenvio devolve a resposta cacheada **sem reprocessar**. Stock decrementa **uma só vez**. |
| RN07 Stock decrementa atomicamente | Validate-then-decrement por linha; se falhar uma, rollback transacional. |

## UC02 — Anular Venda + Devolução

| RN | Implementação |
|----|---------------|
| RN01 Janela de 24h para Operador | `AnularVendaAsync` compara `DateTime.UtcNow - venda.DataHora`. Se >24h e role = `OperadorCaixa`, `errorCode=PRAZO_ANULACAO_EXCEDIDO`. Gerente/Admin sem limite. |
| RN02 Reverter stock só do não-devolvido | Para cada linha: `stockDelta = Quantidade - QuantidadeDevolvida`. |
| RN03 Devolução cumulativa | `DevolucaoAsync` valida `qtyPedida ≤ Quantidade - QuantidadeDevolvida`; incrementa `QuantidadeDevolvida` em vez de substituir. |
| RN04 Devolução não anula a venda | Estado mantém-se `Concluida`; só anula explicitamente é que vai para `Anulada`. |
| RN05 Motivo obrigatório no anular | Validation em DTO + service. |

## UC03 — Promoções

| RN | Implementação |
|----|---------------|
| RN01 Validade temporal | `DataInicio ≤ hoje ≤ DataFim` no momento de aplicar. |
| RN02 Precedência loja-específica > global | `IPromocaoRepository.ListActivesByProdutoAsync` ordena por `LojaId IS NULL ASC` (não-null primeiro), depois por `DataCriacao DESC`. |
| RN03 Tipos disjuntos | Enum `TipoPromocao { Percentagem=1, PrecoFixo=2 }`. Service valida `Valor` consoante tipo (percentagem ∈ [0.01, 100], preço ∈ [0.01, 99999.99]). |
| RN04 Soft delete | `Ativo=false` em vez de DELETE. |

## UC04 — Caixa Diária

| RN | Implementação |
|----|---------------|
| RN01 Uma caixa aberta por dia per loja | Unique constraint `(LojaId, Data) WHERE Estado='Aberta'` + check no service. |
| RN02 Diferença >10€ exige justificação | `if (Math.Abs(Diferenca) > 10m && string.IsNullOrEmpty(Justificacao))` falha com `CAIXA_JUSTIFICACAO_OBRIGATORIA`. |
| RN03 Valor esperado | `FundoInicial + SUM(Vendas em Numerario do dia)`. |
| RN04 Hook de consolidação | `FecharCaixaAsync` chama `ConsolidationService.ProcessAsync(loja, data)` no fim. |

## UC05 — Catálogo

| RN | Implementação |
|----|---------------|
| RN01 EAN único | Unique constraint em `Produtos.Ean13`. Service captura e devolve `EAN_DUPLICADO`. |
| RN02 EAN imutável | `UpdateProdutoRequest` não tem campo `Ean13`. |
| RN03 Histórico de preço | Sempre que `UpdateProdutoAsync` deteta mudança de `Preco`, insere uma linha em `HistoricoPreco`. |
| RN04 Soft delete | `Ativo=false`. Produtos inativos não aparecem nas listagens default (filtrável). |

## UC06 — Stock

| RN | Implementação |
|----|---------------|
| RN01 Stock auto-materializa | Linhas qty=0 são criadas: na criação de loja (uma por produto existente) e na criação de produto (uma por loja). |
| RN02 Gerente scoped à própria loja | `[Authorize(Roles="GerenteLoja,AdminCadeia")]` + leitura de `JwtClaimNames.LojaId` no controller para passar `scopeLojaId` ao service. |
| RN03 Alertas | Linha está "abaixo do mínimo" quando `Quantidade < QtdMinima`. Endpoint dedicado `/stock/alertas` filtra. |

## UC09 — Fornecedores

| RN | Implementação |
|----|---------------|
| RN01 NIF válido (mod-11) | `NifValidator.IsValid` no `CreateAsync`. |
| RN02 NIF imutável | `UpdateFornecedorRequest` não tem campo `Nif`. |
| RN03 Soft delete | `Ativo=false`. |
| RN04 Substituir associações | `UpdateAsync` substitui o conjunto inteiro de `ProdutosFornecedores` em vez de patch. |

## UC10 — Encomenda a Fornecedor

| RN | Implementação |
|----|---------------|
| RN01 Estados | Enum `EstadoEncomenda { Rascunho, Submetida, ParcialmenteRecebida, Recebida, Cancelada }`. Transições válidas no service. |
| RN02 Total com desconto da associação | `total = Σ (precoCusto * qty * (1 - desconto/100))` no `CriarEncomendaAsync`. |
| RN03 Aprovação para >5000€ | `SubmeterEncomendaAsync`: se `total > 5000m` e role do actor não é Admin → `OperationStatus.Forbidden` com `errorCode=APPROVAL_REQUIRED`. |
| RN04 Linhas só de produtos associados | Valida que cada `ProdutoId` do request está em `Fornecedor.ProdutosAssociados`. |

## UC11 — Receção de Mercadoria

| RN | Implementação |
|----|---------------|
| RN01 Receção parcial permitida | `RececionarEncomendaAsync` aceita qty < `Quantidade` por linha; estado vai a `ParcialmenteRecebida`. |
| RN02 Stock incrementa | Para cada linha rececionada: `stock.Quantidade += qty`. |
| RN03 Não exceder pendente | Valida `qty ≤ Quantidade - QuantidadeRecebida`; senão `errorCode=QUANTIDADE_EXCEDE_PENDENTE`. |
| RN04 Estado final | `Recebida` quando todas as linhas estão completas. |

## UC12 — Consolidação Diária

| RN | Implementação |
|----|---------------|
| RN01 Hook imediato no fecho de caixa | `FecharCaixaAsync` chama `ConsolidationService.ProcessAsync` antes de devolver. |
| RN02 Retry assíncrono | `ConsolidationHostedService` (BackgroundService) faz polling a cada 30 min. Procura `ConsolidacaoDiaria` em `Pendente | Falha`, com `Tentativas < 3`. |
| RN03 Idempotente | `ProcessAsync` faz upsert (não insert duplicate) em `KPIDiarios` e `RankingProdutoDiarios`. Reprocessar não quebra. |

## UC13 — Dashboard de KPIs

| RN | Implementação |
|----|---------------|
| RN01 Gerente preso à própria loja | `DashboardsController` resolve `scopeLojaId` no controller; o service não conhece o JWT. |
| RN02 Top-10 por receita líquida | `RankingProdutoDiario` é precalculado na consolidação ordenado por `Receita DESC`, top 10. Página apenas lê. |
| RN03 Janela default | Front-end default = últimos 7 dias; backend aceita qualquer intervalo. |

## UC15 — Autenticar

| RN | Implementação |
|----|---------------|
| RN01 BCrypt cost 11 | `BCryptPasswordHasher` com `WorkFactor = 11`. |
| RN02 Sem enumeração | Login devolve **sempre 401 com mesmo body** quer email não exista, quer password esteja errada. Mais: `LoginAsync` corre BCrypt verify contra um hash dummy mesmo quando o user não existe — mitigação CWE-203 (timing attack). |
| RN03 Lockout 5 tentativas | Contador in-memory por email; ao 5º falha bloqueia 15 min. |
| RN04 JWT 15 min + refresh 7d | Configurado em `JwtTokenGenerator`. |
| RN05 Refresh rotativo | Refresh devolve novo access + novo refresh, marca o antigo como `Revogado`. Replay de refresh já usado revoga toda a família. |

## UC16 — Gerir Utilizadores

| RN | Implementação |
|----|---------------|
| RN01 Email único | Unique constraint + `EMAIL_EXISTS`. |
| RN02 Operador/Gerente requerem loja | `if (perfil ∈ {OperadorCaixa, GerenteLoja} && lojaId == null)` → `PERFIL_REQUER_LOJA`. |
| RN03 Password temporária no create | `RandomPasswordGenerator` (12 chars, 4-de-4) devolvida na resposta com `PasswordTemporaria=true`. |
| RN04 Soft delete | `DELETE` faz `Ativo=false`. |

## UC17 — Editar Perfil Próprio

| RN | Implementação |
|----|---------------|
| RN01 Email/Perfil read-only | `UpdateMeRequest` só tem `Nome`. |
| RN02 Password policy 8+ 4-de-4 | `PasswordPolicy.Validate(...)` exige minúscula + maiúscula + dígito + símbolo + length ≥ 8. |
| RN03 Password atual obrigatória | `ChangePasswordAsync` faz BCrypt verify antes. |
| RN04 Password diferente da atual | Bloqueia "trocar pela mesma". |

## UC19 — Gerir Lojas

| RN | Implementação |
|----|---------------|
| RN01 Designação única | Unique constraint. |
| RN02 StockMax ≥ StockMin | CHECK constraint + validação no service. |
| RN03 Soft delete | Sem DELETE; `UpdateAsync(... Ativo=false)`. |
| RN04 Stock auto-materializa em produtos existentes | Hook em `CreateAsync` (UC15 nota arquitetural). |

## Códigos de erro mais comuns

| Código | Origem | Mensagem PT no front-end |
|--------|--------|--------------------------|
| `AUTH_INVALID_CREDENTIALS` | login | "Email ou password inválidos." |
| `AUTH_LOCKED` | login após 5 falhas | "Conta bloqueada. Tenta dentro de 15 min." |
| `EAN_INVALIDO` | catálogo | "EAN-13 inválido." |
| `EAN_DUPLICADO` | catálogo | "Já existe um produto com este EAN." |
| `NIF_INVALIDO` | clientes/fornecedores | "NIF inválido." |
| `CAIXA_JA_ABERTA_HOJE` | abrir caixa | "Já existe uma caixa aberta hoje." |
| `CAIXA_JUSTIFICACAO_OBRIGATORIA` | fechar caixa | "Diferença >10€ exige justificação." |
| `STOCK_INSUFICIENTE` | venda | "Stock insuficiente para esse produto." |
| `IDEMPOTENCY_KEY_OBRIGATORIA` | criar venda | "Falta o header Idempotency-Key." |
| `PRAZO_ANULACAO_EXCEDIDO` | anular venda | "Operadores só podem anular nas primeiras 24h." |
| `VENDA_JA_ANULADA` | anular/devolver | "Esta venda já estava anulada." |
| `APPROVAL_REQUIRED` | submeter encomenda | "Encomenda excede 5000€ — só Admin pode submeter." |
| `PASSWORD_FRACA` | mudar password | "Não cumpre requisitos (8+ caracteres, 4-de-4)." |
| `PASSWORD_INVALIDA` | mudar password | "Password atual errada." |

Catálogo completo em `BeloMercado.Application/Common/Constants/*ErrorCodes.cs`.
