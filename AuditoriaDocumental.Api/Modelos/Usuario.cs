namespace AuditoriaDocumental.Api.Modelos;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = "Auditor"; // Puede ser Admin o Auditor
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Relación: Un usuario puede auditar muchos documentos
    public List<Documento> Documentos { get; set; } = new();
}