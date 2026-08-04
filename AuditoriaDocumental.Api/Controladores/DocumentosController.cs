namespace AuditoriaDocumental.Api.Controladores;

using System; 
using Microsoft.Extensions.Configuration; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Datos;
using AuditoriaDocumental.Api.Modelos;
using Azure.Storage.Blobs; 
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly string _connectionString;
    private readonly string _containerName = "documentos-auditados"; // el nombre de la carpeta en azure

    // inyectamos la base de datos y la configuracion para leer las variables
    public DocumentosController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        
        // pillamos la cadena de conexion.lo leera de  user secrets
        _connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") 
                            ?? configuration.GetConnectionString("AzureStorage")
                            ?? throw new InvalidOperationException("no encuentro la cadena de conexion de azure");
    }

    // get: api/documentos
    // este endpoint nos servirá para llenar la tabla de angular
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos()
    {
        // buscamos los documentos e incluimos los datos de la ia si los tiene
        var documentos = await _context.Documentos
            .Include(d => d.Extraccion)
            .ToListAsync();

        return Ok(documentos);
    }

    // post: api/documentos/upload
    // aquí es donde mandaremos el pdf desde el frontend
    [HttpPost("upload")]
    [EnableRateLimiting("limiteSubida")] // aplicamos la regla antispam
    [RequestSizeLimit(5242880)] // limitamos el peso a 5mb (en bytes) para que no suban archivos gigantes
    public async Task<IActionResult> UploadDocumento(IFormFile archivo)
    {
        // comprobamos que nos han mandado algo y que es un pdf
        if (archivo == null || archivo.Length == 0)
            return BadRequest("no has subido ningún archivo.");

        if (archivo.ContentType != "application/pdf" && !archivo.FileName.EndsWith(".pdf"))
            return BadRequest("solo aceptamos archivos pdf.");

        try
        {
            // nos conectamos a azure usando la cadena de conexion
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            
            // creamos el contenedor de azure si no existe todavia
            await containerClient.CreateIfNotExistsAsync();

            // generamos un nombre único para que no se pisen si suben dos con el mismo nombre
            var nombreArchivo = Guid.NewGuid().ToString() + ".pdf";
            var blobClient = containerClient.GetBlobClient(nombreArchivo);

            // subimos el archivo a la nube directamente desde la memoria
            using (var stream = archivo.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            // sacamos la url real de donde se ha guardado en azure
            var rutaAzure = blobClient.Uri.AbsoluteUri;

            // creamos el registro para guardarlo en la base de datos sql
            var nuevoDocumento = new Documento
            {
                NombreArchivo = archivo.FileName, // el nombre original que tenía el usuario
                RutaBlobAzure = rutaAzure, // ahora guardamos la url de azure en vez de la local
                Estado = "Pendiente",
                FechaSubida = DateTime.UtcNow
            };

            _context.Documentos.Add(nuevoDocumento);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "pdf subido a azure con éxito", documentoId = nuevoDocumento.Id, url = rutaAzure });
        }
        catch (Exception ex)
        {
            // por si peta la conexion con la nube o algo
            return StatusCode(500, $"error interno al subir a azure: {ex.Message}");
        }
    }
}