# Belo Mercado — Notas de Desenvolvimento

Esta pasta é o **handbook técnico** do projeto, dirigido a quem precisa de
entender o código sem ter de o ler todo. Complementa (não substitui) os
outros documentos:

| Documento | Para quê |
|-----------|----------|
| `README.md` (raiz) | Stack, comandos para arrancar, tabela de UCs |
| `setup.txt` | Onboarding de uma VM zero ao primeiro login |
| `docs/CLAUDE.md` | Regras canónicas (decisões de arquitetura, ADRs) |
| `docs/relatorio/relatorio.pdf` | Etapas 1–2 (requisitos + arquitetura) |
| **`docs/dev/`** ← estás aqui | Mapa do código por layer/feature |

## Como ler estes documentos

| Ficheiro | Quando ler |
|----------|------------|
| [`architecture.md`](architecture.md) | Primeiro contacto. Mostra os 6 projetos, dependências e abstrações transversais (OperationResult, IUnitOfWork, AuthorizedApiClient). |
| [`backend.md`](backend.md) | Vais tocar em serviços/repositórios/controllers. Lista cada serviço com métodos-chave e o catálogo de endpoints REST. |
| [`frontend.md`](frontend.md) | Vais tocar em páginas Blazor. Cobre PosWeb e BackofficeWeb — fluxo, rotas, API clients. |
| [`business-rules.md`](business-rules.md) | Estás a debugar uma regra ou a alterar uma feature. Resumo das RNs por UC. |

## Convenções de nomenclatura

- **UC**: caso de uso, numeração do relatório (UC01–UC19; UC07/08/14/18 fora de scope).
- **WF**: wireframe / ecrã front-end. WF-01 a WF-03 estão no relatório §4.6;
  WF-04 a WF-10 são extensões internas para cobrir o backoffice.
- **RN**: regra de negócio. Cada UC no relatório lista as suas RNs (ex: RN02
  do UC15 = "login não enumera utilizadores").

## Estado atual

15 UCs em scope **todos implementados**, smoke-tested, com UI:

- **PosWeb**: login, abrir/fechar caixa, registar venda, anular/devolver, perfil.
- **BackofficeWeb**: login, dashboard KPI, catálogo, utilizadores, lojas,
  stock, promoções, fornecedores, encomendas, perfil.

Build limpo (0 warnings, 0 errors). Smoke E2E manual: login admin → criar
loja/produto/categoria → seed stock via UC11 (encomenda+receção) → venda
no PosWeb → KPI atualizado em backoffice.
