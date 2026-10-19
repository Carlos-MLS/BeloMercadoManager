# Geração de Testes com LLM — Estratégia

> Este projeto está sob a constraint formal de que **todo o código é
> gerado por IA** (CLAUDE.md). A revisão humana garante adequação ao
> domínio. Este documento explica como aplicámos a geração assistida
> por LLM (Claude Code) à fase de testes da Etapa 4.

## 1. Princípios

1. **O LLM não substitui o engenheiro — alavanca-o.** Cada teste
   gerado é lido, validado contra o relatório (SRS), e ajustado se
   estiver fora do contexto do projeto.
2. **Testes são especificação.** Pedimos ao LLM para escrever testes
   que documentem *intenção de regra*, não testes que decoram
   *implementação atual*. Em particular, evitamos testes que
   verifiquem detalhes internos (cadeia de chamadas, ordem de calls)
   quando podemos verificar estado observável.
3. **Cobertura de fronteira é prioritária.** Pedimos partições de
   equivalência e valores de fronteira ao LLM em vez de "alguns
   exemplos felizes".

## 2. Técnicas aplicadas

### 2.1 Requisito-driven (UC-driven)

Para cada caso de uso do Capítulo 3 do relatório, gerámos:
- 1 teste do **fluxo principal** (happy path).
- N testes dos **fluxos alternativos** documentados.
- Os testes de aceitação têm comentários Given/When/Then em prosa para
  serem legíveis ao docente/revisor académico.

Prompt-tipo:

> "Aqui está o UC15 do relatório (autenticação). Gera testes de
> aceitação xUnit (Given/When/Then) que validem cada um dos 4 fluxos
> alternativos documentados. Não inventes regras: se algo é ambíguo,
> deixa um comentário a marcar a ambiguidade."

### 2.2 Equivalence partitioning + Boundary Value Analysis

Aplicado especialmente a:
* **PasswordPolicy** (UC17): comprimento (< 8 / = 8 / > 8), classes em
  falta (uma a uma), null/empty/whitespace.
* **NifValidator** (UC01/UC09): comprimento, dígitos vs letras, check
  digit correto/errado, NIFs de teste conhecidos.
* **Ean13Validator** (UC05): mesma estrutura — incluindo o caso curioso
  `"0000000000000"` que **é matematicamente válido** (descoberto via
  cálculo do check digit pelo LLM).
* **Caixa diferença 10 €** (UC04): diferença = 5 €, = 10 €, = 11 €,
  com/sem justificação.
* **Lockout 5 tentativas** (UC15): falhar 4, falhar 5, tentar 6ª com
  password certa.

### 2.3 State-machine coverage

Para entidades com máquina de estados, geramos testes para cada
transição válida + inválidas:

```
CaixaDiaria:  Aberta ──fechar──> Fechada  (✅ testado)
              Aberta ──abrir──> Aberta? (✅ rejeita - UC04 alt2)
              Fechada ──fechar──> Fechada? (✅ rejeita)

Venda:        Concluida ──anular──> Anulada (✅)
              Concluida ──devolução──> Concluida (✅ mantém estado)
              Anulada ──anular──> Anulada? (✅ rejeita)

Encomenda:    Rascunho ──submeter──> Submetida (✅)
              Rascunho ──submeter (>5000€) sem admin──> Forbidden (✅)
              Submetida ──rececionar parcial──> ParcialmenteRecebida (✅)
              Submetida ──rececionar total──> Recebida (✅)
              ParcialmenteRecebida ──rececionar resto──> Recebida (parcial via testar 2x)
              Recebida ──rececionar──> Conflict (não testado explicitamente — ✅ via cobertura indireta)
```

### 2.4 Edge cases "óbvios para o LLM" (descobertas surpreendentes)

Casos que o LLM levantou por iniciativa, e que validámos como
relevantes:

* **Anti-enumeration timing-safe verify** (UC15): o LLM lembrou que
  precisamos invocar `BCrypt.Verify` mesmo quando o utilizador é
  desconhecido, para evitar timing-side-channel. Tornou-se o teste
  `Login_EmailDesconhecido_DevolveNull_SemRevelarExistencia_EChamaVerifyParaTimingConstant`.
* **NIF `000000000` matematicamente válido**: ao computar o algoritmo
  mod-11, vimos que `0 * pesos = 0`, `0 mod 11 = 0`, expected = `0` →
  o validador aceita. O LLM começou por usar este valor como "NIF
  inválido óbvio"; o teste falhou e a investigação revelou que era
  mesmo válido. Trocámos por `"123456788"` (check digit errado em 1).
* **Refresh token replay**: testar que um token revogado não pode ser
  reutilizado nem renovado — caso clássico de OAuth replay.
* **Cliente NIF case**: NIF de cliente que existe → reutiliza o
  Cliente; NIF novo → cria novo. Testado em
  `SalesServiceTests.CriarVenda_HappyPath`.
* **Promoção com valor que torna preço ≤ 0**: a implementação faz
  clamp em 0 (não cobra negativo). O LLM sugeriu testar; ainda não
  está coberto, marcado como gap em [cobertura.md](cobertura.md).

### 2.5 Property-based / parametrizado

Usamos `[Theory] + [InlineData]` para múltiplos exemplos:

```csharp
[Theory]
[InlineData("Abc123!@")]          // 8 chars, todas as classes
[InlineData("Password1!")]
[InlineData("XyZ9-aBcd")]
[InlineData("Lorem1Ipsum$Dolor")]
public void IsValid_ReturnsTrue_ForCompliantPasswords(string p) { ... }
```

Não usámos uma biblioteca property-based (FsCheck) por simplicidade
académica, mas o padrão é equivalente.

## 3. Catálogo de prompts úteis

### 3.1 Para gerar testes a partir de uma classe

```
Aqui está o serviço X.cs (cola código).
Lista, em formato de tabela, todas as regras de negócio que detectas,
com:
1. nome curto da regra
2. caminho de código que a implementa (método + linhas)
3. um nome de teste sugerido (Verb_Cenario_Expectativa)
4. tipo de teste (happy path / erro / boundary / state transition)

Depois, escreve os testes xUnit em FluentAssertions, usando fakes
disponíveis em tests/.../TestDoubles/.
```

### 3.2 Para encontrar edge cases

```
Olha para o método Y. Pensa em pelo menos 5 inputs/sequências que
um humano experiente em PT-pt poderia esquecer:
- valores negativos / zero / max
- whitespace / unicode
- ordens não-canónicas
- concorrência (mesmo objecto modificado por 2 chamadas)
- timing (clock muda durante a operação)
Para cada um, escreve um teste que falharia se o método não tratasse
o caso.
```

### 3.3 Para escrever testes Given/When/Then

```
Lê o UC0X do docs/relatorio/relatorio.pdf. Quero 1 teste por cenário
(principal + cada alternativo). Cada teste tem:
- comentário XML <summary> com Given/When/Then em prosa
- nome NomeUC_Cenario_NomeDoCriterioObservavel
- código que usa a IntegrationTestFixture
- assertions sobre o estado da BD via f.Db.*
```

## 4. Limitações

* **Confiança e revisão**: cada teste foi corrido localmente. Quando
  uma assertion falha, investigamos a regra real no SRS antes de
  ajustar — o LLM por vezes "inventa" um threshold (e.g. lockout de
  10 min em vez de 15) e foi preciso fixar.
* **Testes flaky**: detetámos um caso onde `OrderByDescending(.DataMudanca)`
  podia devolver qualquer das duas entradas com timestamp igual.
  Corrigido para `Where(...).Should().Contain(2.50m)`.
* **Lookup de erros**: o LLM por vezes assume nomes de error codes
  (e.g. `MetodoPagamento.Multibanco`) sem ler o enum real. Mitigado
  por re-corrida do `dotnet test` após cada bloco gerado.

## 5. Métricas de geração

| Métrica                        | Valor |
|--------------------------------|------:|
| Testes totais escritos         | 165   |
| Testes gerados em primeira ronda LLM | ~138 |
| Testes ajustados depois (typos / API real / regras erradas) | ~21 |
| Testes adicionados manualmente após inspeção | ~6 |
| Tempo total (geração + revisão) | ~3 h |
| Tempo equivalente sem LLM (estimativa do autor) | ~12-15 h |

A relação de produtividade observada (~4×) é consistente com outros
trabalhos académicos sobre LLM-assisted test generation, e está
explicitamente alinhada com a constraint deste projeto de "todo o
código é AI-gerado".
