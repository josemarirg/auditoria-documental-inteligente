namespace AuditoriaDocumental.Api.Modelos;

public class Documento
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaBlobAzure { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobado, Rechazado
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    // Relación: Puede ser validado por un usuario (Auditor)
    public int? AuditorId { get; set; }
    public Usuario? Auditor { get; set; }

    // Relación: Un documento tiene una extracción de IA asociada
    public Extraccion? Extraccion { get; set; }
}