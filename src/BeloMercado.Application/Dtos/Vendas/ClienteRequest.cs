using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Vendas;

public class ClienteRequest
{
    [Required, StringLength(9, MinimumLength = 9)]
    public string Nif { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nome { get; set; }

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }
}
