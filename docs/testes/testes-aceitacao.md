# Testes de Aceitação — Belo Mercado Manager

Cenários Given/When/Then por caso de uso. Cada cenário tem um teste
correspondente em `tests/BeloMercado.AcceptanceTests/` que valida o
critério.

---

## UC01 — Registar Venda

### Cenário principal: venda em numerário com troco

**Given** a loja Braga tem um produto "Sumo" (2 €, IVA 23 %) com 100
unidades em stock e o operador abriu hoje a sua caixa diária com fundo
de 50 €.

**When** o operador finaliza uma venda de 3 unidades, com o cliente a
entregar 10 € em numerário.

**Then**
- a venda fica em estado *Concluída*;
- é emitida fatura com número sequencial `FT YYYY/00001-XXXX`;
- o troco devolvido é de 4 €;
- o stock desce para 97 unidades;
- é registado um log de auditoria `VENDA_FINALIZADA`.

**Teste:** `UC01_RegistarVendaTests.UC01_Principal_VendaEmNumerarioComTroco_FinalizaEDecrementaStock`

### Cenário alternativo 1: sem caixa aberta

**Given** a loja não tem nenhuma `CaixaDiaria` aberta para o dia atual.

**When** o operador tenta finalizar uma venda.

**Then** o sistema recusa com código `CAIXA_NAO_ABERTA` e nenhuma venda
é criada na base de dados.

**Teste:** `UC01_RegistarVendaTests.UC01_Alt1_SemCaixaAberta_RejeitaSemCriarVenda`

### Cenário alternativo 2: stock insuficiente

**Given** o produto tem apenas 2 unidades em stock.

**When** o operador tenta vender 5 unidades.

**Then** o sistema recusa com código `STOCK_INSUFICIENTE`, o stock
mantém-se nas 2 unidades originais e nenhuma venda é criada.

**Teste:** `UC01_RegistarVendaTests.UC01_Alt2_StockInsuficiente_RejeitaSemDecrementar`

### Cenário alternativo 3: retry com mesma idempotency-key

**Given** o operador já submeteu uma venda com `Idempotency-Key: same`.

**When** por falha de rede, o cliente POS reenvia a mesma requisição
com a mesma key.

**Then** o sistema devolve a venda original (mesmo ID), não duplica
linhas nem decrementa stock duas vezes.

**Teste:** `UC01_RegistarVendaTests.UC01_Alt3_RetryComMesmaIdempotencyKey_NaoDuplica`

---

## UC02 — Anular / Devolver Venda

### Cenário principal: anular venda recente

**Given** uma venda foi finalizada há minutos com 2 unidades de stock
decrementado.

**When** o mesmo operador anula a venda.

**Then** a venda passa a estado *Anulada*, o stock é reposto e gera-se
log `VENDA_ANULADA`.

**Teste:** `UC02_AnularEDevolverTests.UC02_Principal_AnularRecente_*`

### Cenário alternativo 1: anular venda antiga sem perfil

**Given** uma venda existe há mais de 24 h.

**When** um operador (não-gerente) tenta anular.

**Then** o sistema recusa com código `APPROVAL_REQUIRED`. Apenas o
Gerente de Loja (ou Admin da Cadeia) pode concluir esta operação.

**Teste:** `UC02_AnularEDevolverTests.UC02_Alt1_Anular24hPorOperador_RejeitaComApprovalRequired`

### Cenário alternativo 2: devolução parcial

**Given** uma venda com 3 unidades, *Concluída*.

**When** o operador devolve 1 unidade.

**Then** a `LinhaVenda` regista `QuantidadeDevolvida = 1`; o stock
incrementa em 1 unidade; a venda mantém-se *Concluída*.

**Teste:** `UC02_AnularEDevolverTests.UC02_DevolucaoParcial_*`

---

## UC04 — Gerir Caixa Diária

### Cenário principal: abrir e fechar caixa sem diferenças

**Given** existe uma loja e o operador acaba de iniciar o turno.

**When** ele abre a caixa com 50 € de fundo e, no fim do dia, conta
exatamente 50 € no fecho.

**Then** a caixa fica *Fechada*, a consolidação do dia é executada
imediatamente (estado *Concluída*) e os KPIs do dia ficam disponíveis.

**Teste:** `UC04_GerirCaixaTests.UC04_Principal_AbrirEFecharCaixa_*`

### Cenário alternativo 1: diferença > 10 € sem justificação

**Given** caixa aberta com fundo 50 €.

**When** o operador tenta fechar com `ValorContado = 100` e sem
preencher o campo `Justificacao`.

**Then** o sistema recusa com código
`CAIXA_JUSTIFICACAO_OBRIGATORIA` e a caixa permanece *Aberta*.

**Teste:** `UC04_GerirCaixaTests.UC04_Alt1_DiferencaSuperiorA10SemJustificacao_Rejeita`

### Cenário alternativo 2: duas caixas no mesmo dia

**Given** uma caixa já está aberta hoje para a loja.

**When** tenta-se abrir uma segunda caixa.

**Then** o sistema recusa com código `CAIXA_JA_ABERTA` (RN: 1 caixa
por loja/dia).

**Teste:** `UC04_GerirCaixaTests.UC04_Alt2_TentarAbrirSegundaCaixaNoMesmoDia_Rejeita`

---

## UC05 — Gerir Catálogo

### Cenário principal: criar produto válido

**Given** existe uma loja "L" e a categoria "X".

**When** o admin cria um produto com EAN-13 `5601234567892`, preço
1.99 €, IVA reduzida.

**Then** o produto fica criado, é gerada uma entrada em
`HistoricoPreco` e uma linha de `Stock` em "L".

**Teste:** `UC05_GerirCatalogoTests.UC05_Principal_CriarProdutoValido_GeraHistoricoEStock`

### Cenário alternativo 1: EAN-13 inválido

**Given** uma categoria existente.

**When** o admin tenta criar produto com EAN `1111111111111` (check
digit incorreto).

**Then** o sistema recusa com código `PRODUTO_EAN13_INVALIDO`.

**Teste:** `UC05_GerirCatalogoTests.UC05_Alt1_Ean13Invalido_Rejeita`

### Cenário alternativo 2: alteração de preço

**Given** um produto criado com preço 1 €.

**When** o admin altera o preço para 1.50 €.

**Then** uma nova entrada de `HistoricoPreco` é adicionada (passam a
ser 2 entradas: criação + alteração).

**Teste:** `UC05_GerirCatalogoTests.UC05_Alt2_AlterarPreco_AdicionaEntradaAoHistorico`

---

## UC15 — Autenticar Utilizador

### Cenário principal: login com credenciais válidas

**Given** existe um Operador `op@belomercado.pt` com password
`Password1!`.

**When** ele submete login com essas credenciais.

**Then** recebe `AccessToken` e `RefreshToken`; o `Perfil` devolvido
é `OperadorCaixa`.

**Teste:** `UC15_AutenticarTests.UC15_Principal_LoginComCredenciaisValidas_DevolveTokens`

### Cenário alternativo 1: anti user-enumeration

**Given** existe `op@belomercado.pt` na base de dados;
`naoexiste@belomercado.pt` não existe.

**When** um atacante tenta login com password errada em ambos.

**Then** ambos devolvem `null` sem distinção visível (mesmo
comportamento, mesma mensagem genérica, mesmo tempo aproximado de
resposta — implementado via verify sempre executado).

**Teste:** `UC15_AutenticarTests.UC15_Alt1_PasswordErrada_DevolveNullSemRevelarSeOEmailExiste`

### Cenário alternativo 2: lockout após 5 falhas

**Given** um utilizador ativo.

**When** ele falha o login 5 vezes consecutivas e depois tenta com a
password correta.

**Then** mesmo a sexta tentativa (com a password correta) é recusada;
`BloqueadoAte` está preenchido com `agora + 15 min`.

**Teste:** `UC15_AutenticarTests.UC15_Alt2_CincoFalhas_BloqueiamConta_*`

### Refresh: rotação

**Given** o operador acabou de fazer login e tem um refresh token
ativo.

**When** ele invoca `/auth/refresh` com esse token.

**Then** recebe um novo par (access + refresh); o token antigo fica
com `RevokedAt` preenchido; permanece exatamente 1 token ativo.

**Teste:** `UC15_AutenticarTests.UC15_Refresh_RotacionaTokenERevogaAntigo`

---

## Notas

* Os testes acima reproduzem a estrutura de "Fluxo Principal / Fluxos
  Alternativos" usada nos casos de uso do Capítulo 3 do relatório.
* Cada teste é executável standalone e completa em < 1 segundo.
* Critérios não-funcionais (ex.: tempo de resposta < 200 ms,
  acessibilidade) **não** estão cobertos por testes automatizados —
  são validados pela equipa em execução manual sobre a aplicação real.
