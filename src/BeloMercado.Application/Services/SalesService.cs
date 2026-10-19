using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Common.Validation;
using BeloMercado.Application.Dtos.Caixa;
using BeloMercado.Application.Dtos.Promocoes;
using BeloMercado.Application.Dtos.Vendas;
using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;

namespace BeloMercado.Application.Services;

/// <summary>
/// Service responsible for sales-side operations (UC01-UC04).
/// </summary>
public class SalesService
{
    // > 10 € absolute difference between expected and counted requires
    // a justificacao at close time (UC04 RN, fluxo alternativo 6a).
    private const decimal CashDiscrepancyThreshold = 10m;

    private readonly ICaixaRepository _caixaRepository;
    private readonly IVendaRepository _vendaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IPromocaoRepository _promocaoRepository;
    private readonly ILojaRepository _lojaRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ConsolidationService _consolidationService;
    private readonly IClock _clock;

    public SalesService(
        ICaixaRepository caixaRepository,
        IVendaRepository vendaRepository,
        IClienteRepository clienteRepository,
        IIdempotencyRepository idempotencyRepository,
        IStockRepository stockRepository,
        IProdutoRepository produtoRepository,
        IPromocaoRepository promocaoRepository,
        ILojaRepository lojaRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ConsolidationService consolidationService,
        IClock clock)
    {
        _caixaRepository = caixaRepository;
        _vendaRepository = vendaRepository;
        _clienteRepository = clienteRepository;
        _idempotencyRepository = idempotencyRepository;
        _stockRepository = stockRepository;
        _produtoRepository = produtoRepository;
        _promocaoRepository = promocaoRepository;
        _lojaRepository = lojaRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _consolidationService = consolidationService;
        _clock = clock;
    }

    // ---------- UC04 ----------

    public async Task<CaixaResponse?> GetCaixaByIdAsync(
        Guid id,
        Guid? scopeLojaId,
        CancellationToken cancellationToken = default)
    {
        var caixa = await _caixaRepository.GetByIdAsync(id, cancellationToken);
        if (caixa is null)
        {
            return null;
        }
        if (scopeLojaId.HasValue && caixa.LojaId != scopeLojaId.Value)
        {
            return null;
        }
        return MapCaixa(caixa);
    }

    /// <summary>
    /// Devolve a caixa de hoje para a loja indicada (aberta ou fechada).
    /// Usada pelo POS para distinguir entre "ainda não há caixa hoje",
    /// "há uma aberta" e "já foi fechada hoje" — regra RN01 do UC04 só
    /// permite uma caixa por loja por dia, ainda que já encerrada.
    /// </summary>
    public async Task<CaixaResponse?> GetCaixaHojeAsync(
        Guid lojaId,
        CancellationToken cancellationToken = default)
    {
        var hoje = _clock.Today;
        var caixa = await _caixaRepository.GetForLojaDayAsync(lojaId, hoje, cancellationToken);
        return caixa is null ? null : MapCaixa(caixa);
    }

    public async Task<OperationResult<CaixaResponse>> AbrirCaixaAsync(
        AbrirCaixaRequest request,
        Guid actorId,
        Guid? actorLojaId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (actorLojaId is null)
        {
            return OperationResult<CaixaResponse>.Validation(
                CaixaErrorCodes.LojaObrigatoria,
                "O operador tem de estar associado a uma loja.");
        }

        if (request.FundoInicial < 0)
        {
            return OperationResult<CaixaResponse>.Validation(
                CaixaErrorCodes.FundoInvalido,
                "Fundo inicial não pode ser negativo.");
        }

        var hoje = _clock.Today;
        if (await _caixaRepository.ExistsForLojaDayAsync(actorLojaId.Value, hoje, cancellationToken))
        {
            return OperationResult<CaixaResponse>.Conflict(
                CaixaErrorCodes.CaixaJaAberta,
                "Já existe uma caixa registada para esta loja hoje.");
        }

        var caixa = new CaixaDiaria
        {
            Id = Guid.NewGuid(),
            LojaId = actorLojaId.Value,
            Data = hoje,
            OperadorAberturaId = actorId,
            DataHoraAbertura = _clock.UtcNow,
            FundoInicial = request.FundoInicial,
            Estado = EstadoCaixa.Aberta
        };

        await _caixaRepository.AddAsync(caixa, cancellationToken);
        await _auditService.LogAsync(
            AuditAcoes.CaixaAberta, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(CaixaDiaria), targetId: caixa.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<CaixaResponse>.Ok(MapCaixa(caixa));
    }

    public async Task<OperationResult<CaixaResponse>> FecharCaixaAsync(
        Guid caixaId,
        FecharCaixaRequest request,
        Guid actorId,
        Guid? actorLojaId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var caixa = await _caixaRepository.GetByIdAsync(caixaId, cancellationToken);
        if (caixa is null
            || (actorLojaId.HasValue && caixa.LojaId != actorLojaId.Value))
        {
            return OperationResult<CaixaResponse>.NotFound(
                CaixaErrorCodes.CaixaNaoEncontrada,
                "Caixa não encontrada.");
        }

        if (caixa.Estado != EstadoCaixa.Aberta)
        {
            return OperationResult<CaixaResponse>.Conflict(
                CaixaErrorCodes.CaixaJaFechada,
                "Esta caixa já está fechada.");
        }

        if (request.ValorContado < 0)
        {
            return OperationResult<CaixaResponse>.Validation(
                CaixaErrorCodes.ValorContadoInvalido,
                "Valor contado não pode ser negativo.");
        }

        // Sales (UC01) not yet aggregated into expected — for now expected
        // = fundo inicial only. When UC01 cashed sales feed in, this should
        // sum Σ(numerário das vendas concluídas para esta caixa).
        var valorEsperado = caixa.FundoInicial;
        var diferenca = request.ValorContado - valorEsperado;

        if (Math.Abs(diferenca) > CashDiscrepancyThreshold
            && string.IsNullOrWhiteSpace(request.Justificacao))
        {
            return OperationResult<CaixaResponse>.Validation(
                CaixaErrorCodes.JustificacaoObrigatoria,
                "Diferença superior a 10€ requer justificação.");
        }

        caixa.Estado = EstadoCaixa.Fechada;
        caixa.DataHoraFecho = _clock.UtcNow;
        caixa.OperadorFechoId = actorId;
        caixa.ValorEsperado = valorEsperado;
        caixa.ValorContado = request.ValorContado;
        caixa.Diferenca = diferenca;
        caixa.Justificacao = string.IsNullOrWhiteSpace(request.Justificacao) ? null : request.Justificacao;
        caixa.ConsolidacaoTriggered = true; // UC12 trigger imediato (RN03 do UC04)

        await _auditService.LogAsync(
            AuditAcoes.CaixaFechada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(CaixaDiaria), targetId: caixa.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // UC12 — Trigger imediato da consolidação (RN03 do UC04). Se falhar,
        // não invalida o fecho de caixa: a consolidação fica em estado Falha
        // e o ConsolidationHostedService retenta-a periodicamente.
        await _consolidationService.ExecuteForLojaDayAsync(
            caixa.LojaId, caixa.Data, "Imediata", cancellationToken);

        return OperationResult<CaixaResponse>.Ok(MapCaixa(caixa));
    }

    // ---------- UC01 ----------

    public async Task<VendaResponse?> GetVendaByIdAsync(
        Guid id,
        Guid? scopeLojaId,
        CancellationToken cancellationToken = default)
    {
        var venda = await _vendaRepository.GetByIdAsync(id, cancellationToken);
        if (venda is null
            || (scopeLojaId.HasValue && venda.LojaId != scopeLojaId.Value))
        {
            return null;
        }
        return await BuildVendaResponseAsync(venda, cancellationToken);
    }

    /// <summary>
    /// Lista as vendas associadas a uma caixa diária (loja + data).
    /// Usado pela UI de anular/devolução para escolher qual a venda.
    /// </summary>
    public async Task<IReadOnlyList<VendaSummaryResponse>> ListVendasByCaixaAsync(
        Guid caixaId,
        Guid? scopeLojaId,
        CancellationToken cancellationToken = default)
    {
        var caixa = await _caixaRepository.GetByIdAsync(caixaId, cancellationToken);
        if (caixa is null
            || (scopeLojaId.HasValue && caixa.LojaId != scopeLojaId.Value))
        {
            return Array.Empty<VendaSummaryResponse>();
        }

        var vendas = await _vendaRepository.GetVendasForLojaDayAsync(
            caixa.LojaId, caixa.Data, cancellationToken);

        var result = new List<VendaSummaryResponse>(vendas.Count);
        foreach (var v in vendas.Where(v => v.CaixaDiariaId == caixaId).OrderByDescending(v => v.DataHora))
        {
            var fatura = await _vendaRepository.GetFaturaForVendaAsync(v.Id, cancellationToken);
            result.Add(new VendaSummaryResponse
            {
                Id = v.Id,
                DataHora = v.DataHora,
                Estado = v.Estado.ToString(),
                MetodoPagamento = v.MetodoPagamento.ToString(),
                Total = v.Total,
                FaturaNumero = fatura?.NumeroCompleto,
                ClienteNif = v.Cliente?.Nif,
                NumLinhas = v.Linhas.Count,
                TemDevolucoes = v.Linhas.Any(l => l.QuantidadeDevolvida > 0)
            });
        }
        return result;
    }

    public async Task<OperationResult<VendaResponse>> CriarVendaAsync(
        CriarVendaRequest request,
        string idempotencyKey,
        Guid actorId,
        Guid? actorLojaId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (actorLojaId is null)
        {
            return OperationResult<VendaResponse>.Validation(
                CaixaErrorCodes.LojaObrigatoria,
                "O operador tem de estar associado a uma loja.");
        }

        // Idempotency: if we already processed this key, return the same venda
        var existing = await _idempotencyRepository.GetAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var prev = await _vendaRepository.GetByIdAsync(existing.VendaId, cancellationToken);
            if (prev is not null)
            {
                return OperationResult<VendaResponse>.Ok(
                    await BuildVendaResponseAsync(prev, cancellationToken));
            }
        }

        if (request.Linhas.Count == 0)
        {
            return OperationResult<VendaResponse>.Validation(
                VendasErrorCodes.SemLinhas,
                "A venda tem de ter pelo menos uma linha.");
        }

        // Pre-condition UC01: caixa do dia aberta para esta loja
        var hoje = _clock.Today;
        var caixa = await _caixaRepository.GetOpenForLojaAsync(actorLojaId.Value, hoje, cancellationToken);
        if (caixa is null)
        {
            return OperationResult<VendaResponse>.Validation(
                VendasErrorCodes.CaixaNaoAberta,
                "Não existe caixa aberta para esta loja hoje.");
        }

        // Validate cliente NIF if present
        if (request.Cliente is not null && !NifValidator.IsValid(request.Cliente.Nif))
        {
            return OperationResult<VendaResponse>.Validation(
                VendasErrorCodes.NifInvalido,
                "NIF inválido.");
        }

        // Build linhas with snapshots; check produto active and stock available
        var linhas = new List<LinhaVenda>(request.Linhas.Count);
        var stocksToDecrement = new List<(Stock stock, int qty)>();
        // (linhaId, promoId, descontoTotalAplicado) — registado após SaveChanges
        var promoApplications = new List<(Guid linhaId, Guid promoId, decimal desconto)>();
        decimal totalSemIva = 0m;
        decimal totalIva = 0m;

        foreach (var l in request.Linhas)
        {
            if (l.Quantidade <= 0)
            {
                return OperationResult<VendaResponse>.Validation(
                    VendasErrorCodes.QuantidadeInvalida,
                    "Quantidade tem de ser positiva.");
            }

            var produto = await _produtoRepository.GetByIdAsync(l.ProdutoId, cancellationToken);
            if (produto is null)
            {
                return OperationResult<VendaResponse>.Validation(
                    VendasErrorCodes.ProdutoNaoEncontrado,
                    $"Produto {l.ProdutoId} não encontrado.");
            }
            if (!produto.Ativo)
            {
                return OperationResult<VendaResponse>.Validation(
                    VendasErrorCodes.ProdutoInativo,
                    $"Produto {produto.Ean13} está inativo.");
            }

            var stock = await _stockRepository.GetByLojaProdutoAsync(actorLojaId.Value, l.ProdutoId, cancellationToken);
            if (stock is null || stock.Quantidade < l.Quantidade)
            {
                return OperationResult<VendaResponse>.Validation(
                    VendasErrorCodes.StockInsuficiente,
                    $"Stock insuficiente para o produto {produto.Ean13} (disponível: {stock?.Quantidade ?? 0}).");
            }

            var precoUnit = produto.Preco;
            var taxaPct = (int)produto.TaxaIva;

            // UC03: aplicar promoção activa se existir.
            // PrecoUnitario passa a ser o preço já descontado; o detalhe da
            // promoção fica em LinhaVendaPromocao para auditoria/relatórios.
            var promo = await _promocaoRepository.GetActiveForProdutoAsync(
                produto.Id, actorLojaId.Value, hoje, cancellationToken);
            decimal descontoUnitario = 0m;
            if (promo is not null)
            {
                descontoUnitario = promo.Tipo switch
                {
                    TipoPromocao.Percentagem => Math.Round(precoUnit * promo.Valor / 100m, 4),
                    TipoPromocao.PrecoFixo => Math.Max(0m, precoUnit - promo.Valor),
                    _ => 0m
                };
                if (descontoUnitario < 0) descontoUnitario = 0m;
                if (descontoUnitario > precoUnit) descontoUnitario = precoUnit;
                precoUnit = Math.Round(precoUnit - descontoUnitario, 2);
            }

            var subtotal = Math.Round(precoUnit * l.Quantidade, 2);
            // IVA mode: preço com IVA incluído (tradicional retalho português).
            // valorIva = subtotal * taxa / (100 + taxa); valorSemIva = subtotal - valorIva
            var valorIva = Math.Round(subtotal * taxaPct / (100m + taxaPct), 2, MidpointRounding.AwayFromZero);
            var valorSemIva = subtotal - valorIva;

            var linhaId = Guid.NewGuid();
            linhas.Add(new LinhaVenda
            {
                Id = linhaId,
                ProdutoId = produto.Id,
                ProdutoDescricao = produto.Descricao,
                ProdutoEan13 = produto.Ean13,
                Quantidade = l.Quantidade,
                PrecoUnitario = precoUnit,
                TaxaIvaPercentagem = taxaPct,
                Subtotal = subtotal,
                ValorIva = valorIva
            });

            if (promo is not null && descontoUnitario > 0)
            {
                promoApplications.Add((linhaId, promo.Id,
                    Math.Round(descontoUnitario * l.Quantidade, 2)));
            }

            stocksToDecrement.Add((stock, l.Quantidade));
            totalSemIva += valorSemIva;
            totalIva += valorIva;
        }

        var total = totalSemIva + totalIva;
        // Round totalSemIva back from accumulated values (line-level rounding).
        totalSemIva = Math.Round(totalSemIva, 2);
        totalIva = Math.Round(totalIva, 2);
        total = Math.Round(total, 2);

        // Cash payment: valor recebido must >= total
        decimal? troco = null;
        if (request.MetodoPagamento == MetodoPagamento.Numerario)
        {
            if (request.ValorRecebido is null || request.ValorRecebido.Value < total)
            {
                return OperationResult<VendaResponse>.Validation(
                    VendasErrorCodes.ValorRecebidoInsuficiente,
                    "Valor recebido é inferior ao total.");
            }
            troco = Math.Round(request.ValorRecebido.Value - total, 2);
        }

        // Cliente: find or create
        Guid? clienteId = null;
        if (request.Cliente is not null)
        {
            var cli = await _clienteRepository.GetByNifAsync(request.Cliente.Nif, cancellationToken);
            if (cli is null)
            {
                cli = new Cliente
                {
                    Id = Guid.NewGuid(),
                    Nif = request.Cliente.Nif,
                    Nome = request.Cliente.Nome,
                    Email = request.Cliente.Email,
                    DataCriacao = _clock.UtcNow
                };
                await _clienteRepository.AddAsync(cli, cancellationToken);
            }
            clienteId = cli.Id;
        }

        // Build Venda + Fatura. Fatura sequence: MAX + 1 for (loja, ano).
        var ano = _clock.Now.Year;
        var nextSeq = await _vendaRepository.GetMaxFaturaSequencialAsync(actorLojaId.Value, ano, cancellationToken) + 1;

        var venda = new Venda
        {
            Id = Guid.NewGuid(),
            LojaId = actorLojaId.Value,
            CaixaDiariaId = caixa.Id,
            OperadorId = actorId,
            ClienteId = clienteId,
            DataHora = _clock.UtcNow,
            MetodoPagamento = request.MetodoPagamento,
            Estado = EstadoVenda.Concluida,
            ValorRecebido = request.ValorRecebido,
            Troco = troco,
            TotalSemIva = totalSemIva,
            TotalIva = totalIva,
            Total = total
        };
        foreach (var l in linhas)
        {
            l.VendaId = venda.Id;
            venda.Linhas.Add(l);
        }
        await _vendaRepository.AddAsync(venda, cancellationToken);

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            VendaId = venda.Id,
            LojaId = actorLojaId.Value,
            Ano = ano,
            NumeroSequencial = nextSeq,
            // Formato: FT YYYY/NNNNN (sequencial por loja/ano) com sufixo
            // estável derivado da loja para garantir unicidade global do
            // NumeroCompleto sem expor o GUID legível.
            NumeroCompleto = $"FT {ano}/{nextSeq:00000}-{actorLojaId.Value.ToString("N").Substring(28, 4).ToUpperInvariant()}",
            DataEmissao = venda.DataHora,
            ClienteId = clienteId,
            TotalSemIva = totalSemIva,
            TotalIva = totalIva,
            Total = total
        };
        await _vendaRepository.AddFaturaAsync(fatura, cancellationToken);

        // Record promo applications for analytics (UC03 RN04)
        foreach (var pa in promoApplications)
        {
            await _promocaoRepository.AddLinhaVendaPromocaoAsync(new LinhaVendaPromocao
            {
                LinhaVendaId = pa.linhaId,
                PromocaoId = pa.promoId,
                ValorDescontoAplicado = pa.desconto
            }, cancellationToken);
        }

        // Decrement stock (entities are tracked, modification is enough)
        foreach (var (stock, qty) in stocksToDecrement)
        {
            stock.Quantidade -= qty;
        }

        // Idempotency entry
        await _idempotencyRepository.AddAsync(new IdempotencyEntry
        {
            Key = idempotencyKey,
            VendaId = venda.Id,
            CreatedAt = _clock.UtcNow
        }, cancellationToken);

        await _auditService.LogAsync(
            AuditAcoes.VendaFinalizada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Venda), targetId: venda.Id,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with cliente for response (only if needed)
        var clienteResp = clienteId.HasValue
            ? await _clienteRepository.GetByNifAsync(request.Cliente!.Nif, cancellationToken)
            : null;

        return OperationResult<VendaResponse>.Ok(BuildVendaResponse(venda, fatura, clienteResp));
    }

    /// <summary>
    /// UC02 — Anular Venda. RN01: Operador só anula vendas com menos de 24h;
    /// vendas mais antigas exigem perfil Gerente. Ao anular: estado vai para
    /// Anulada, stock revertido pela parte ainda não devolvida.
    /// </summary>
    public async Task<OperationResult<VendaResponse>> AnularVendaAsync(
        Guid id,
        AnularVendaRequest request,
        Guid actorId,
        Guid? actorLojaId,
        bool isGerente,
        bool isAdmin,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var venda = await _vendaRepository.GetByIdAsync(id, cancellationToken);
        if (venda is null
            || (actorLojaId.HasValue && !isAdmin && venda.LojaId != actorLojaId.Value))
        {
            return OperationResult<VendaResponse>.NotFound(
                VendasErrorCodes.VendaNaoEncontrada,
                "Venda não encontrada.");
        }

        if (venda.Estado != EstadoVenda.Concluida)
        {
            return OperationResult<VendaResponse>.Conflict(
                VendasErrorCodes.VendaJaAnulada,
                "Apenas vendas concluídas podem ser anuladas.");
        }

        var ageHours = (_clock.UtcNow - venda.DataHora).TotalHours;
        if (ageHours > 24 && !isGerente && !isAdmin)
        {
            return OperationResult<VendaResponse>.Forbidden(
                VendasErrorCodes.ApprovalRequired,
                "Anulação de vendas com mais de 24h requer perfil Gerente.");
        }

        // Revert stock for the non-already-returned portion of each linha
        foreach (var linha in venda.Linhas)
        {
            var qtyToRevert = linha.Quantidade - linha.QuantidadeDevolvida;
            if (qtyToRevert <= 0)
            {
                continue;
            }
            var stock = await _stockRepository.GetByLojaProdutoAsync(
                venda.LojaId, linha.ProdutoId, cancellationToken);
            if (stock is null)
            {
                return OperationResult<VendaResponse>.Validation(
                    EncomendasErrorCodes.StockNaoMaterializado,
                    $"Stock não materializado para {linha.ProdutoEan13} na loja.");
            }
            stock.Quantidade += qtyToRevert;
            // Mark all of the linha as returned-equivalent (consistency)
            linha.QuantidadeDevolvida = linha.Quantidade;
        }

        venda.Estado = EstadoVenda.Anulada;

        await _auditService.LogAsync(
            AuditAcoes.VendaAnulada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Venda), targetId: venda.Id,
            detalhes: request.Motivo,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<VendaResponse>.Ok(await BuildVendaResponseAsync(venda, cancellationToken));
    }

    /// <summary>
    /// UC02 — Devolução parcial. Permite devolver quantidades específicas
    /// de uma venda Concluída sem cancelar a venda inteira. Stock incrementa
    /// pela quantidade devolvida; LinhaVenda.QuantidadeDevolvida é atualizada.
    /// </summary>
    public async Task<OperationResult<VendaResponse>> DevolucaoAsync(
        Guid id,
        DevolucaoRequest request,
        Guid actorId,
        Guid? actorLojaId,
        bool isAdmin,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var venda = await _vendaRepository.GetByIdAsync(id, cancellationToken);
        if (venda is null
            || (actorLojaId.HasValue && !isAdmin && venda.LojaId != actorLojaId.Value))
        {
            return OperationResult<VendaResponse>.NotFound(
                VendasErrorCodes.VendaNaoEncontrada,
                "Venda não encontrada.");
        }

        if (venda.Estado != EstadoVenda.Concluida)
        {
            return OperationResult<VendaResponse>.Conflict(
                VendasErrorCodes.VendaNaoConcluida,
                "Apenas vendas concluídas aceitam devoluções.");
        }

        if (request.Linhas.Count == 0)
        {
            return OperationResult<VendaResponse>.Validation(
                VendasErrorCodes.DevolucaoSemLinhas,
                "Pelo menos uma linha tem de ser indicada.");
        }

        var linhasById = venda.Linhas.ToDictionary(l => l.Id);
        // Validate first
        foreach (var lr in request.Linhas)
        {
            if (!linhasById.TryGetValue(lr.LinhaVendaId, out var linha))
            {
                return OperationResult<VendaResponse>.Validation(
                    VendasErrorCodes.DevolucaoLinhaNaoEncontrada,
                    $"Linha {lr.LinhaVendaId} não pertence a esta venda.");
            }
            if (lr.Quantidade <= 0
                || linha.QuantidadeDevolvida + lr.Quantidade > linha.Quantidade)
            {
                return OperationResult<VendaResponse>.Validation(
                    VendasErrorCodes.DevolucaoQtdInvalida,
                    $"Quantidade devolvida inválida para linha {lr.LinhaVendaId}.");
            }
        }

        // Apply: increment stock, update QuantidadeDevolvida
        foreach (var lr in request.Linhas)
        {
            var linha = linhasById[lr.LinhaVendaId];
            var stock = await _stockRepository.GetByLojaProdutoAsync(
                venda.LojaId, linha.ProdutoId, cancellationToken);
            if (stock is null)
            {
                return OperationResult<VendaResponse>.Validation(
                    EncomendasErrorCodes.StockNaoMaterializado,
                    $"Stock não materializado para {linha.ProdutoEan13} na loja.");
            }
            stock.Quantidade += lr.Quantidade;
            linha.QuantidadeDevolvida += lr.Quantidade;
        }

        await _auditService.LogAsync(
            AuditAcoes.VendaDevolucao, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Venda), targetId: venda.Id,
            detalhes: request.Motivo,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<VendaResponse>.Ok(await BuildVendaResponseAsync(venda, cancellationToken));
    }

    // ---------- mapping helpers ----------

    private async Task<VendaResponse> BuildVendaResponseAsync(Venda venda, CancellationToken cancellationToken)
    {
        var fatura = await _vendaRepository.GetFaturaForVendaAsync(venda.Id, cancellationToken);
        return BuildVendaResponse(venda, fatura ?? new Fatura(), venda.Cliente);
    }

    private static VendaResponse BuildVendaResponse(Venda venda, Fatura fatura, Cliente? cliente)
    {
        return new VendaResponse
        {
            Id = venda.Id,
            LojaId = venda.LojaId,
            CaixaDiariaId = venda.CaixaDiariaId,
            OperadorId = venda.OperadorId,
            DataHora = venda.DataHora,
            MetodoPagamento = venda.MetodoPagamento.ToString(),
            Estado = venda.Estado.ToString(),
            ValorRecebido = venda.ValorRecebido,
            Troco = venda.Troco,
            TotalSemIva = venda.TotalSemIva,
            TotalIva = venda.TotalIva,
            Total = venda.Total,
            Linhas = venda.Linhas.Select(l => new LinhaVendaResponse
            {
                Id = l.Id,
                ProdutoId = l.ProdutoId,
                ProdutoEan13 = l.ProdutoEan13,
                ProdutoDescricao = l.ProdutoDescricao,
                Quantidade = l.Quantidade,
                PrecoUnitario = l.PrecoUnitario,
                TaxaIvaPercentagem = l.TaxaIvaPercentagem,
                Subtotal = l.Subtotal,
                ValorIva = l.ValorIva,
                QuantidadeDevolvida = l.QuantidadeDevolvida
            }).ToList(),
            Fatura = new FaturaResponse
            {
                Id = fatura.Id,
                NumeroCompleto = fatura.NumeroCompleto,
                DataEmissao = fatura.DataEmissao,
                ClienteNif = cliente?.Nif,
                ClienteNome = cliente?.Nome,
                TotalSemIva = fatura.TotalSemIva,
                TotalIva = fatura.TotalIva,
                Total = fatura.Total
            }
        };
    }

    private static CaixaResponse MapCaixa(CaixaDiaria c) => new()
    {
        Id = c.Id,
        LojaId = c.LojaId,
        Data = c.Data,
        OperadorAberturaId = c.OperadorAberturaId,
        DataHoraAbertura = c.DataHoraAbertura,
        OperadorFechoId = c.OperadorFechoId,
        DataHoraFecho = c.DataHoraFecho,
        FundoInicial = c.FundoInicial,
        ValorEsperado = c.ValorEsperado,
        ValorContado = c.ValorContado,
        Diferenca = c.Diferenca,
        Justificacao = c.Justificacao,
        Estado = c.Estado.ToString()
    };

    // ---------- UC03 ----------

    public async Task<IReadOnlyList<PromocaoResponse>> ListPromocoesAsync(CancellationToken cancellationToken = default)
    {
        var promos = await _promocaoRepository.GetAllAsync(cancellationToken);
        return promos.Select(MapPromocao).ToList();
    }

    public async Task<PromocaoResponse?> GetPromocaoByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _promocaoRepository.GetByIdAsync(id, cancellationToken);
        return p is null ? null : MapPromocao(p);
    }

    public async Task<OperationResult<PromocaoResponse>> CreatePromocaoAsync(
        CreatePromocaoRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidatePromocaoAsync(request.ProdutoId, request.LojaId,
            request.Tipo, request.Valor, request.DataInicio, request.DataFim, cancellationToken);
        if (validation is not null)
        {
            return OperationResult<PromocaoResponse>.Validation(validation.Value.code, validation.Value.message);
        }

        var promo = new Promocao
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            ProdutoId = request.ProdutoId,
            LojaId = request.LojaId,
            Tipo = request.Tipo,
            Valor = request.Valor,
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Ativo = true,
            DataCriacao = _clock.UtcNow
        };

        await _promocaoRepository.AddAsync(promo, cancellationToken);
        await _auditService.LogAsync(
            AuditAcoes.PromocaoCriada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Promocao), targetId: promo.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var loaded = await _promocaoRepository.GetByIdAsync(promo.Id, cancellationToken);
        return OperationResult<PromocaoResponse>.Ok(MapPromocao(loaded ?? promo));
    }

    public async Task<OperationResult<PromocaoResponse>> UpdatePromocaoAsync(
        Guid id,
        UpdatePromocaoRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var promo = await _promocaoRepository.GetByIdAsync(id, cancellationToken);
        if (promo is null)
        {
            return OperationResult<PromocaoResponse>.NotFound(
                PromocoesErrorCodes.NaoEncontrada, "Promoção não encontrada.");
        }

        var validation = await ValidatePromocaoAsync(promo.ProdutoId, request.LojaId,
            request.Tipo, request.Valor, request.DataInicio, request.DataFim, cancellationToken);
        if (validation is not null)
        {
            return OperationResult<PromocaoResponse>.Validation(validation.Value.code, validation.Value.message);
        }

        promo.Nome = request.Nome;
        promo.LojaId = request.LojaId;
        promo.Tipo = request.Tipo;
        promo.Valor = request.Valor;
        promo.DataInicio = request.DataInicio;
        promo.DataFim = request.DataFim;
        promo.Ativo = request.Ativo;

        await _auditService.LogAsync(
            AuditAcoes.PromocaoAtualizada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Promocao), targetId: promo.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<PromocaoResponse>.Ok(MapPromocao(promo));
    }

    public async Task<OperationResult<PromocaoResponse>> DeactivatePromocaoAsync(
        Guid id,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var promo = await _promocaoRepository.GetByIdAsync(id, cancellationToken);
        if (promo is null)
        {
            return OperationResult<PromocaoResponse>.NotFound(
                PromocoesErrorCodes.NaoEncontrada, "Promoção não encontrada.");
        }

        promo.Ativo = false;

        await _auditService.LogAsync(
            AuditAcoes.PromocaoDesativada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Promocao), targetId: promo.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<PromocaoResponse>.Ok(MapPromocao(promo));
    }

    private async Task<(string code, string message)?> ValidatePromocaoAsync(
        Guid produtoId,
        Guid? lojaId,
        TipoPromocao tipo,
        decimal valor,
        DateOnly dataInicio,
        DateOnly dataFim,
        CancellationToken cancellationToken)
    {
        if (dataFim < dataInicio)
        {
            return (PromocoesErrorCodes.DatasInvalidas, "DataFim tem de ser >= DataInicio.");
        }
        if (tipo == TipoPromocao.Percentagem && (valor <= 0 || valor > 100))
        {
            return (PromocoesErrorCodes.PercentagemInvalida, "Percentagem tem de estar em (0, 100].");
        }
        if (tipo == TipoPromocao.PrecoFixo && valor <= 0)
        {
            return (PromocoesErrorCodes.PrecoFixoInvalido, "Preço fixo tem de ser positivo.");
        }
        if (await _produtoRepository.GetByIdAsync(produtoId, cancellationToken) is null)
        {
            return (PromocoesErrorCodes.ProdutoInexistente, "Produto não existe.");
        }
        if (lojaId is not null
            && await _lojaRepository.GetByIdAsync(lojaId.Value, cancellationToken) is null)
        {
            return (PromocoesErrorCodes.LojaInexistente, "Loja não existe.");
        }
        return null;
    }

    private static PromocaoResponse MapPromocao(Promocao p) => new()
    {
        Id = p.Id,
        Nome = p.Nome,
        ProdutoId = p.ProdutoId,
        ProdutoEan13 = p.Produto?.Ean13 ?? string.Empty,
        ProdutoDescricao = p.Produto?.Descricao ?? string.Empty,
        LojaId = p.LojaId,
        Tipo = p.Tipo.ToString(),
        Valor = p.Valor,
        DataInicio = p.DataInicio,
        DataFim = p.DataFim,
        Ativo = p.Ativo,
        DataCriacao = p.DataCriacao
    };
}

