using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Lojas;

public class UpdateLojaRequest
{
    [Required, MaxLength(100)]
    public string Designacao { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Morada { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string CodigoPostal { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Contacto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Horario { get; set; }

    [Range(0, int.MaxValue)]
    public int StockMinPorDefeito { get; set; }

    [Range(0, int.MaxValue)]
    public int StockMaxPorDefeito { get; set; }

    public bool Ativo { get; set; } = true;
}
