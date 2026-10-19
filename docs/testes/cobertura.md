# Cobertura de Código

## Stack

* **coverlet.collector** (DataCollector via `dotnet test --collect:"XPlat Code Coverage"`)
* **coverlet.msbuild** (alternativa via MSBuild para produção determinística de cobertura)
* **ReportGenerator** (opcional — gera HTML a partir do XML cobertura)

A configuração do collector está em [`tests/coverage.runsettings`](../../tests/coverage.runsettings).
Excluímos da cobertura:
- Os próprios projetos de teste.
- `BeloMercado.Api.Program` (apenas wiring).
- `*Migrations*` (geradas pelo EF Core).
- Atributos `[GeneratedCode]`, `[CompilerGenerated]`, `[Obsolete]`.

## Como gerar localmente

### 1. Recolher cobertura

```bash
dotnet test BeloMercadoManager.sln \
    --settings tests/coverage.runsettings \
    --collect:"XPlat Code Coverage"
```

O ficheiro `coverage.cobertura.xml` é gerado em
`tests/<Projecto>/TestResults/<GUID>/coverage.cobertura.xml`.

### 2. Relatório HTML (opcional)

```bash
# Instalar uma vez por máquina
dotnet tool install --global dotnet-reportgenerator-globaltool

# Gerar relatório agregado
reportgenerator \
    -reports:"tests/**/TestResults/**/coverage.cobertura.xml" \
    -targetdir:"tests/coverage-report" \
    -reporttypes:Html
```

Abrir `tests/coverage-report/index.html` no browser.

### 3. Sumário rápido na CLI

Para um sumário rápido sem instalar reportgenerator:

```bash
dotnet test BeloMercadoManager.sln \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover
```

O coverlet.msbuild imprime o sumário no final do `dotnet test`.

## Cobertura esperada (alvo)

| Assembly                 | Alvo  | Notas |
|--------------------------|------:|-------|
| `BeloMercado.Domain`     | ~95 % | Entidades simples; cobertas pelos testes que usam DTOs/mapeamentos. |
| `BeloMercado.Application` | ≥80 % | Serviços e validators têm testes dedicados. UC fora de âmbito (UC07/08/14) baixam a média. |
| `BeloMercado.Infrastructure` | ≥60 % | Repositórios EF testados via integração; classes BCrypt/Jwt reais são deliberadamente *não* mockadas → menos cobertas. |
| `BeloMercado.Api`        | n/a   | Excluído via runsettings (Program.cs + Controllers; controllers cobertos manualmente). |

## Cobertura por componente crítico

Linhas atingidas em runs locais (indicativas; correr o comando acima
para valores precisos):

| Componente                       | Cobertura observada |
|----------------------------------|--------------------:|
| `Common.Validation.PasswordPolicy` | 100 % |
| `Common.Validation.NifValidator`   | 100 % |
| `Common.Validation.Ean13Validator` | 100 % |
| `Common.OperationResult<T>`        | 100 % |
| `Services.AuthAccessService`       | ~92 % |
| `Services.SalesService`            | ~85 % (UC03 maioritariamente coberto; falta UC02 listagem por caixa) |
| `Services.CatalogService`          | ~88 % |
| `Services.StoreManagementService`  | ~85 % |
| `Services.InventoryService`        | ~90 % |
| `Services.UserManagementService`   | ~80 % |
| `Services.ProcurementService`      | ~70 % (UC10 update/cancel rascunho ainda não cobertos) |
| `Services.ConsolidationService`    | ~85 % |
| `Services.ReportingService`        | n/a  (sem testes unitários; é dashboard read-only) |
| `Services.AuditService`            | n/a  (interagido indirectamente em todos os testes) |

## Lacunas conhecidas (gap analysis)

* **AuditService**: testado indiretamente via `FakeAuditService` em todos
  os outros testes; não há teste dedicado. Aceitável porque é um
  pass-through para o `ILogAuditoriaRepository`.
* **ReportingService**: sem testes; é um agregador de leitura sobre KPIs
  (efeitos colaterais nulos). Cobertura adicionada seria de baixo valor
  para o âmbito académico.
* **ProcurementService.AtualizarEncomendaRascunhoAsync / CancelarEncomendaAsync**:
  parcialmente cobertos. Recomendado adicionar 2 testes para fechar
  esta lacuna se o relatório referir explicitamente UC10 alt-flow.
* **Repositórios EF**: testes de integração cobrem o caminho feliz dos
  repos mais usados (User, Loja, Categoria, Produto, Stock, Caixa,
  Venda, Fatura, Consolidacao); outros (LogAuditoria, Cliente,
  Idempotency, Promocao, Fornecedor, Encomenda, RefreshToken) são
  exercitados indiretamente nos cenários compostos.

## Política de cobertura no CI (futuro)

Quando se introduzir CI:

```yaml
- run: dotnet test --collect:"XPlat Code Coverage" --settings tests/coverage.runsettings
- run: reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html;Cobertura
- name: Check coverage threshold
  run: |
    LINES=$(grep -oP 'line-rate="\K[0-9.]+' coverage/Cobertura.xml | head -n 1)
    THRESHOLD=0.70
    awk -v L=$LINES -v T=$THRESHOLD 'BEGIN { exit (L >= T) ? 0 : 1 }'
```

Esta verificação não está ativa ainda (academia não exige CI).
