using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeloMercado.Infrastructure.Persistence;

/// <summary>
/// Idempotent seeding of demo data executed once at API startup.
/// Sem isto, uma BD fresca tem zero produtos / fornecedores / vendas e o
/// dashboard fica vazio. Todas as entidades usam Guids fixos para evitar
/// duplicados em reruns. A consolidação é invocada explicitamente após
/// criar vendas para que os KPIs estejam materializados.
/// </summary>
public static class DemoDataSeeder
{
    // 5 lojas como o relatório descreve (cadeia Belo Mercado, zona norte).
    private static readonly (Guid Id, string Designacao, string Morada, string Cp, string Tel, string Email)[] LojasSeed =
    {
        (new("aaaaaaaa-0000-0000-0000-000000000001"), "Belo Mercado — Braga 01",
            "Av. da Liberdade 100", "4710-000", "253000001", "braga01@belomercado.local"),
        (new("aaaaaaaa-0000-0000-0000-000000000002"), "Belo Mercado — Guimarães",
            "R. de Santa Maria 25", "4810-441", "253000002", "guimaraes@belomercado.local"),
        (new("aaaaaaaa-0000-0000-0000-000000000003"), "Belo Mercado — Famalicão",
            "Av. do Brasil 30", "4760-000", "252000001", "famalicao@belomercado.local"),
        (new("aaaaaaaa-0000-0000-0000-000000000004"), "Belo Mercado — Barcelos",
            "R. António Barroso 50", "4750-000", "253000003", "barcelos@belomercado.local"),
        (new("aaaaaaaa-0000-0000-0000-000000000005"), "Belo Mercado — Vila Real",
            "Av. Carvalho Araújo 12", "5000-000", "259000001", "vilareal@belomercado.local")
    };

    public static Guid LojaBragaId => LojasSeed[0].Id;

    private static readonly (Guid Id, string Nome)[] Categorias =
    {
        (new("bbbbbbbb-0000-0000-0000-000000000001"), "Mercearia"),
        (new("bbbbbbbb-0000-0000-0000-000000000002"), "Bebidas"),
        (new("bbbbbbbb-0000-0000-0000-000000000003"), "Lacticínios"),
        (new("bbbbbbbb-0000-0000-0000-000000000004"), "Higiene"),
        (new("bbbbbbbb-0000-0000-0000-000000000005"), "Limpeza"),
        (new("bbbbbbbb-0000-0000-0000-000000000006"), "Padaria"),
        (new("bbbbbbbb-0000-0000-0000-000000000007"), "Congelados"),
        (new("bbbbbbbb-0000-0000-0000-000000000008"), "Frutas e Legumes")
    };

    // EAN-13 com dígitos de controlo correctos (mod-10).
    private static readonly (Guid Id, string Ean, string Descricao, Guid CategoriaId,
        decimal Preco, TaxaIva Iva, string Unidade)[] Produtos =
    {
        (new("cccccccc-0000-0000-0000-000000000001"), "5601234567892",
            "Pão de forma 500g", new("bbbbbbbb-0000-0000-0000-000000000006"),
            1.29m, TaxaIva.Reduzida, "UN"),
        (new("cccccccc-0000-0000-0000-000000000002"), "5602345678905",
            "Iogurte natural 4×125g", new("bbbbbbbb-0000-0000-0000-000000000003"),
            2.49m, TaxaIva.Intermedia, "UN"),
        (new("cccccccc-0000-0000-0000-000000000003"), "5603456789018",
            "Refrigerante 1.5L", new("bbbbbbbb-0000-0000-0000-000000000002"),
            1.49m, TaxaIva.Normal, "UN"),
        (new("cccccccc-0000-0000-0000-000000000004"), "5604567890121",
            "Detergente louça 750ml", new("bbbbbbbb-0000-0000-0000-000000000005"),
            2.89m, TaxaIva.Normal, "UN"),
        (new("cccccccc-0000-0000-0000-000000000005"), "5605678901234",
            "Sabonete 100g", new("bbbbbbbb-0000-0000-0000-000000000004"),
            0.99m, TaxaIva.Normal, "UN"),
        (new("cccccccc-0000-0000-0000-000000000006"), "5606789012347",
            "Café expresso 250g", new("bbbbbbbb-0000-0000-0000-000000000001"),
            3.79m, TaxaIva.Normal, "UN"),
        (new("cccccccc-0000-0000-0000-000000000007"), "5607890123450",
            "Arroz Carolino 1kg", new("bbbbbbbb-0000-0000-0000-000000000001"),
            1.39m, TaxaIva.Reduzida, "UN"),
        (new("cccccccc-0000-0000-0000-000000000008"), "5608901234563",
            "Azeite virgem 750ml", new("bbbbbbbb-0000-0000-0000-000000000001"),
            5.49m, TaxaIva.Reduzida, "UN"),
        (new("cccccccc-0000-0000-0000-000000000009"), "5609012345676",
            "Leite meio-gordo 1L", new("bbbbbbbb-0000-0000-0000-000000000003"),
            0.89m, TaxaIva.Reduzida, "UN"),
        (new("cccccccc-0000-0000-0000-000000000010"), "5600123456786",
            "Água 1.5L", new("bbbbbbbb-0000-0000-0000-000000000002"),
            0.49m, TaxaIva.Normal, "UN")
    };

    private static readonly Guid FornAlmeidaId = new("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid FornBebidasNorteId = new("eeeeeeee-0000-0000-0000-000000000002");
    private static readonly Guid FornHigieneProId = new("eeeeeeee-0000-0000-0000-000000000003");

    // BCrypt cost-11 hash de "BeloMercado2026!"
    private const string DefaultPasswordHash =
        "$2a$11$NTAjq03pqc8lDzIqdr0nDe42S040X37qaOce9t4ds9obe5vlEF3mC";

    // Operador demo legacy (mantido para login rápido no POS)
    private static readonly Guid OperadorDemoId = new("dddddddd-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BeloMercadoDbContext>();
        var consolidationService = scope.ServiceProvider.GetRequiredService<ConsolidationService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoDataSeeder");

        try
        {
            await db.Database.MigrateAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Demo seed: não foi possível aplicar migrations — verifica a ligação à BD.");
            return;
        }

        var now = DateTime.UtcNow;

        await SeedLojasAsync(db, now, logger, ct);
        await SeedCategoriasAsync(db, now, logger, ct);
        await SeedProdutosAsync(db, now, logger, ct);
        await db.SaveChangesAsync(ct);

        await SeedStockAsync(db, ct);
        await db.SaveChangesAsync(ct);

        await SeedUtilizadoresAsync(db, now, logger, ct);
        await SeedFornecedoresAsync(db, now, logger, ct);
        await db.SaveChangesAsync(ct);

        await SeedProdutoFornecedorAsync(db, logger, ct);
        await db.SaveChangesAsync(ct);

        await SeedPromocoesAsync(db, now, logger, ct);
        await SeedEncomendasAsync(db, now, logger, ct);
        await db.SaveChangesAsync(ct);

        var diasComVendas = await SeedHistoricoVendasAsync(db, now, logger, ct);
        await db.SaveChangesAsync(ct);

        // Consolida KPIs para cada (loja, dia) com vendas — alimenta o dashboard.
        if (diasComVendas.Count > 0)
        {
            await RunConsolidationsAsync(consolidationService, diasComVendas, logger, ct);
        }
    }

    // ---------------------------------------------------------------- Lojas

    private static async Task SeedLojasAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        foreach (var (id, designacao, morada, cp, tel, email) in LojasSeed)
        {
            if (await db.Lojas.AnyAsync(l => l.Id == id, ct)) continue;
            db.Lojas.Add(new Loja
            {
                Id = id,
                Designacao = designacao,
                Morada = morada,
                CodigoPostal = cp,
                Contacto = tel,
                Email = email,
                Horario = "Seg-Sex 8h-21h, Sáb 8h-13h",
                StockMinPorDefeito = 5,
                StockMaxPorDefeito = 100,
                Ativo = true,
                DataCriacao = now.AddMonths(-12)
            });
            logger.LogInformation("Demo seed: criada loja {Nome}", designacao);
        }
    }

    // ----------------------------------------------------------- Categorias

    private static async Task SeedCategoriasAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        foreach (var (id, nome) in Categorias)
        {
            if (await db.Categorias.AnyAsync(c => c.Id == id, ct)) continue;
            db.Categorias.Add(new Categoria
            {
                Id = id, Nome = nome, Ativo = true, DataCriacao = now
            });
        }
    }

    // ------------------------------------------------------------- Produtos

    private static async Task SeedProdutosAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        foreach (var (id, ean, descricao, catId, preco, iva, unidade) in Produtos)
        {
            if (await db.Produtos.AnyAsync(p => p.Id == id, ct)) continue;
            db.Produtos.Add(new Produto
            {
                Id = id, Ean13 = ean, Descricao = descricao,
                CategoriaId = catId, Preco = preco, TaxaIva = iva,
                UnidadeMedida = unidade, Ativo = true, DataCriacao = now
            });
        }
    }

    // ---------------------------------------------------------------- Stock

    private static async Task SeedStockAsync(BeloMercadoDbContext db, CancellationToken ct)
    {
        foreach (var (lojaId, _, _, _, _, _) in LojasSeed)
        {
            foreach (var (prodId, _, _, _, _, _, _) in Produtos)
            {
                if (await db.Stocks.AnyAsync(s => s.ProdutoId == prodId && s.LojaId == lojaId, ct))
                    continue;
                db.Stocks.Add(new Stock
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = prodId, LojaId = lojaId,
                    Quantidade = 50, QtdMinima = 5, QtdMaxima = 100
                });
            }
        }
    }

    // ----------------------------------------------------- Utilizadores demo

    private static async Task SeedUtilizadoresAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        // Operador demo (loja Braga) — mantido para login rápido no POS
        if (!await db.Utilizadores.AnyAsync(u => u.Id == OperadorDemoId, ct))
        {
            db.Utilizadores.Add(new Utilizador
            {
                Id = OperadorDemoId,
                Nome = "Operador Demo",
                Email = "operador@belomercado.local",
                PasswordHash = DefaultPasswordHash,
                PasswordTemporaria = false,
                Perfil = Perfil.OperadorCaixa,
                LojaId = LojaBragaId,
                Ativo = true,
                DataCriacao = now,
                TentativasFalhadas = 0
            });
            logger.LogInformation("Demo seed: criado utilizador OperadorCaixa demo");
        }

        // Um Gerente de Loja por cada uma das 5 lojas — assim o admin pode
        // simular o fluxo completo de Gerente em qualquer loja.
        var gerentes = new (Guid Id, string Email, string Nome, Guid LojaId)[]
        {
            (new("dddddddd-0000-0000-0000-000000000002"), "gerente.braga@belomercado.local",
                "Teresa Vieira", LojasSeed[0].Id),
            (new("dddddddd-0000-0000-0000-000000000003"), "gerente.guimaraes@belomercado.local",
                "Carlos Sousa", LojasSeed[1].Id),
            (new("dddddddd-0000-0000-0000-000000000004"), "gerente.famalicao@belomercado.local",
                "Ana Pereira", LojasSeed[2].Id),
            (new("dddddddd-0000-0000-0000-000000000005"), "gerente.barcelos@belomercado.local",
                "Rui Costa", LojasSeed[3].Id),
            (new("dddddddd-0000-0000-0000-000000000006"), "gerente.vilareal@belomercado.local",
                "Mariana Lopes", LojasSeed[4].Id)
        };
        foreach (var (id, email, nome, lojaId) in gerentes)
        {
            if (await db.Utilizadores.AnyAsync(u => u.Id == id, ct)) continue;
            db.Utilizadores.Add(new Utilizador
            {
                Id = id,
                Nome = nome,
                Email = email,
                PasswordHash = DefaultPasswordHash,
                PasswordTemporaria = false,
                Perfil = Perfil.GerenteLoja,
                LojaId = lojaId,
                Ativo = true,
                DataCriacao = now,
                TentativasFalhadas = 0
            });
            logger.LogInformation("Demo seed: criado Gerente {Nome} para {Loja}", nome, lojaId);
        }
    }

    // ------------------------------------------------------- Fornecedores

    private static async Task SeedFornecedoresAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        var fornecedores = new[]
        {
            new Fornecedor
            {
                Id = FornAlmeidaId,
                Nome = "Distribuição Almeida, Lda.",
                Nif = "500111222",
                Morada = "Zona Industrial, Lote 14, Braga",
                Contacto = "253111222",
                Email = "encomendas@almeida.pt",
                Ramo = "Mercearia geral",
                CondicoesPagamento = "30 dias",
                Ativo = true, DataCriacao = now.AddMonths(-12)
            },
            new Fornecedor
            {
                Id = FornBebidasNorteId,
                Nome = "Bebidas do Norte",
                Nif = "501222333",
                Morada = "R. da Estação 7, Vila Real",
                Contacto = "259333444",
                Email = "comercial@bebidasnorte.pt",
                Ramo = "Bebidas",
                CondicoesPagamento = "Pronto pagamento",
                Ativo = true, DataCriacao = now.AddMonths(-9)
            },
            new Fornecedor
            {
                Id = FornHigieneProId,
                Nome = "HigienePro",
                Nif = "502333444",
                Morada = "Av. Central 200, Porto",
                Contacto = "223555666",
                Email = "vendas@higienepro.pt",
                Ramo = "Higiene e limpeza",
                CondicoesPagamento = "60 dias",
                Ativo = true, DataCriacao = now.AddMonths(-6)
            }
        };
        foreach (var f in fornecedores)
        {
            if (await db.Fornecedores.AnyAsync(x => x.Id == f.Id, ct)) continue;
            db.Fornecedores.Add(f);
            logger.LogInformation("Demo seed: criado fornecedor {Nome}", f.Nome);
        }
    }

    // -------------------------------------------- Produto <-> Fornecedor

    private static async Task SeedProdutoFornecedorAsync(
        BeloMercadoDbContext db, ILogger logger, CancellationToken ct)
    {
        var set = db.Set<ProdutoFornecedor>();
        var mapping = new (Guid Forn, Guid[] Cats, decimal PrecoFactor)[]
        {
            (FornAlmeidaId, new[]
            {
                new Guid("bbbbbbbb-0000-0000-0000-000000000001"),
                new Guid("bbbbbbbb-0000-0000-0000-000000000003"),
                new Guid("bbbbbbbb-0000-0000-0000-000000000006"),
                new Guid("bbbbbbbb-0000-0000-0000-000000000007"),
                new Guid("bbbbbbbb-0000-0000-0000-000000000008")
            }, 0.65m),
            (FornBebidasNorteId, new[]
            {
                new Guid("bbbbbbbb-0000-0000-0000-000000000002")
            }, 0.55m),
            (FornHigieneProId, new[]
            {
                new Guid("bbbbbbbb-0000-0000-0000-000000000004"),
                new Guid("bbbbbbbb-0000-0000-0000-000000000005")
            }, 0.60m)
        };

        foreach (var (forn, cats, factor) in mapping)
        {
            foreach (var (prodId, _, _, catId, preco, _, _) in Produtos)
            {
                if (!cats.Contains(catId)) continue;
                if (await set.AnyAsync(pf =>
                    pf.FornecedorId == forn && pf.ProdutoId == prodId, ct)) continue;

                set.Add(new ProdutoFornecedor
                {
                    FornecedorId = forn,
                    ProdutoId = prodId,
                    PrecoCusto = Math.Round(preco * factor, 2),
                    DescontoPercentagem = 0,
                    PrazoEntregaDias = 3
                });
            }
        }
    }

    // ------------------------------------------------------------ Promoções

    private static async Task SeedPromocoesAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(now);
        var promos = new[]
        {
            new Promocao
            {
                Id = new Guid("ffffffff-0000-0000-0000-000000000001"),
                Nome = "10% off Refrigerante",
                ProdutoId = new Guid("cccccccc-0000-0000-0000-000000000003"),
                LojaId = null,
                Tipo = TipoPromocao.Percentagem,
                Valor = 10m,
                DataInicio = hoje.AddDays(-15),
                DataFim = hoje.AddDays(15),
                Ativo = true,
                DataCriacao = now.AddDays(-15)
            },
            new Promocao
            {
                Id = new Guid("ffffffff-0000-0000-0000-000000000002"),
                Nome = "Pão a 0.99€ (Braga)",
                ProdutoId = new Guid("cccccccc-0000-0000-0000-000000000001"),
                LojaId = LojaBragaId,
                Tipo = TipoPromocao.PrecoFixo,
                Valor = 0.99m,
                DataInicio = hoje.AddDays(-3),
                DataFim = hoje.AddDays(7),
                Ativo = true,
                DataCriacao = now.AddDays(-3)
            }
        };
        foreach (var p in promos)
        {
            if (await db.Promocoes.AnyAsync(x => x.Id == p.Id, ct)) continue;
            db.Promocoes.Add(p);
        }
    }

    // ----------------------------------------------------------- Encomendas

    private static async Task SeedEncomendasAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        // Conjunto inicial: 1 rascunho + 1 submetida + 1 parcial + 1 recebida (loja Braga).
        var encomendas = new (Guid Id, Guid Forn, Guid Loja, EstadoEncomenda Estado, int DiasAtras,
            (Guid Prod, int Qtd, decimal Custo)[] Linhas, int[]? Rececionado)[]
        {
            (new Guid("baaaaaa0-0000-0000-0000-000000000001"), FornAlmeidaId, LojasSeed[0].Id,
                EstadoEncomenda.Rascunho, 0,
                new[]
                {
                    (new Guid("cccccccc-0000-0000-0000-000000000007"), 30, 0.90m),
                    (new Guid("cccccccc-0000-0000-0000-000000000008"), 12, 3.57m)
                },
                null),

            (new Guid("baaaaaa0-0000-0000-0000-000000000002"), FornBebidasNorteId, LojasSeed[1].Id,
                EstadoEncomenda.Submetida, 2,
                new[]
                {
                    (new Guid("cccccccc-0000-0000-0000-000000000003"), 60, 0.82m),
                    (new Guid("cccccccc-0000-0000-0000-000000000010"), 96, 0.27m)
                },
                null),

            (new Guid("baaaaaa0-0000-0000-0000-000000000003"), FornAlmeidaId, LojasSeed[2].Id,
                EstadoEncomenda.ParcialmenteRecebida, 5,
                new[]
                {
                    (new Guid("cccccccc-0000-0000-0000-000000000001"), 50, 0.84m),
                    (new Guid("cccccccc-0000-0000-0000-000000000002"), 24, 1.62m),
                    (new Guid("cccccccc-0000-0000-0000-000000000009"), 80, 0.58m)
                },
                new[] { 50, 24, 40 }),

            (new Guid("baaaaaa0-0000-0000-0000-000000000004"), FornHigieneProId, LojasSeed[0].Id,
                EstadoEncomenda.Recebida, 10,
                new[]
                {
                    (new Guid("cccccccc-0000-0000-0000-000000000004"), 30, 1.73m),
                    (new Guid("cccccccc-0000-0000-0000-000000000005"), 60, 0.59m)
                },
                new[] { 30, 60 }),

            (new Guid("baaaaaa0-0000-0000-0000-000000000005"), FornBebidasNorteId, LojasSeed[3].Id,
                EstadoEncomenda.Recebida, 8,
                new[]
                {
                    (new Guid("cccccccc-0000-0000-0000-000000000003"), 40, 0.82m),
                    (new Guid("cccccccc-0000-0000-0000-000000000010"), 60, 0.27m)
                },
                new[] { 40, 60 }),

            (new Guid("baaaaaa0-0000-0000-0000-000000000006"), FornAlmeidaId, LojasSeed[4].Id,
                EstadoEncomenda.Submetida, 1,
                new[]
                {
                    (new Guid("cccccccc-0000-0000-0000-000000000006"), 20, 2.46m),
                    (new Guid("cccccccc-0000-0000-0000-000000000007"), 25, 0.90m)
                },
                null)
        };

        foreach (var (id, fornId, lojaId, estado, diasAtras, linhas, rececionado) in encomendas)
        {
            if (await db.Encomendas.AnyAsync(x => x.Id == id, ct)) continue;

            var dataCriacao = now.AddDays(-diasAtras);
            DateTime? dataSubmissao = estado == EstadoEncomenda.Rascunho
                ? null
                : dataCriacao.AddHours(1);
            DateTime? dataRececao = estado is EstadoEncomenda.Recebida or EstadoEncomenda.ParcialmenteRecebida
                ? dataCriacao.AddDays(3)
                : null;

            decimal total = 0;
            var linhaEntities = new List<LinhaEncomenda>();
            for (var i = 0; i < linhas.Length; i++)
            {
                var (prodId, qtd, custo) = linhas[i];
                var produto = await db.Produtos.AsNoTracking().FirstAsync(p => p.Id == prodId, ct);
                var sub = Math.Round(qtd * custo, 2);
                total += sub;
                linhaEntities.Add(new LinhaEncomenda
                {
                    Id = Guid.NewGuid(),
                    EncomendaId = id,
                    ProdutoId = prodId,
                    ProdutoDescricao = produto.Descricao,
                    ProdutoEan13 = produto.Ean13,
                    Quantidade = qtd,
                    PrecoCustoUnitario = custo,
                    DescontoPercentagem = 0,
                    Subtotal = sub,
                    QuantidadeRecebida = rececionado is null ? 0 : rececionado[i]
                });
            }

            // Atribui a criação ao gerente da loja correspondente
            var criadoPor = (await db.Utilizadores.AsNoTracking()
                .FirstOrDefaultAsync(u => u.LojaId == lojaId && u.Perfil == Perfil.GerenteLoja, ct))
                ?.Id ?? OperadorDemoId;

            db.Encomendas.Add(new Encomenda
            {
                Id = id,
                FornecedorId = fornId,
                LojaId = lojaId,
                CriadoPorId = criadoPor,
                Estado = estado,
                DataCriacao = dataCriacao,
                DataSubmissao = dataSubmissao,
                DataRececao = dataRececao,
                Total = total,
                Linhas = linhaEntities
            });
        }
    }

    // ----------------------------------------------- Caixas + Vendas histórico

    private static async Task<List<(Guid lojaId, DateOnly data)>> SeedHistoricoVendasAsync(
        BeloMercadoDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        var diasComVendas = new List<(Guid, DateOnly)>();
        var hojeUtc = DateTime.UtcNow.Date;
        var marcadorData = DateOnly.FromDateTime(hojeUtc.AddDays(-7));

        // Marcador idempotente: se já existir caixa em D-7 para a loja Braga, assumimos
        // que o histórico já foi semeado num arranque anterior.
        if (await db.CaixasDiarias.AnyAsync(c => c.LojaId == LojaBragaId && c.Data == marcadorData, ct))
        {
            // Mesmo assim, pode haver KPIs em falta — devolve a lista de dias
            // para a fase de consolidação tentar materializar (idempotente).
            var caixasExistentes = await db.CaixasDiarias
                .AsNoTracking()
                .Where(c => c.Data >= marcadorData && c.Estado == EstadoCaixa.Fechada)
                .Select(c => new { c.LojaId, c.Data })
                .ToListAsync(ct);
            return caixasExistentes.Select(c => (c.LojaId, c.Data)).ToList();
        }

        var catalogo = await db.Produtos.AsNoTracking().Where(p => p.Ativo).ToListAsync(ct);
        if (catalogo.Count == 0) return diasComVendas;

        var rng = new Random(42);

        // Para cada loja, sequencia separada de faturas por ano
        var seqPorLojaAno = new Dictionary<(Guid lojaId, int ano), int>();
        async Task<int> NextSeq(Guid lojaId, int ano)
        {
            var key = (lojaId, ano);
            if (!seqPorLojaAno.TryGetValue(key, out var seq))
            {
                seq = await db.Faturas
                    .Where(f => f.LojaId == lojaId && f.Ano == ano)
                    .Select(f => (int?)f.NumeroSequencial)
                    .MaxAsync(ct) ?? 0;
            }
            seq++;
            seqPorLojaAno[key] = seq;
            return seq;
        }

        foreach (var (lojaId, lojaNome, _, _, _, _) in LojasSeed)
        {
            // operador desta loja (fallback ao operador demo se nenhum)
            var operador = (await db.Utilizadores.AsNoTracking()
                .FirstOrDefaultAsync(u => u.LojaId == lojaId && u.Perfil == Perfil.GerenteLoja, ct))
                ?.Id ?? OperadorDemoId;

            for (var d = 7; d >= 1; d--)
            {
                var dia = hojeUtc.AddDays(-d);
                var diaOnly = DateOnly.FromDateTime(dia);

                if (await db.CaixasDiarias.AnyAsync(c => c.LojaId == lojaId && c.Data == diaOnly, ct))
                {
                    diasComVendas.Add((lojaId, diaOnly));
                    continue;
                }

                var caixaId = Guid.NewGuid();
                var abertura = dia.AddHours(8);
                var fecho = dia.AddHours(21);

                var nVendas = rng.Next(8, 18);
                decimal totalNumerario = 0m;

                for (var i = 0; i < nVendas; i++)
                {
                    var minutos = rng.Next(0, 13 * 60);
                    var hora = abertura.AddMinutes(minutos);
                    var seq = await NextSeq(lojaId, dia.Year);
                    var (venda, linhas, fatura) = BuildVendaAleatoria(
                        hora, caixaId, lojaId, operador, rng, catalogo, seq);
                    db.Vendas.Add(venda);
                    db.LinhasVenda.AddRange(linhas);
                    db.Faturas.Add(fatura);
                    if (venda.MetodoPagamento == MetodoPagamento.Numerario)
                    {
                        totalNumerario += venda.Total;
                    }
                }

                var valorEsperado = 100m + totalNumerario;
                var caixa = new CaixaDiaria
                {
                    Id = caixaId,
                    LojaId = lojaId,
                    Data = diaOnly,
                    OperadorAberturaId = operador,
                    DataHoraAbertura = abertura,
                    OperadorFechoId = operador,
                    DataHoraFecho = fecho,
                    FundoInicial = 100m,
                    ValorEsperado = valorEsperado,
                    ValorContado = Math.Round(valorEsperado + (decimal)(rng.NextDouble() * 4 - 2), 2),
                    Estado = EstadoCaixa.Fechada,
                    ConsolidacaoTriggered = false
                };
                caixa.Diferenca = caixa.ValorContado - caixa.ValorEsperado;
                db.CaixasDiarias.Add(caixa);

                diasComVendas.Add((lojaId, diaOnly));
                logger.LogInformation("Demo seed: loja {Loja} dia {Dia} — {N} vendas", lojaNome, diaOnly, nVendas);
            }
        }

        return diasComVendas;
    }

    private static (Venda venda, List<LinhaVenda> linhas, Fatura fatura)
        BuildVendaAleatoria(
            DateTime quando, Guid caixaId, Guid lojaId, Guid operadorId,
            Random rng, List<Produto> catalogo, int faturaSeq)
    {
        var nLinhas = rng.Next(1, 5);
        var produtosSelecionados = catalogo
            .OrderBy(_ => rng.Next())
            .Take(nLinhas)
            .ToList();

        var vendaId = Guid.NewGuid();
        var linhas = new List<LinhaVenda>(nLinhas);
        decimal totalSemIva = 0, totalIva = 0, total = 0;

        foreach (var p in produtosSelecionados)
        {
            var qtd = rng.Next(1, 4);
            var taxa = (int)p.TaxaIva;
            var subtotal = Math.Round(p.Preco * qtd, 2);
            var valorIva = Math.Round(subtotal * taxa / (100m + taxa), 2);
            var subtotalSemIva = subtotal - valorIva;

            linhas.Add(new LinhaVenda
            {
                Id = Guid.NewGuid(),
                VendaId = vendaId,
                ProdutoId = p.Id,
                ProdutoDescricao = p.Descricao,
                ProdutoEan13 = p.Ean13,
                Quantidade = qtd,
                PrecoUnitario = p.Preco,
                TaxaIvaPercentagem = taxa,
                Subtotal = subtotal,
                ValorIva = valorIva
            });

            total += subtotal;
            totalIva += valorIva;
            totalSemIva += subtotalSemIva;
        }

        var metodo = rng.Next(3) switch
        {
            0 => MetodoPagamento.Numerario,
            1 => MetodoPagamento.Cartao,
            _ => MetodoPagamento.MBWay
        };

        decimal? valorRecebido = null;
        decimal? troco = null;
        if (metodo == MetodoPagamento.Numerario)
        {
            valorRecebido = Math.Ceiling(total / 5m) * 5m;
            troco = valorRecebido - total;
        }

        var venda = new Venda
        {
            Id = vendaId,
            LojaId = lojaId,
            CaixaDiariaId = caixaId,
            OperadorId = operadorId,
            DataHora = quando,
            MetodoPagamento = metodo,
            Estado = EstadoVenda.Concluida,
            ValorRecebido = valorRecebido,
            Troco = troco,
            TotalSemIva = Math.Round(totalSemIva, 2),
            TotalIva = Math.Round(totalIva, 2),
            Total = Math.Round(total, 2)
        };

        var ano = quando.Year;
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            VendaId = vendaId,
            LojaId = lojaId,
            Ano = ano,
            NumeroSequencial = faturaSeq,
            NumeroCompleto = $"FT {ano}/{faturaSeq:00000}-{lojaId.ToString("N").Substring(28, 4).ToUpperInvariant()}",
            DataEmissao = quando,
            TotalSemIva = venda.TotalSemIva,
            TotalIva = venda.TotalIva,
            Total = venda.Total
        };

        return (venda, linhas, fatura);
    }

    // ------------------------------------------------------- Consolidação

    private static async Task RunConsolidationsAsync(
        ConsolidationService service,
        List<(Guid lojaId, DateOnly data)> dias,
        ILogger logger,
        CancellationToken ct)
    {
        foreach (var (lojaId, data) in dias.Distinct())
        {
            try
            {
                await service.ExecuteForLojaDayAsync(lojaId, data, "Seed", ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Demo seed: falha a consolidar loja {LojaId} dia {Data}", lojaId, data);
            }
        }
        logger.LogInformation(
            "Demo seed: consolidação executada para {N} (loja, dia) — KPIs materializados",
            dias.Distinct().Count());
    }
}
