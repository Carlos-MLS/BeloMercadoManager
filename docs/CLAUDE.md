# Belo Mercado Manager — Claude Code Instructions

This file contains all the context Claude Code needs to work on this project.
**Always read this file first** when starting a new session.

## Project overview

**Belo Mercado Manager** is an integrated management system for a chain of 5
small convenience stores in northern Portugal. It supports daily operations
(sales, stock, supplier orders) and provides centralized management
(dashboards, reports, user administration).

This is an **academic project** for the course Laboratórios de Informática IV
(Universidade do Minho). The implementation must follow the architecture and
requirements documented in Chapters 3 and 4 of the project report (located at
`docs/relatorio/relatorio.pdf`).

## Critical project constraints

- **Timeline:** 7 days for full implementation
- **All code must be AI-generated.** Human team members review, test and
  give feedback but do not write code directly.
- **Scope:** 15 use cases out of 19. The following UCs are explicitly
  out of scope and must NOT be implemented: UC07, UC08, UC14, UC18 (UI only).
- **Topology:** single centralized instance (ADR-001). No offline mode.
- **Fiscal compliance:** simplified academic prototype (ADR-009). No ATCUD,
  no SAF-T, no hash chain, no AT certification.

## Tech stack (do not deviate)

- **.NET 8** (C# 12)
- **ASP.NET Core 8** for the REST API
- **Blazor Server** for both UIs (POS and Backoffice)
- **Entity Framework Core 8** as ORM (Code-First migrations)
- **Microsoft SQL Server 2022** (running in Docker locally on each developer's machine)
- **JWT** for authentication (with refresh tokens)
- **BCrypt.Net-Next** for password hashing
- **Serilog** for structured logging
- **Swashbuckle.AspNetCore** for OpenAPI/Swagger

Do not introduce additional dependencies without explicit approval.

## Repository structure

    BeloMercadoManager/
    ├── src/
    │   ├── BeloMercado.Domain/           # Entities, value objects, enums
    │   ├── BeloMercado.Application/      # Business services, DTOs, interfaces
    │   ├── BeloMercado.Infrastructure/   # EF Core, repositories, migrations
    │   ├── BeloMercado.Api/              # ASP.NET Core REST API
    │   ├── BeloMercado.PosWeb/           # Blazor Server POS application
    │   └── BeloMercado.BackofficeWeb/    # Blazor Server Backoffice application
    ├── database/
    │   └── seed/                         # Seed data scripts
    ├── docs/
    │   ├── CLAUDE.md                     # This file
    │   └── relatorio/
    │       └── relatorio.pdf             # Full project report (reference)
    ├── tests/                            # (empty for now — Etapa 4)
    └── README.md

## Development environment

Each team member develops locally on their own machine. The required
tooling is:

- VS Code with C# Dev Kit, Claude Code, SQL Server (mssql), GitLens, and
  Error Lens extensions
- .NET 8 SDK (any platform-specific installer)
- Docker (Desktop on Windows/macOS, Engine on Linux) for running SQL
  Server 2022 locally
- Git for version control

The OS is left to each developer's choice as long as it supports .NET 8
and Docker. When suggesting commands, prefer cross-platform invocations
(`dotnet` CLI, `docker` CLI, `git`); when shell-specific syntax is
unavoidable, give both PowerShell and bash variants instead of assuming
one.

## Architectural rules (from Chapter 4 of the report)

### Layered architecture
1. **Presentation:** PosWeb and BackofficeWeb consume the Api over HTTPS.
2. **Application:** services in `BeloMercado.Application/Services/`
   organized by domain (Sales, Inventory, Procurement, Catalog,
   Reporting, StoreManagement, Auth, Audit).
3. **Persistence:** all DB access goes through repositories in
   `BeloMercado.Infrastructure/Repositories/` exposed via interfaces
   in `BeloMercado.Application/Interfaces/`.

### Key services (one per file, in `Application/Services/`)
- `AuthAccessService` — UC15, UC17
- `SalesService` — UC01, UC02, UC03, UC04
- `CatalogService` — UC05
- `InventoryService` — UC06
- `ProcurementService` — UC09, UC10, UC11
- `ReportingService` — UC13
- `StoreManagementService` — UC19
- `AuditService` — transversal, called by all other services
- `ConsolidationScheduler` — UC12 (background hosted service)

### Cross-cutting rules
- **AuditService is called from every service** for state-changing
  operations (creation, deletion, status change). Log entries include
  userId, action, timestamp, IP, target entity, result.
- **All write operations are wrapped in EF Core transactions.** Rollback
  on failure (RNF-06: ACID guarantees).
- **All endpoints (except /api/v1/auth/login) require Bearer JWT.**
- **Authorization is checked at every endpoint** based on role claim
  and (when relevant) lojaId claim.

### DTOs and project references
- DTOs live exclusively in `BeloMercado.Application/Dtos/` and are
  shared between the API and the Blazor Web projects.
- `BeloMercado.PosWeb` and `BeloMercado.BackofficeWeb` have a
  `ProjectReference` to `BeloMercado.Application` to consume DTOs only.
- The Web projects MUST NOT import or instantiate types from
  `Application/Services/` or `Application/Interfaces/`. Those are
  invoked exclusively by `BeloMercado.Api` over HTTPS.
- Any new DTO needed by the UI is added to `Application/Dtos/`. Do
  not create DTOs inside Web projects.

## Domain model — key entities

Refer to the diagrams inside `docs/relatorio/relatorio.pdf` (Chapter 4)
for the full class diagram.

**Sales package:** Venda, LinhaVenda, Fatura, Cliente
**Catalog package:** Produto, Categoria, Preco (value object), Quantidade (VO)
**Inventory package:** Stock
**Procurement package:** Encomenda, LinhaEncomenda, Fornecedor
**Organization package:** Cadeia, Loja, Funcionario, GerenteDeLoja
**Security package:** Utilizador, Perfil, LogAuditoria
**Daily Operations package:** CaixaDiaria, ConsolidacaoDiaria
**Reporting package:** Relatorio, KPIDiario, RankingProdutosDiario
**Promotions package:** Promocao, PromocaoLoja, LinhaVendaPromocao

**IMPORTANT:** Cliente is INDEPENDENT of Utilizador. They are separate
tables with separate purposes (Cliente = external consumer with NIF;
Utilizador = internal employee with login). Never link them.

## API conventions

- Base path: `/api/v1` (URL versioning per ADR-005). All current
  endpoints are under v1.
- All routes pluralized (`/api/v1/vendas`, not `/api/v1/venda`)
- Sub-resources for actions: `/api/v1/vendas/{id}/anular`
- JSON only (`application/json`)
- Errors follow RFC 7807 (`application/problem+json`) with domain error
  codes (e.g., `STOCK_INSUFICIENTE`, `AUTH_INVALID_CREDENTIALS`)
- JWT in `Authorization: Bearer <token>` header
- Idempotency-Key header required for `POST /api/v1/vendas`

The full API specification is in Chapter 4 of `docs/relatorio/relatorio.pdf`.

## Critical business rules

These rules must be enforced regardless of who calls the service:

### Authentication (UC15)
- Password hashed with BCrypt (cost factor 11)
- 5 failed attempts → account locked for 15 minutes
- JWT access token: 15 minutes lifetime
- Refresh token: 7 days, rotated on each use
- JWT claims: `sub` (userId), `role`, `lojaId`, `exp`
- Failed login response is generic ("invalid credentials") — never reveal
  whether the email exists
- Failed login ALWAYS returns HTTP 401 with a generic message,
  regardless of the underlying cause (email not found, wrong
  password, account inactive, account locked). This enforces RN02
  (no user enumeration).
- The login handler MUST always perform a BCrypt verify operation
  (against a dummy hash if the user does not exist) to prevent
  timing attacks that would reveal account existence.

### Sales (UC01)
- A sale requires at least one line item
- Cannot create a sale if no CaixaDiaria is open for that store/day
- IVA calculated per line at the product's rate (6%, 13%, 23%)
- Invoice number is sequential per store per year (format: `<storeId>/<year>/<seq>`)
- All persistence (Venda + LinhaVenda + Fatura + Stock decrement) happens
  in ONE transaction
- After successful sale, emit AuditService event `VENDA_FINALIZADA`
- Invoice footer: "PROTÓTIPO ACADÉMICO — SEM CONFORMIDADE FISCAL AT"

### Cash Register (UC04)
- Only one open caixa per store per day
- Closing caixa triggers ConsolidationScheduler immediately
- Difference > 10€ requires justification text (not optional)

### Authorization matrix
- **Operador de Caixa:** can do POS operations on their assigned store only
- **Gerente de Loja:** Operador permissions + manage stock/orders/inventory
  on their store; can anular vendas <24h or any age
- **Administrador da Cadeia:** all operations across all stores; manages
  catalog, suppliers, users, stores
- **Administrador IT:** user management + audit log access

## Configuration policy (where to put what)

The development environment uses SQL Server running in Docker, accessed
via SQL Authentication (Windows Authentication is not used). Credentials
must be managed via User Secrets.

The project uses three configuration sources, with this precedence at
runtime: `appsettings.json` ← `appsettings.Development.json` ← User Secrets.

### `appsettings.json` (committed to Git)
Shared settings, identical for everyone, no secrets:
- JWT issuer, audience, lifetimes
- Logging defaults
- Pagination sizes, timeouts, feature flags

### `appsettings.Development.json` (committed to Git)
Development-environment values that contain NO secrets:
- Verbose logging levels (e.g., `"Microsoft.EntityFrameworkCore": "Information"`
  to see SQL queries during development)
- Local development URLs
- **Does NOT contain the connection string** (because the connection
  string contains the SQL `sa` password and must be a secret)

### User Secrets (NEVER committed to Git)
All secrets — both passwords and signing keys:
- `ConnectionStrings:DefaultConnection` — full SQL Server connection
  string with `User Id=sa;Password=...`
- `Jwt:Key` — signing key for JWT tokens (≥ 32 chars, random)

User Secrets apply only in the `Development` environment by default.
Production deployments will use environment variables or a secret manager.

### Initialization commands
The `BeloMercado.Api` project is the ONLY project that needs User Secrets.
Initialize it once with:

    cd src/BeloMercado.Api
    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=BeloMercadoDB;User Id=sa;Password=<TEAM_PASSWORD>;TrustServerCertificate=True;Encrypt=False;"
    dotnet user-secrets set "Jwt:Key" "<random-key-shared-by-the-team>"

Each developer runs these once on their local machine. The team agrees on
a shared `<TEAM_PASSWORD>` (matching the `MSSQL_SA_PASSWORD` of the Docker
container) and `<random-key>` to keep tokens portable across machines.

### SQL Server local setup (every developer)
SQL Server runs in Docker on each developer's machine. To start it:

    docker run -e "ACCEPT_EULA=Y" \
               -e "MSSQL_SA_PASSWORD=<TEAM_PASSWORD>" \
               -p 1433:1433 \
               --name belomercado-sql \
               -v belomercado-data:/var/opt/mssql \
               -d mcr.microsoft.com/mssql/server:2022-latest

Subsequent starts: `docker start belomercado-sql`. Data persists in the
named volume `belomercado-data`.

Verify the container is running and reachable:

    docker exec -it belomercado-sql /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P '<TEAM_PASSWORD>' -Q "SELECT @@VERSION"

### Code-level rules
- Always read configuration via `IConfiguration` (e.g.,
  `IConfiguration.GetConnectionString("DefaultConnection")` or
  `IConfiguration["Jwt:Key"]`) or strongly-typed options
  (`IOptions<JwtSettings>`). Never hardcode secrets.
- The connection string **must** include `TrustServerCertificate=True`
  because the Docker SQL Server uses a self-signed certificate.
- The connection string format for our setup is `Server=localhost,1433`
  (comma + port), because we connect to the Docker SQL Server over TCP/IP.
  Do not use named-instance formats like `localhost\SQLEXPRESS`.
- The default registration in `Program.cs` (`WebApplication.CreateBuilder`)
  already loads `appsettings.json` → `appsettings.Development.json` →
  User Secrets (in Development) → Environment Variables in the correct
  order. Do not override this unless strictly necessary.

## Coding conventions

- **Naming:** PascalCase for classes/methods/public properties;
  camelCase for parameters/locals; UPPER_CASE for constants
- **One class per file.** File name matches class name.
- **No public fields.** Use auto-properties.
- **DTOs separate from entities.** DTOs live in `Application/Dtos/`,
  entities in `Domain/Entities/`.
- **Use AutoMapper or manual mapping** — be consistent in one direction.
- **Async all the way down.** All repository and service methods that
  hit the DB are `async Task<T>`.
- **No magic strings.** Define constants for role names, error codes, etc.
- **No swallowed exceptions.** Either handle them meaningfully or let
  them propagate.

## Frontend conventions (Blazor Server)

- **POS pages** under `BeloMercado.PosWeb/Pages/`
- **Backoffice pages** under `BeloMercado.BackofficeWeb/Pages/`
- **Shared components** under `<Project>/Shared/`
- **Services** that wrap API calls under `<Project>/Services/`
- **Use HttpClient injected via DI**, configured to call the API base URL
- **Authentication state** stored using `AuthenticationStateProvider`
  (custom implementation that reads JWT from secure storage)

### POS UX rules (from WF-01, WF-02, WF-03)
- Search input keeps autofocus to receive barcode scanner input
- Touch targets ≥ 60×60 px
- Primary color = confirm; danger color = cancel
- Sale completion in ≤ 4 taps from product scan to receipt

### Backoffice UX rules (from WF-04, WF-05, WF-06)
- Persistent left navigation
- Filters always visible at the top of lists
- Master-detail layout for entity editing
- Display "last consolidation timestamp" on dashboards

## Git workflow

- **All commits go to `main`.** The team works in lockstep with manual
  verification (running the code), so feature branches and per-UC PRs add
  overhead without adding review value.
- One commit per UC (or per closely related sub-task within a UC).
- Commit messages follow Conventional Commits with a UC-scoped tag:
  `feat(auth): implement UC15 user authentication`,
  `feat(lojas): implement UC19 store management`,
  `chore(scaffold): ...`, `docs: ...`, `fix(sales): ...`.
- Push at end of every day, even if work is incomplete.

## What NOT to do

- ❌ Do not implement UC07, UC08, UC14, or UC18 (UI). They are out of scope.
- ❌ Do not add fiscal compliance features (ATCUD, hash chain, SAF-T, QR code).
- ❌ Do not introduce a message broker, Redis, microservices, or any
   infrastructure beyond .NET + SQL Server.
- ❌ Do not link Cliente and Utilizador entities.
- ❌ Do not invent new endpoints not in the API specification.
- ❌ Do not skip the AuditService call in state-changing operations.
- ❌ Do not commit secrets (passwords, JWT keys) to Git.
- ❌ Do not assume a specific developer OS in code or scripts. Where the
   command line matters, prefer cross-platform tooling (`dotnet`,
   `docker`, `git`) or provide both PowerShell and bash variants.

## When in doubt

If a requirement is ambiguous or seems to conflict with another:

1. Re-read the relevant UC in Chapter 3 of `docs/relatorio/relatorio.pdf`
2. Re-read the relevant ADR in Chapter 4 of `docs/relatorio/relatorio.pdf`
3. Pick the option that minimizes scope creep
4. Document the decision in a comment in the code
5. Flag it in the PR description for human review

Never silently invent business rules.