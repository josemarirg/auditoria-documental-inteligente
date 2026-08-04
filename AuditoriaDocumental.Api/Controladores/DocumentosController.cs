namespace AuditoriaDocumental.Api.Controladores;

using System; 
using Microsoft.Extensions.Configuration; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Datos;
using AuditoriaDocumental.Api.Modelos;
using AuditoriaDocumental.Api.Servicios; // añadido para poder usar nuestro servicio de ia
using Azure.Storage.Blobs; 
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly string _connectionString;
    private readonly string _containerName = "documentos-auditados";
    private readonly ServicioExtraccionIA _servicioIA; // variable para guardar el cerebro de la ia

    // inyectamos la base de datos, la configuracion y nuestro nuevo servicio de ia
    public DocumentosController(AppDbContext context, IConfiguration configuration, ServicioExtraccionIA servicioIA)
    {
        _context = context;
        _servicioIA = servicioIA; // lo guardamos para usarlo luego
        
        _connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") 
                            ?? configuration.GetConnectionString("AzureStorage")
                            ?? throw new InvalidOperationException("no se encuentra la cadena de conexion de azure");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos()
    {
        var documentos = await _context.Documentos
            .Include(d => d.Extraccion)
            .ToListAsync();

        return Ok(documentos);
    }

    [HttpPost("upload")]
    [EnableRateLimiting("limiteSubida")] 
    [RequestSizeLimit(5242880)] 
    public async Task<IActionResult> UploadDocumento(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest("no has subido ningún archivo.");

        if (archivo.ContentType != "application/pdf" && !archivo.FileName.EndsWith(".pdf"))
            return BadRequest("solo aceptamos archivos pdf.");

        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var nombreArchivo = Guid.NewGuid().ToString() + ".pdf";
            var blobClient = containerClient.GetBlobClient(nombreArchivo);

            // abrimos el archivo una sola vez
            using (var stream = archivo.OpenReadStream())
            {
                // 1. lo subimos al blob storage de azure
                await blobClient.UploadAsync(stream, true);

                // 2. se lo pasamos a la ia para que extraiga el json, el importe, etc.
                var datosExtraidos = await _servicioIA.AnalizarFacturaAsync(stream);

                var rutaAzure = blobClient.Uri.AbsoluteUri;

                // 3. creamos el documento y le enganchamos lo que ha extraido la ia
                var nuevoDocumento = new Documento
                {
                    NombreArchivo = archivo.FileName, 
                    RutaBlobAzure = rutaAzure, 
                    Estado = "Pendiente",
                    FechaSubida = DateTime.UtcNow,
                    Extraccion = datosExtraidos // aqui hacemos la magia de unir las dos tablas
                };

                // guardamos todo del tiron en sql server
                _context.Documentos.Add(nuevoDocumento);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    mensaje = "pdf subido y analizado por ia con éxito", 
                    documentoId = nuevoDocumento.Id, 
                    url = rutaAzure,
                    datosIA = datosExtraidos // lo devolvemos para verlo en swagger
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"error interno al subir a azure o analizar con ia: {ex.Message}");
        }
    }
}