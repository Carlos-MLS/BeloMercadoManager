namespace BeloMercado.Application.Common.Constants;

public static class VendasErrorCodes
{
    public const string SemLinhas = "VENDA_SEM_LINHAS";
    public const string ProdutoNaoEncontrado = "PRODUTO_NAO_ENCONTRADO";
    public const string ProdutoInativo = "PRODUTO_INATIVO";
    public const string StockInsuficiente = "STOCK_INSUFICIENTE";
    public const string CaixaNaoAberta = "CAIXA_NAO_ABERTA";
    public const string NifInvalido = "NIF_INVALIDO";
    public const string ValorRecebidoInsuficiente = "VALOR_RECEBIDO_INSUFICIENTE";
    public const string QuantidadeInvalida = "QUANTIDADE_INVALIDA";
    public const string VendaNaoEncontrada = "VENDA_NAO_ENCONTRADA";
    public const string IdempotencyKeyObrigatoria = "IDEMPOTENCY_KEY_OBRIGATORIA";
    public const string VendaJaAnulada = "VENDA_JA_ANULADA";
    public const string VendaNaoConcluida = "VENDA_NAO_CONCLUIDA";
    public const string ApprovalRequired = "APPROVAL_REQUIRED";
    public const string DevolucaoQtdInvalida = "DEVOLUCAO_QUANTIDADE_INVALIDA";
    public const string DevolucaoLinhaNaoEncontrada = "DEVOLUCAO_LINHA_NAO_ENCONTRADA";
    public const string DevolucaoSemLinhas = "DEVOLUCAO_SEM_LINHAS";
}
