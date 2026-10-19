using BeloMercado.Application.Common;
using BeloMercado.Application.Dtos.Stock;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Services;

public class InventoryService
{
    private readonly IStockRepository _stockRepository;
    private readonly ILojaRepository _lojaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IStockRepository stockRepository,
        ILojaRepository lojaRepository,
        IProdutoRepository produtoRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _lojaRepository = lojaRepository;
        _produtoRepository = produtoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<StockResponse>> ListAsync(
        Guid? scopeLojaId,
        CancellationToken cancellationToken = default)
    {
        var stocks = scopeLojaId.HasValue
            ? await _stockRepository.ListByLojaAsync(scopeLojaId.Value, cancellationToken)
            : await _stockRepository.ListAllAsync(cancellationToken);

        return stocks.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<StockResponse>> ListAlertasAsync(
        Guid? scopeLojaId,
        CancellationToken cancellationToken = default)
    {
        var alertas = scopeLojaId.HasValue
            ? await _stockRepository.ListAlertasByLojaAsync(scopeLojaId.Value, cancellationToken)
            : await _stockRepository.ListAllAlertasAsync(cancellationToken);

        return alertas.Select(MapToResponse).ToList();
    }

    /// <summary>
    /// Materialises Stock rows for every existing Produto under a newly
    /// created Loja. Min/Max default to the loja's per-default values;
    /// quantidade starts at 0.
    /// </summary>
    public async Task InitializeStockForNewLojaAsync(
        Loja loja,
        CancellationToken cancellationToken = default)
    {
        var produtos = await _produtoRepository.GetAllAsync(cancellationToken);
        if (produtos.Count == 0)
        {
            return;
        }

        var rows = produtos.Select(p => new Stock
        {
            Id = Guid.NewGuid(),
            LojaId = loja.Id,
            ProdutoId = p.Id,
            Quantidade = 0,
            QtdMinima = loja.StockMinPorDefeito,
            QtdMaxima = loja.StockMaxPorDefeito
        });

        await _stockRepository.AddRangeAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Materialises Stock rows for every existing Loja when a new Produto
    /// is added to the catálogo. Each row uses that loja's default min/max.
    /// </summary>
    public async Task InitializeStockForNewProdutoAsync(
        Guid produtoId,
        CancellationToken cancellationToken = default)
    {
        var lojas = await _lojaRepository.GetAllAsync(cancellationToken);
        if (lojas.Count == 0)
        {
            return;
        }

        var rows = lojas.Select(l => new Stock
        {
            Id = Guid.NewGuid(),
            LojaId = l.Id,
            ProdutoId = produtoId,
            Quantidade = 0,
            QtdMinima = l.StockMinPorDefeito,
            QtdMaxima = l.StockMaxPorDefeito
        });

        await _stockRepository.AddRangeAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Atualiza os limites de stock (mínimo/máximo) de uma linha. Quantidade
    /// não é alterada por esta operação — usa-se na ficha de stock do
    /// Backoffice para que o gerente afine os alertas de reposição.
    /// </summary>
    public async Task<OperationResult<StockResponse>> UpdateLimitsAsync(
        Guid stockId,
        UpdateStockLimitsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QtdMinima < 0 || request.QtdMaxima < 0)
        {
            return OperationResult<StockResponse>.Validation(
                "STOCK_LIMITES_INVALIDOS",
                "Os limites de stock têm de ser >= 0.");
        }
        if (request.QtdMaxima < request.QtdMinima)
        {
            return OperationResult<StockResponse>.Validation(
                "STOCK_LIMITES_INVALIDOS",
                "O stock máximo tem de ser >= ao stock mínimo.");
        }

        var stock = await _stockRepository.GetByIdAsync(stockId, cancellationToken);
        if (stock is null)
        {
            return OperationResult<StockResponse>.NotFound(
                "STOCK_NAO_ENCONTRADO", "Linha de stock não encontrada.");
        }

        stock.QtdMinima = request.QtdMinima;
        stock.QtdMaxima = request.QtdMaxima;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<StockResponse>.Ok(MapToResponse(stock));
    }

    private static StockResponse MapToResponse(Stock s) => new()
    {
        Id = s.Id,
        LojaId = s.LojaId,
        LojaDesignacao = s.Loja?.Designacao ?? string.Empty,
        ProdutoId = s.ProdutoId,
        ProdutoEan13 = s.Produto?.Ean13 ?? string.Empty,
        ProdutoDescricao = s.Produto?.Descricao ?? string.Empty,
        Quantidade = s.Quantidade,
        QtdMinima = s.QtdMinima,
        QtdMaxima = s.QtdMaxima,
        AbaixoMinimo = s.Quantidade < s.QtdMinima
    };
}
