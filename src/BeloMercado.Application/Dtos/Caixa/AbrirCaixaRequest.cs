using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Caixa;

public class AbrirCaixaRequest
{
    [Range(0, 99999.99)]
    public decimal FundoInicial { get; set; }
}
