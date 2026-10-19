namespace BeloMercado.Application.Dtos.Utilizadores;

public class CreateUtilizadorResponse
{
    public UtilizadorResponse Utilizador { get; set; } = new();

    /// <summary>
    /// Temporary password for first login. In a production system this would
    /// be sent by email and never returned in the API response. Returned here
    /// only because the academic prototype has no email integration.
    /// </summary>
    public string TemporaryPassword { get; set; } = string.Empty;
}
