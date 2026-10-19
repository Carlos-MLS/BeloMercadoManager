namespace BeloMercado.Application.Dtos.Me;

public class MeResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public Guid? LojaId { get; set; }

    /// <summary>Nome amigável da loja (preenchido só quando LojaId não é nulo).</summary>
    public string? LojaDesignacao { get; set; }

    public bool PasswordTemporaria { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
}
