namespace AuditoriaDocumental.Api.Modelos;

public class Extraccion
{
    public int Id { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public decimal ImporteTotal { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string DatosRawJSON { get; set; } = string.Empty; // Respuesta cruda de la IA

    // Relación: Pertenece a un único documento
    public int DocumentoId { get; set; }
    public Documento? Documento { get; set; }
}