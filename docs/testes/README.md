# Belo Mercado Manager — Etapa 4: Testes e Qualidade

Esta pasta agrupa toda a documentação relativa à fase de testes
automáticos, satisfação dos requisitos (SRS) e qualidade do produto
(ISO/IEC 25010) do projeto Belo Mercado Manager.

## Índice

1. [Plano de testes](plano-testes.md) — estratégia, níveis, ferramentas
   e como executar.
2. [Matriz SRS ↔ UC ↔ testes](matriz-srs.md) — rastreabilidade entre
   cada requisito (funcional e não-funcional), o caso de uso que o
   instancia e os testes automatizados que o cobrem.
3. [Testes de aceitação](testes-aceitacao.md) — cenários Given/When/Then
   por caso de uso, com o critério de aceitação.
4. [Cobertura de código](cobertura.md) — como gerar relatórios com
   Coverlet/ReportGenerator e o que está coberto/não coberto.
5. [Avaliação ISO/IEC 25010](iso25010.md) — análise por característica de
   qualidade, com evidências e atributos avaliados.
6. [Geração de testes com LLM](llm-test-generation.md) — estratégia,
   prompts-tipo, técnicas (equivalence partitioning, boundary value,
   property-based, requisito-driven) e quotas de testes gerados.

## Resumo executivo

| Tipo                | Projeto                              | # Testes |
|---------------------|--------------------------------------|----------|
| Unitário            | `tests/BeloMercado.UnitTests`        | **142**  |
| Integração          | `tests/BeloMercado.IntegrationTests` | **6**    |
| Aceitação           | `tests/BeloMercado.AcceptanceTests`  | **17**   |
| **Total**           |                                      | **165**  |

Suite atual: **0 failed / 165 passed**, ~3 s a correr toda a solução.

## Como executar

Pré-requisito: .NET 8 SDK (ou .NET 9 SDK com runtime 8 instalado).

```bash
# Todos os testes
dotnet test BeloMercadoManager.sln

# Apenas unitários (muito rápido — ~0.1s)
dotnet test tests/BeloMercado.UnitTests/BeloMercado.UnitTests.csproj

# Com cobertura
dotnet test BeloMercadoManager.sln --settings tests/coverage.runsettings
```

Ver [cobertura.md](cobertura.md) para gerar o relatório HTML.
