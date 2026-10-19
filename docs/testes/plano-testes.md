# Plano de Testes — Belo Mercado Manager

## 1. Objetivos

Garantir que a implementação do Belo Mercado Manager satisfaz o conjunto
de requisitos funcionais e não-funcionais documentados nos Capítulos 3
(Modelação de Negócio + Casos de Uso) e 4 (Arquitetura) do relatório,
através de uma suite de testes automáticos executável por qualquer
membro da equipa em segundos.

São objetivos secundários:
* Permitir refactoring seguro durante a fase 5 (consolidação) do projeto.
* Servir de evidência de qualidade para a defesa final (Etapa 4 do plano).
* Documentar regras de negócio implícitas através de testes legíveis
  (specification by example).

## 2. Níveis de teste

| Nível        | Âmbito                                       | Tooling                            | Localização |
|--------------|----------------------------------------------|------------------------------------|-------------|
| Unitário     | classes isoladas; deps via fakes/in-memory   | xUnit + FluentAssertions + Moq     | `tests/BeloMercado.UnitTests` |
| Integração   | Service ↔ Repository ↔ DbContext (EF Core)   | EF Core InMemory + repos reais     | `tests/BeloMercado.IntegrationTests` |
| Sistema /    | fluxo end-to-end multi-UC (várias operações) | mesmas ferramentas, fixture pesada | `tests/BeloMercado.IntegrationTests` (cenários compostos) |
| Aceitação    | critério de aceitação Given/When/Then por UC | mesmas ferramentas, prosa do UC    | `tests/BeloMercado.AcceptanceTests` |

> **Nota sobre o nível "Sistema":** numa aplicação Blazor Server + API
> REST, o teste de sistema ideal correria o servidor com
> `WebApplicationFactory<Program>` e simularia chamadas HTTP. Para o
> âmbito académico (1 semana, prototipagem) optámos pela mesma fixture
> dos testes de integração, encadeando múltiplos serviços através do
> mesmo DbContext — equivalente em termos de cobertura de regras de
> negócio, sem a latência do TestServer.

## 3. Stack de testes

* **xUnit 2.9** — framework de testes.
* **FluentAssertions 6.12** — assertivas legíveis.
* **Moq 4.20** — disponível para testes que precisam de mocks em vez de
  fakes (atualmente preferimos fakes/in-memory repos).
* **Microsoft.EntityFrameworkCore.InMemory 8.0.10** — provider InMemory
  para EF Core nos testes de integração.
* **coverlet.collector / coverlet.msbuild 6.0.2** — cobertura de código.
* **Microsoft.NET.Test.Sdk 17.11.1** — runner standard de `dotnet test`.

## 4. Convenções

### 4.1 Nomenclatura de testes

```
[Método]_[Cenário]_[Expectativa]
```

Exemplos:
* `Login_ComCredenciaisValidas_DevolveTokensEAtualizaUltimoLogin`
* `CriarVenda_StockInsuficiente_Rejeita`
* `UC01_Alt3_RetryComMesmaIdempotencyKey_NaoDuplica`

### 4.2 Estrutura AAA (Arrange/Act/Assert)

Cada teste tem três blocos visualmente distintos. Para testes de
aceitação usamos a linguagem Given/When/Then no comentário XML do
método para reforçar a leitura ao público não-técnico.

### 4.3 Test doubles

* **Fakes** (preferido): objetos com lógica simplificada e estado
  observável. Estão em `tests/BeloMercado.UnitTests/TestDoubles/`.
  Exemplos: `InMemoryUserRepository`, `FakeClock`, `FakeAuditService`.
* **Mocks** (raro): só quando precisamos de verificar interações com
  uma dependência sem outro estado relevante.
* **Stubs reais via EF InMemory**: para integração, usamos os
  repositórios concretos da Infrastructure ligados ao DbContext
  InMemory.

## 5. Estratégia de geração de testes (resumo)

Os testes deste projeto foram **escritos com o auxílio de um LLM**
(Claude Code) seguindo a constraint global do projeto (todo o código é
gerado por IA, sob revisão humana). As técnicas aplicadas estão
detalhadas em [llm-test-generation.md](llm-test-generation.md). Em
síntese:

1. **Requisito-driven**: cada UC do relatório alimentou pelo menos um
   teste de aceitação correspondente (ver [matriz-srs.md](matriz-srs.md)).
2. **Equivalence partitioning + boundary value**: aplicado aos
   validators (NIF, EAN-13, password policy) e a fronteiras numéricas
   (5 falhas → bloqueio; 10 € diferença → justificação).
3. **Edge cases sugeridos pelo LLM**: timing-safe verify para emails
   inexistentes, idempotência de retry, NIF "000000000" (caso curioso
   onde o check digit do mod-11 dá zero → matematicamente válido,
   contra-intuitivo, descoberto pelo gerador).
4. **State-machine coverage**: estados de Caixa (Aberta→Fechada), Venda
   (Concluida→Anulada / Devolvida), Encomenda
   (Rascunho→Submetida→Recebida).

## 6. Critérios de saída (exit criteria)

* **Todos os testes verdes** em `dotnet test BeloMercadoManager.sln`.
* **Cobertura de linhas** dos serviços críticos (Auth, Sales, Catalog,
  Procurement, Consolidation) ≥ 70 %. Ver [cobertura.md](cobertura.md).
* **Regras críticas de negócio cobertas**: matriz em
  [matriz-srs.md](matriz-srs.md) sem células marcadas como ❌.
* **Build sem warnings** nos projetos de teste.

## 7. Como executar

```bash
# Toda a suite
dotnet test BeloMercadoManager.sln

# Por projeto
dotnet test tests/BeloMercado.UnitTests
dotnet test tests/BeloMercado.IntegrationTests
dotnet test tests/BeloMercado.AcceptanceTests

# Com runsettings (cobertura ativada)
dotnet test BeloMercadoManager.sln --settings tests/coverage.runsettings

# Filtrar por padrão
dotnet test --filter "FullyQualifiedName~UC01"
dotnet test --filter "FullyQualifiedName~SalesServiceTests"
```

## 8. Limitações conhecidas

* **EF Core InMemory** não simula:
  - Constraints únicas reforçadas pelo SQL (verificamos por código).
  - Cascading deletes do SQL Server.
  - Transações distribuídas (apenas SaveChanges atómico do contexto).
* **Não testamos**: rate-limiting, autenticação JWT real (assinaturas
  RS/HS256), conectividade com SQL Server, Hosted Services em background.
* **Aceitação pelo utilizador**: a validação final pelo cliente
  académico (docente) faz-se pela execução manual conforme o capítulo
  3 do relatório, complementando a suite automatizada.
