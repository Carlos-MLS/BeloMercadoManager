# Belo Mercado Manager

Sistema de gestão integrada para uma cadeia de cinco lojas de conveniência
no norte de Portugal. Projeto académico da unidade curricular **Laboratórios
de Informática IV** (Universidade do Minho, 2025/2026).

> Reporta a **Etapa 3** (Implementação) do trabalho. As Etapas 1
> (Engenharia de Requisitos) e 2 (Arquitetura e Design) estão documentadas
> em [`docs/relatorio/relatorio.pdf`](docs/relatorio/relatorio.pdf).

## Stack

- **.NET 8** + C# 12
- **ASP.NET Core 8** (REST API com controllers, OpenAPI/Swagger)
- **Blazor Web App** com interactivity Server (POS e Backoffice)
- **Entity Framework Core 8** + Microsoft **SQL Server 2022** (Docker)
- **JWT** (HS256) com refresh tokens rotativos
- **BCrypt.Net-Next** (cost factor 11) para hashing de passwords

## Estrutura do repositório

    BeloMercadoManager/
    ├── src/
    │   ├── BeloMercado.Domain/          # Entidades, value objects, enums
    │   ├── BeloMercado.Application/     # DTOs, serviços, interfaces
    │   ├── BeloMercado.Infrastructure/  # EF Core, repositórios, migrations
    │   ├── BeloMercado.Api/             # ASP.NET Core REST API
    │   ├── BeloMercado.PosWeb/          # Blazor — ponto de venda
    │   └── BeloMercado.BackofficeWeb/   # Blazor — gestão
    ├── database/seed/                   # Scripts de seed
    ├── docs/
    │   ├── CLAUDE.md                    # Regras de implementação
    │   └── relatorio/relatorio.pdf      # Relatório completo (Etapas 1-2)
    ├── tests/                           # Testes unitários, de integração e aceitação
    ├── README.md
    └── setup.txt                        # Onboarding ambiente local

## Como começar

Lê e segue o [`setup.txt`](setup.txt) — descreve passo-a-passo a
preparação de uma VM Ubuntu 24.04: instalar .NET 8 SDK, Docker com
SQL Server 2022, configurar User Secrets, aplicar migrations, e correr
a API.

Resumo dos comandos principais:

    dotnet tool restore
    dotnet restore
    dotnet build
    dotnet ef database update --project src/BeloMercado.Infrastructure \
                              --startup-project src/BeloMercado.Api
    dotnet run --project src/BeloMercado.Api

API arranca em http://localhost:5009 com Swagger em /swagger.

**Conta admin de seed** (dev only, alterar em produção):

    email:    admin@belomercado.local
    password: BeloMercado2026!

## Casos de uso implementados

| UC | Nome | Estado |
|----|------|--------|
| UC01 | Registar Venda (POS) | ✅ |
| UC02 | Anular Venda + Devolução | ✅ |
| UC03 | Aplicar Promoções e Descontos | ✅ |
| UC04 | Abrir e Fechar Caixa | ✅ |
| UC05 | Gerir Catálogo de Produtos | ✅ |
| UC06 | Consultar e Monitorizar Stock | ✅ |
| UC09 | Gerir Fornecedores | ✅ |
| UC10 | Criar Encomenda a Fornecedor | ✅ |
| UC11 | Registar Receção de Mercadoria | ✅ |
| UC12 | Consolidar Dados Diários | ✅ |
| UC13 | Consultar Dashboard de KPIs | ✅ |
| UC15 | Autenticar no Sistema | ✅ |
| UC16 | Gerir Utilizadores e Perfis | ✅ |
| UC17 | Editar Perfil Próprio | ✅ |
| UC19 | Gerir Lojas da Cadeia | ✅ |

UC07, UC08, UC14 e UC18 (UI) estão fora do scope do projeto.

## Front-end (Blazor Web App, interactivity Server)

**PosWeb** (`src/BeloMercado.PosWeb`, `dotnet run` → http://localhost:5108):
- `/login` — autenticação JWT
- `/caixa` — abrir nova caixa diária
- `/caixa/{id}` — detalhe da caixa, fechar caixa
- `/caixa/{id}/vendas` — vendas registadas na caixa
- `/venda` — registar venda (3 stages: carrinho → pagamento → fatura)
- `/venda/{id}/anular` — anular venda completa ou processar devolução parcial
- `/perfil` — alterar nome próprio e password

**BackofficeWeb** (`src/BeloMercado.BackofficeWeb`, `dotnet run` → http://localhost:5132):
- `/login` — autenticação JWT
- `/dashboard` — KPIs por loja e intervalo de datas, top-10 produtos
- `/produtos` + `/produtos/{id|novo}` — catálogo (CRUD para AdminCadeia)
- `/categorias` — criar/listar categorias
- `/utilizadores` + `/utilizadores/{id|novo}` — gestão de contas
- `/lojas` + `/lojas/{id|nova}` — gestão de lojas (AdminCadeia)
- `/stock` — consulta com filtros + painel de alertas de mínimos
- `/promocoes` + `/promocoes/{id|nova}` — promoções globais ou por loja
- `/fornecedores` + `/fornecedores/{id|novo}` — gestão de fornecedores e produtos associados
- `/encomendas` + `/encomendas/nova` + `/encomendas/{id}` — criar/submeter/rececionar
- `/perfil` — alterar nome e password

Os três projetos (Api + PosWeb + Backoffice) precisam de correr em paralelo
para um demo end-to-end. Em três terminais:

    dotnet run --project src/BeloMercado.Api          # http://localhost:5009
    dotnet run --project src/BeloMercado.PosWeb       # http://localhost:5108
    dotnet run --project src/BeloMercado.BackofficeWeb  # http://localhost:5132

## Convenções e regras de implementação

Definidas em [`docs/CLAUDE.md`](docs/CLAUDE.md). Resumo:

- Arquitetura em camadas (Domain ← Application ← Infrastructure ← Api)
- API base path `/api/v1` (URL versioning, ADR-005)
- Login devolve sempre HTTP 401 genérico (RN02 do UC15: sem enumeração)
- DTOs vivem em `Application/Dtos/`; Web projects podem referenciá-los
- Audit trail transversal (todos os fluxos de mudança escrevem em
  `LogAuditoria`)
- Todas as escritas em transação EF Core (RNF-06: ACID)
- `appsettings.Development.json` no Git **sem** secrets; connection
  string e JWT key em User Secrets

## Equipa

Francisco Soares (106882), Carlos Silva (106792), Nuno Soares (107366),
Diogo Silva (97941), Rodrigo Guimarães (93236).
