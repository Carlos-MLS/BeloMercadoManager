namespace BeloMercado.Application.Common.Constants;

public static class EncomendasErrorCodes
{
    public const string NaoEncontrada = "ENCOMENDA_NAO_ENCONTRADA";
    public const string SemLinhas = "ENCOMENDA_SEM_LINHAS";
    public const string FornecedorInativo = "ENCOMENDA_FORNECEDOR_INATIVO";
    public const string FornecedorNaoEncontrado = "ENCOMENDA_FORNECEDOR_NAO_ENCONTRADO";
    public const string ProdutoSemAssociacao = "ENCOMENDA_PRODUTO_SEM_ASSOCIACAO";
    public const string QuantidadeInvalida = "ENCOMENDA_QUANTIDADE_INVALIDA";
    public const string EstadoInvalido = "ENCOMENDA_ESTADO_INVALIDO";
    public const string ApprovalRequired = "APPROVAL_REQUIRED";
    public const string LojaObrigatoria = "ENCOMENDA_LOJA_OBRIGATORIA";
    public const string QuantidadeRecebidaInvalida = "ENCOMENDA_QUANTIDADE_RECEBIDA_INVALIDA";
    public const string LinhaNaoEncontrada = "ENCOMENDA_LINHA_NAO_ENCONTRADA";
    public const string EstadoNaoRececionavel = "ENCOMENDA_ESTADO_NAO_RECECIONAVEL";
    public const string StockNaoMaterializado = "ENCOMENDA_STOCK_NAO_MATERIALIZADO";
}
